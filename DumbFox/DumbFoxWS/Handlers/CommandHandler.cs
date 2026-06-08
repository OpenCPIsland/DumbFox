using System.Text.Json;
using DumbFoxWS.State;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;

namespace DumbFoxWS.Handlers;

public static class CommandHandler 
{
    public static async Task HandleCommandAsync(string command, string[] args, Session session)
    {
        await (command switch
        {
            ":ac" => AddCoins(session, args),
            ":help" => HelpCommand(session, args),
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

        await SfsWire.WriteMessageAsync(session.Socket!, 0, (short)RequestType.SetUserPosition, joinedPayload2);
        await SfsWire.WriteMessageAsync(session.Socket!, 0, 1000, joinedPayload);
        

        SFSObject resMsg = new SFSObject();
        SFSObject resData = new SFSObject();
		resMsg.PutLong("senderSessionId", 1L);
		resMsg.PutInt("emotion", 0);
		resMsg.PutBool("moderated", false);
		resMsg.PutUtfString("message", "Commands: !ac coins amount");
		resData.PutUtfString("msg", resMsg.ToJson());
		resData.PutInt("senderId", 1);

        await ExtensionHandler.SendExtensionResponse(session, "chat.msg", session.CurrentRoomId??0, resData);
    }
}