using Common.Utils;
using Microsoft.Extensions.Options;
using Sfs2X.Bitswarm;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Exceptions;
using Sfs2X.Requests;
using Sfs2X.Util;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DumbFox.Handlers;

public static class CommandHandler
{
    private static int _restartScheduled;

    public static async Task HandleCommandAsync(string command, string[] args, Session session)
    {
        await (command switch
        {
            ":ac" => AddCoins(session, args),
            ":help" => HelpCommand(session, args),
            ":users" => GetUsersinRoom(session, args),
            ":mmoitem" => RoomStateTester(session, args),
            ":admin-alert" => Alert(session, args),
            ":admin-swre-restart" => AdminScheduledRestart(session, args),
            _ => Task.Run(() => Console.WriteLine($"Unhandled command: {command}"))
        });
    }

    private static async Task AdminScheduledRestart(Session session, string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine($"[SERVER RESTART] Invalid countdown from {session.UserName}. Usage: :admin-swre-restart <seconds>");
            return;
        }

        string secondsText = args[0].Trim();
        if (secondsText.EndsWith('s') || secondsText.EndsWith('S'))
            secondsText = secondsText[..^1];

        if (!int.TryParse(secondsText, out int seconds) || seconds < 1)
        {
            Console.WriteLine($"[SERVER RESTART] Invalid countdown from {session.UserName}. Seconds must be a positive integer.");
            return;
        }

        if (Interlocked.CompareExchange(ref _restartScheduled, 1, 0) != 0)
        {
            Console.WriteLine($"[SERVER RESTART] Ignored duplicate countdown request from {session.UserName}; restart already scheduled.");
            return;
        }

