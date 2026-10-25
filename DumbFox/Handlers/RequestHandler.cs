using Common.Utils;
using Common.Utils.Game;
using DumbFox.Utils;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace DumbFox.Handlers;

public static class RequestHandler
{
    public static Task HandleRequest(short action, byte controller, SFSObject parameters, Session session)
    {
        RequestType requestType = (RequestType)action;

        return requestType switch
        {
            RequestType.Handshake       => HandleHandshake(session),
            RequestType.Login           => HandleLogin(parameters, session),
            RequestType.JoinRoom        => HandleJoin(parameters, session),
            RequestType.CallExtension   => HandleCallExtension(parameters, session),
            RequestType.PingPong        => HandlePingPong(session) ,
            RequestType.SetUserVariables=> HandleSetUserVariables(parameters, session),

            _ => LogUnhandled(controller, action, requestType)
        };
    }

    private static async Task LogUnhandled(byte controller, short action, RequestType requestType)
    {
        Console.WriteLine($"Unhandled request: controller={controller} action={action} ({requestType})");
        await Task.CompletedTask;
    }

    private static async Task HandleHandshake(Session session)
    {
        SFSObject payload = new SFSObject();
        payload.PutUtfString("tk", Guid.NewGuid().ToString("N"));
        payload.PutInt("ct", 1_000_000);
        payload.PutInt("ms", 65_535);

        await SfsWire.WriteMessageAsync(session, 0, (short)RequestType.Handshake, payload);
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

        Console.WriteLine($"{session.LocalEndPoint.Address}: {session.LocalEndPoint.Port}");
        Console.WriteLine($"{session.RemoteEndPoint.Address}: {session.RemoteEndPoint.Port}");
        int userId = session.RemoteEndPoint.Port; //Environment.TickCount;

        session.UserId = userId;
        session.UserName = user;
        session.SessionId = cpSessionId;
        session.Swid = joinRoomData.GetUtfString("swid");
        session.Quest = JsonSerializer.Deserialize<List<QuestStateCollection>>(playerRoomData.GetSFSArray("quests").ToJson()) ?? new List<QuestStateCollection>();
        if (playerRoomData.ContainsKey("consumableInventory"))
        {
            ConsumableInventory? inventory = JsonSerializer.Deserialize<ConsumableInventory>(
                playerRoomData.GetSFSObject("consumableInventory").ToJson());
            if (inventory?.inventoryMap is not null)
                session.Consumables = inventory.inventoryMap;
        }
        session.earnedRewards = JsonSerializer.Deserialize<Dictionary<string, long>>(data.GetSFSObject("earnedRewards").ToJson()) ?? new Dictionary<string, long>();
        session.IsLoggedIn = true;
        session.LastPongUtc = DateTime.UtcNow;
        session.IsDisconnected = false;
        
        session.SetUserVar(new SFSUserVariable("sess", session.SessionId.ToString()));
        session.SetUserVar(new SFSUserVariable("tube", playerRoomData.GetInt("selectedTubeId")));
        session.SetUserVar(new SFSUserVariable("colour", playerRoomData.GetSFSObject("profile").GetInt("colour")));
        session.SetUserVar(new SFSUserVariable("outfit", playerRoomData.GetSFSObject("outfit").ToJson()));

        foreach (var quest in session.Quest)
        {
            if (quest.status == 1)
            {
                session.ActivQuest = quest.questId;
                if (session.ActivQuest.Contains("DJC")) session.SetUserVar(new SFSUserVariable("on_quest", "DJCadence"));
                if (session.ActivQuest.Contains("RHC")) session.SetUserVar(new SFSUserVariable("on_quest", "Rockhopper")); 
                if (session.ActivQuest.Contains("AAC")) session.SetUserVar(new SFSUserVariable("on_quest", "AuntArctic"));
                if (session.ActivQuest.Contains("RKC")) session.SetUserVar(new SFSUserVariable("on_quest", "Rookie"));
            }
        }



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

        await SfsWire.WriteMessageAsync(session, controller: 0, action: 1, parameters: res);
        Console.WriteLine($"Login OK -> userId={userId} room='{room.Name}' sessionId={cpSessionId}");
        
        await RoomUtils.SendJoinRoom(room, "default", session);
    }

    private static Task HandleJoin(SFSObject req, Session session)
    {
        string? roomName = req.ContainsKey("n") ? req.GetUtfString("n") : null;

        if (roomName is null) throw new InvalidOperationException("Join room request missing room name");
        
        Room room = World.GetOrCrateRoom(roomName);
        
        return RoomUtils.SendJoinRoom(room, "default", session);
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
        session.LastPongUtc = DateTime.UtcNow;

        SFSObject res = new SFSObject();
        return SfsWire.WriteMessageAsync(session, controller: 0, action: (short)RequestType.PingPong, parameters: res);
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