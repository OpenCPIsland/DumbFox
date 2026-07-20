using Common.Utils;
using Microsoft.Extensions.Options;
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
    public static async Task HandleCommandAsync(string command, string[] args, Session session)
    {
        await (command switch
        {
            ":ac" => AddCoins(session, args),
            ":help" => HelpCommand(session, args),
            ":users" => GetUsersinRoom(session, args),
            ":mmoitem" => RoomStateTester(session, args),
            _ => Task.Run(() => Console.WriteLine($"Unhandled command: {command}"))
        });
    }

    private static async Task AddCoins(Session session, string[] args)
    {
        if (args.Length is > 1 or < 1)
        {
            Console.WriteLine("[CHAT] !ac requires a message argument");
            return;
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

		await ExtensionHandler.SendExtensionResponse(session, "reward.add", session.CurrentRoomId??0, data);

        
    }

    private static async Task GetUsersinRoom(Session session, string[] args)
    {
        int roomId = session.CurrentRoomId??0;
        if (!Common.Utils.World.RoomsById.TryGetValue(roomId, out var room))
        {
            Console.WriteLine($"Room {roomId} not found.");
        }

       // var err = new SFSObject();

       // err.PutShort("ec", (short)SFSErrorCode.LOGIN_BAD_ZONENAME);
      //  err.PutUtfString("em", $"Zone does not exist");

      //  var ep = new SFSArray();
      //  ep.AddUtfString("ClubPenguin");

       // err.PutSFSArray("ep", ep);


        Console.WriteLine($"In Current RoomId {roomId} are " + room.Users.Count.ToString() + " Users Online.");

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
            await SfsWire.WriteMessageAsync(target, 0, (short)RequestType.SetUserPosition, joinedPayload2);
            await SfsWire.WriteMessageAsync(target, 0, 1000, joinedPayload);
            await ExtensionHandler.SendExtensionResponse(target, "chat.msg", session.CurrentRoomId??0, resData);
        }

        //await SfsWire.WriteMessageAsync(session, 0, (short)RequestType.Login, err);
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
        AddVariable(variables,"path", args[2]);// /Minigames/AquaFunHut/GeyserLocations/GeyserLocation02
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
        joinedPayload.PutInt("r", session.CurrentRoomId??0);
        joinedPayload.PutSFSArray("u", u);

        SFSArray u3 = new SFSArray();
        u3.AddSFSArray(u);
        SFSObject joinedPayload2 = new SFSObject();
        joinedPayload2.PutSFSArray("p", u3);
        joinedPayload2.PutInt("r", session.CurrentRoomId??0);

        await SfsWire.WriteMessageAsync(session, 0, (short)RequestType.SetUserPosition, joinedPayload2);
        await SfsWire.WriteMessageAsync(session, 0, 1000, joinedPayload);
        

        SFSObject resMsg = new SFSObject();
        SFSObject resData = new SFSObject();
		resMsg.PutLong("senderSessionId", 1L);
		resMsg.PutInt("emotion", 0);
		resMsg.PutBool("moderated", false);
		resMsg.PutUtfString("message", "Commands: :ac coins amount");
		resData.PutUtfString("msg", resMsg.ToJson());
		resData.PutInt("senderId", 1);

        await ExtensionHandler.SendExtensionResponse(session, "chat.msg", session.CurrentRoomId??0, resData);
    }
}