        await ExtensionHandler.BroadcastServerRestartMessageAsync();
        Console.WriteLine($"[SERVER RESTART] {session.UserName} scheduled FrontEnd shutdown in {seconds} seconds.");
        _ = RestartAfterDelayAsync(seconds, session.UserName);
    }

    private static async Task RestartAfterDelayAsync(int seconds, string requestedBy)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds));

        Action? requestShutdown = World.RequestShutdown;
        if (requestShutdown is null)
        {
            Interlocked.Exchange(ref _restartScheduled, 0);
            Console.WriteLine("[SERVER RESTART] Cannot stop FrontEnd: no shutdown callback is registered.");
            return;
        }

        Console.WriteLine($"[SERVER RESTART] Countdown completed (requested by {requestedBy}); stopping FrontEnd.");
        requestShutdown();
    }

    private static async Task Alert(Session session, string[] args)
    {
        /*if (!World.AlertCommandUserIds.Contains(session.UserId))
        {
            Console.WriteLine($"[SERVER ALERT] Rejected unauthorized request from {session.UserName} (user {session.UserId}).");
            return;
        }*/

        string message = string.Join(" ", args).Trim();
        SFSObject payloada = new();
        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine($"[SERVER ALERT] {session.UserName} used :admin-alert without message text.");
            payloada.PutUtfString("message", ":admin-alert requires message text.");
            await ExtensionHandler.SendExtensionResponse(session, "server.alert", session.CurrentRoomId ?? 0, payloada);
            return;
        }

        if (message.Length > 500)
            message = message[..500];

        await ExtensionHandler.BroadcastServerAlertAsync(message);
        Console.WriteLine($"[SERVER ALERT] Broadcast from {session.UserName}: {message}");
    }

    private static async Task AddCoins(Session session, string[] args)
    {
        SFSObject payloada = new();
        if (args.Length is > 1 or < 1)
        {
            Console.WriteLine("[CHAT] :ac requires a message argument");
            payloada.PutUtfString("message", ":ac requires a message argument");
            await ExtensionHandler.SendExtensionResponse(session, "server.alert", session.CurrentRoomId ?? 0, payloada);
            return;
        }

        int roomId = session.CurrentRoomId ?? 0;
        if (!Common.Utils.World.RoomsById.TryGetValue(roomId, out var room))
        {
            Console.WriteLine($"Room {roomId} not found.");
            payloada.PutUtfString("message", $"Room {roomId} not found.");
            await ExtensionHandler.SendExtensionResponse(session, "server.alert", session.CurrentRoomId ?? 0, payloada);
        }

        SFSObject data = new SFSObject();
        SFSObject data2 = new SFSObject();
        SFSObject fakecurrency = new SFSObject();
        SFSObject fakereward = new SFSObject();
        SFSObject fakerewards = new SFSObject();
        SFSObject fakesignedresponse = new SFSObject();

        fakecurrency.PutInt("coins", int.Parse(args[0]));
        fakereward.PutInt("coins", int.Parse(args[0]));
        fakereward.PutSFSObject("currency", fakecurrency);
        /*  fakereward.PutNull("mascotXP");
          fakereward.PutNull("collectibleCurrency");
          fakereward.PutNull("colourPacks");
          fakereward.PutNull("decals");
          fakereward.PutNull("fabrics");
          fakereward.PutNull("emotePacks");
          fakereward.PutNull("sizzleClips");
          fakereward.PutNull("equipmentTemplates");
          fakereward.PutNull("equipmentInstances");
          fakereward.PutNull("lots");
          fakereward.PutNull("decorationInstances");
          fakereward.PutNull("structureInstances");
          fakereward.PutNull("decorationPurchaseRights");
          fakereward.PutNull("structurePurchaseRights");
          fakereward.PutNull("musicTracks");
          fakereward.PutNull("lighting");
          fakereward.PutNull("durables");
          fakereward.PutNull("tubes");
          fakereward.PutInt("savedOutfitSlots", 0);
          fakereward.PutInt("iglooSlots", 0);
          fakereward.PutNull("consumables");
          fakereward.PutNull("partySupplies");*/

        data2.PutSFSObject(session.SessionId.ToString(), fakereward);

        fakerewards.PutInt("source", 5);
        fakerewards.PutUtfString("sourceId", session.UserId.ToString());
        fakerewards.PutSFSObject("rewards", data2);

        fakesignedresponse.PutLong("goodUntil", 0L);
        fakesignedresponse.PutNull("signature");
        fakesignedresponse.PutNull("swid");
        fakesignedresponse.PutSFSObject("data", fakerewards);

        data.PutInt("senderId", session.UserId);
        data.PutUtfString("reward", fakesignedresponse.ToJson());


        if (!World.RoomsById.TryGetValue(session.CurrentRoomId ?? 0, out Common.Utils.Room? room2))
        {
            Console.WriteLine($"Room {roomId} not found.");
            return;
        }

        foreach (KeyValuePair<int, Session> kv in room2.Users)
        {
            Session target = kv.Value;
            Console.WriteLine($"Room {target} not found.");
            Console.WriteLine($"Room {kv.Value} not found.");
            await ExtensionHandler.SendExtensionResponse(target, "reward.add", session.CurrentRoomId ?? 0, data);
        }


    }

    private static async Task GetUsersinRoom(Session session, string[] args)
    {
        int roomId = session.CurrentRoomId ?? 0;
        SFSObject payloada = new();
        if (!Common.Utils.World.RoomsById.TryGetValue(roomId, out var room))
        {
            Console.WriteLine($"Room {roomId} not found.");
            payloada.PutUtfString("message", $"Room {roomId} not found.");
            await ExtensionHandler.SendExtensionResponse(session, "server.alert", session.CurrentRoomId ?? 0, payloada);
        }

        Console.WriteLine($"In Current RoomId {roomId} are " + room.Users.Count.ToString() + " Users Online.");

        string message = $"In Current RoomId {roomId} are " + room.Users.Count.ToString() + " Users Online.";
        SFSObject payload = new();
        payload.PutUtfString("message", message);
        await ExtensionHandler.SendExtensionResponse(session, "server.alert", session.CurrentRoomId ?? 0, payload);
    }

    private static async Task RoomStateTester(Session session, string[] args)
    {
        SFSObject joinedPayload2 = new SFSObject();
        joinedPayload2.PutInt("r", session.CurrentRoomId ?? 0);

        SFSArray u3 = new SFSArray();
        SFSArray u = new SFSArray();

        u.AddUtfString("statetimestamp");
        u.AddByte(4);
        u.AddUtfString(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        u.AddBool(false);

        SFSArray u2 = new SFSArray();
        u2.AddUtfString("scheduledworldobjectstate");
        u2.AddByte(2);
        u2.AddInt(int.Parse(args[0])); // 0 1 2
        u2.AddBool(false);

        u3.AddSFSArray(u);
        u3.AddSFSArray(u2);

        joinedPayload2.PutSFSArray("v", u3);
        joinedPayload2.PutInt("i", int.Parse(args[1])); //16887

        SFSObject payload = new SFSObject();
        SFSArray items = new SFSArray();
        SFSArray itemData = new SFSArray();
        itemData.AddInt(int.Parse(args[1]));
        SFSArray variables = new SFSArray();
        AddVariable(variables, "path", args[2]);// /Minigames/AquaFunHut/GeyserLocations/GeyserLocation02
        AddVariable(variables, "creator", -1);
        AddVariable(variables, "statetimestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        AddVariable(variables, "position", new float[] { session.PosX, session.PosY, session.PosZ });
        AddVariable(variables, "category", 2);
        AddVariable(variables, "scheduledworldobjectstate", int.Parse(args[0]));
        itemData.AddSFSArray(variables);
        itemData.AddFloatArray([session.PosX, session.PosY, session.PosZ]);
        items.AddSFSArray(itemData);

        payload.PutInt("r", session.CurrentRoomId ?? 0);
        payload.PutSFSArray("q", items);

        if (!World.RoomsById.TryGetValue(session.CurrentRoomId ?? 0, out Common.Utils.Room? room))
        {
            return;
        }

        foreach (KeyValuePair<int, Session> kv in room.Users)
        {
            Session target = kv.Value;
            await SfsWire.WriteMessageAsync(target, 0, (short)30, payload);
            await SfsWire.WriteMessageAsync(target, 0, (short)1007, joinedPayload2);
        }
    }

    private static void AddVariable(
    SFSArray array,
    string name,
    object value)
    {
        SFSArray variable = new SFSArray();

        variable.AddUtfString(name);

        switch (value)
        {
            case int i:
                variable.AddByte(2);
                variable.AddInt(i);
                break;

            case string s:
                variable.AddByte(4);
                variable.AddUtfString(s);
                break;

            case float[] f:
                variable.AddByte(6);
                SFSArray inner = new SFSArray();
                inner.AddFloatArray(f);
                variable.AddSFSArray(inner);
                break;
        }

        variable.AddBool(false);

        array.AddSFSArray(variable);
    }

    private static async Task HelpCommand(Session session, string[] args)
    {
        string message = "Commands:\n :ac coins amount \n:users shows how many are online in current room you are in not whole server.";
        SFSObject payload = new();
        payload.PutUtfString("message", message);
        await ExtensionHandler.SendExtensionResponse(session, "server.alert", session.CurrentRoomId ?? 0, payload);
    }
}
/* can be still use  full for player spawns
        SFSArray u = new SFSArray();
        u.AddInt(1);
        u.AddUtfString("Bot1");
        u.AddShort(0);
        u.AddShort(0);
        SFSArray u2 = new SFSArray();
        SFSArray usess = new SFSArray();
        SFSArray ucolor = new SFSArray();
        SFSArray utube = new SFSArray();
        SFSArray uoutfit = new SFSArray();

        usess.AddUtfString("sess");
        usess.AddByte(4);
        usess.AddUtfString("1");
        usess.AddBool(false);
        ucolor.AddUtfString("colour");
        ucolor.AddByte(2);
        ucolor.AddInt(0);
        ucolor.AddBool(false);
        utube.AddUtfString("tube");
        utube.AddByte(2);
        utube.AddInt(0);
        utube.AddBool(false);
        uoutfit.AddUtfString("outfit");
        uoutfit.AddByte(4);
        uoutfit.AddUtfString("{\"parts\":[]}");
        uoutfit.AddBool(false);

        u2.AddSFSArray(usess);
        u2.AddSFSArray(ucolor);
        u2.AddSFSArray(utube);
        u2.AddSFSArray(uoutfit);

        u.AddSFSArray(u2);
        u.AddFloatArray([session.PosX, session.PosY, session.PosZ]);

        SFSObject joinedPayload = new SFSObject();
        joinedPayload.PutInt("r", session.CurrentRoomId ?? 0);
        joinedPayload.PutSFSArray("u", u);

        SFSArray u3 = new SFSArray();
        u3.AddSFSArray(u);
        SFSObject joinedPayload2 = new SFSObject();
        joinedPayload2.PutSFSArray("p", u3);
        joinedPayload2.PutInt("r", session.CurrentRoomId ?? 0);

    //    await SfsWire.WriteMessageAsync(session, 0, (short)RequestType.SetUserPosition, joinedPayload2);
     //   await SfsWire.WriteMessageAsync(session, 0, 1000, joinedPayload);


        SFSObject resMsg = new SFSObject();
        SFSObject resData = new SFSObject();
        resMsg.PutLong("senderSessionId", 1L);
        resMsg.PutInt("emotion", 0);
        resMsg.PutBool("moderated", false);
        resMsg.PutUtfString("message", $"In Current RoomId {roomId} are " + room.Users.Count.ToString()+ " Users Online.");
        resData.PutUtfString("msg", resMsg.ToJson());
        resData.PutInt("senderId", 1);

        //await ExtensionHandler.SendExtensionResponse(session, "chat.msg", session.CurrentRoomId ?? 0, resData);


        if (!World.RoomsById.TryGetValue(session.CurrentRoomId??0, out Common.Utils.Room? room2))
        {
            Console.WriteLine($"Room {roomId} not found.");
            return;
        }
        

        foreach (KeyValuePair<int, Session> kv in room2.Users)
        {
            Session target = kv.Value;
            Console.WriteLine($"Room {target} not found.");
            Console.WriteLine($"Room {kv.Value} not found.");
          //  await SfsWire.WriteMessageAsync(target, 0, (short)RequestType.SetUserPosition, joinedPayload2);
         //   await SfsWire.WriteMessageAsync(target, 0, 1000, joinedPayload);
        //    await ExtensionHandler.SendExtensionResponse(target, "chat.msg", session.CurrentRoomId??0, resData);
        }

        //await SfsWire.WriteMessageAsync(session, 0, (short)RequestType.Login, err);
*/