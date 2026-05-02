using Common.Utils;
using DumbFox.Utils;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using System.Net.Sockets;

namespace DumbFox.Handlers;

public static class RequestHandler
{
    public static Task HandleRequest(short action, byte controller, SFSObject parameters, Session session)
    {
        RequestType requestType = (RequestType)action;

        return requestType switch
        {
            RequestType.Handshake       => HandleHandshake(session.Stream),
            RequestType.Login           => HandleLogin(parameters, session),
            RequestType.JoinRoom        => HandleJoin(parameters, session),
            RequestType.CallExtension   => HandleCallExtension(parameters, session),
            RequestType.PingPong        => HandlePingPong(session),
            RequestType.SetUserVariables=> HandleSetUserVariables(parameters, session),

            _ => LogUnhandled(controller, action, requestType)
        };
    }

    private static async Task LogUnhandled(byte controller, short action, RequestType requestType)
    {
        Console.WriteLine($"Unhandled request: controller={controller} action={action} ({requestType})");
        await Task.CompletedTask;
    }

    private static async Task HandleHandshake(NetworkStream stream)
    {
        SFSObject payload = new SFSObject();
        payload.PutUtfString("tk", Guid.NewGuid().ToString("N"));
        payload.PutInt("ct", 1_000_000);
        payload.PutInt("ms", 65_535);

        await SfsWire.WriteMessageAsync(stream, 0, (short)RequestType.Handshake, payload);
    }

    private static async Task HandleLogin(SFSObject req, Session session)
    {
        string zone = req.GetUtfString("zn");
        string user = req.GetUtfString("un");
        SFSObject extra = req.ContainsKey("p") ? (SFSObject)req.GetSFSObject("p") : new SFSObject();
        ISFSObject joinRoomData = SFSObject.NewFromJsonData(extra.GetUtfString("joinRoomData"));
        ISFSObject data = joinRoomData.GetSFSObject("data");
        ISFSObject playerRoomData = data.GetSFSObject("playerRoomData");
        
        Console.WriteLine($"Login request extra: {extra.ToJson()}");

        (string? requestedRoom, long? joinRoomSessionId) = RoomUtils.TryGetJoinRoomData(req);
        requestedRoom ??= "Lobby";
        
        long cpSessionId = joinRoomSessionId ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        int userId = Environment.TickCount;

        session.UserId = userId;
        session.UserName = user;
        session.SessionId = cpSessionId;
        session.Swid = joinRoomData.GetUtfString("swid");
        session.IsLoggedIn = true;
        
        session.SetUserVar(new SFSUserVariable("sess", session.SessionId.ToString()));
        session.SetUserVar(new SFSUserVariable("tube", playerRoomData.GetInt("selectedTubeId")));
        session.SetUserVar(new SFSUserVariable("colour", playerRoomData.GetSFSObject("profile").GetInt("colour")));
        session.SetUserVar(new SFSUserVariable("outfit", playerRoomData.GetSFSObject("outfit").ToJson()));

        World.SessionsByUserId[userId] = session;
        
        var res = new SFSObject();
        res.PutInt("id", userId);
        res.PutUtfString("un", user);
        res.PutShort("pi", 0);
        res.PutShort("rs", 0);
        res.PutUtfString("zn", zone);

        Room room = World.GetOrCrateRoom(requestedRoom);

        SFSArray rl = new SFSArray();
        rl.AddSFSArray(RoomUtils.FromRoomState(room, "default", 39, 100));
        res.PutSFSArray("rl", rl);

        SFSObject payload = new SFSObject();
        payload.PutUtfString("_sid", "node-1");
        res.PutSFSObject("p", payload);

        await SfsWire.WriteMessageAsync(session.Stream, controller: 0, action: 1, parameters: res);
        Console.WriteLine($"Login OK -> userId={userId} room='{room.Name}' sessionId={cpSessionId}");
        
        await RoomUtils.SendJoinRoom(session.Stream, room, "default", session);
    }

    private static Task HandleJoin(SFSObject req, Session session)
    {
        string? roomName = req.ContainsKey("n") ? req.GetUtfString("n") : null;

        if (roomName is null) throw new InvalidOperationException("Join room request missing room name");
        
        Room room = World.GetOrCrateRoom(roomName);
        
        return RoomUtils.SendJoinRoom(session.Stream, room, "default", session);
    }

    private static Task HandleCallExtension(SFSObject req, Session session)
    {
        string cmd = req.GetUtfString("c");
        int roomId = req.ContainsKey("r") ? req.GetInt("r") : (session.CurrentRoomId ?? 0);
        SFSObject p = req.ContainsKey("p") ? (SFSObject)req.GetSFSObject("p") : new SFSObject();

        return ExtensionHandler.HandleExtensionAsync(cmd, roomId, session, p);
    }

    private static Task HandlePingPong(Session session)
    {
        SFSObject res = new SFSObject();
        return SfsWire.WriteMessageAsync(session.Stream, controller: 0, action: (short)RequestType.PingPong, parameters: res);
    }

    private static async Task HandleSetUserVariables(SFSObject req, Session session)
    {
        Console.WriteLine($"SetUserVariables Request: {req.ToJson()}");
        ISFSArray varList = req.GetSFSArray("vl");
        foreach (ISFSArray var in varList)
        {
            UserVariable userVar = SFSUserVariable.FromSFSArray(var);
            await UserVarHandler.HandleUserVarUpdate(session, userVar);
        }
    }
}