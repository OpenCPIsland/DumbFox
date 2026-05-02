using DumbFoxWS.State;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using System.Security.Cryptography;

namespace DumbFoxWS.Handlers;

public static class ExtensionHandler
{
	public static async Task HandleExtensionAsync(string command, int roomId, Session session, SFSObject parameters)
	{
		await (command switch
		{
			"time.get" => HandleTimeGet(session, roomId, parameters),
			"encryption.get" => HandleEncryptionGet(session, roomId, parameters),
			"l.a" => HandleLocomotionAction(session, roomId, parameters),
			"zone.logout" => HandleRoomLeave(session, roomId),
			"chat.msg" => HandleChatMessage(session, roomId, parameters),
			"chat.activity" => HandleChatActivity(session, roomId, parameters),
			"chat.activity_cancel" => HandleChatActivityCancel(session, roomId, parameters),
			"task.p" => HandleTaskPickup(session, roomId, parameters),
			"quest.c_obj" => HandleQuestCompleteObjective(session, roomId, parameters),
			"quest.set" => HandleQuestStateStates(session, roomId, parameters),
			"durable.equip" => HandleEquipDurable(session, roomId, parameters),
			"dispensable.equip" => HandleEquipDispensable(session, roomId, parameters),
			"con.use" => HandleConsumableUse(session, roomId, parameters),
			//after con.use is all not coded but see whats in parameters are
			"con.reuse" => HandleConsumableReuse(session, roomId, parameters),
			"con.set" => HandleConsumableInventorySet(session, roomId, parameters),
			"validate.state" => HandleGetSignedState(session, roomId, parameters),
			"reward.broadcast" => HandleSendEarnedRewards(session, roomId, parameters),
			"membership.refresh" => HandleMembershipRefresh(session, roomId, parameters),
			"proto" => HandlePrototype(session, roomId, parameters),
			"zone.player_location" => HandleGetPenguinLocation(session, roomId, parameters),
			"pgame.message" => HandlePartyGameMessage(session, roomId, parameters),
			"room.igloo_updated" => HandleIglooUpdate(session, roomId, parameters),
			//"wsevent.{0}" => HandleWebServerEvents(session, roomId, parameters), not sure how that works
			_ => Task.Run(() => Console.WriteLine($"Unhandled command: {command}"))
		});
	}

	private static async Task HandleTimeGet(Session session, int roomId, SFSObject parameters)
	{
		long ct = parameters.ContainsKey("ct") ? parameters.GetLong("ct") : 0L;

		long st = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

		SFSObject data = new SFSObject();
		data.PutLong("ct", ct);
		data.PutLong("st", st);

		await SendExtensionResponse(session, "time.get", roomId, data);
	}

	private static async Task HandleEncryptionGet(Session session, int roomId, SFSObject reqParams)
	{
		Console.WriteLine("Encryption request received: " + reqParams.ToJson());

		string pkmB64 = reqParams.GetUtfString("pkm");
		string pkeB64 = reqParams.GetUtfString("pke");

		byte[] modulus = Convert.FromBase64String(pkmB64);
		byte[] exponent = Convert.FromBase64String(pkeB64);

		byte[] aesKey = DumbfoxEncryption.RandomAes256Key();

		byte[] encrypted;
		using (RSA rsa = DumbfoxEncryption.BuildRsaFromModExp(modulus, exponent))
		{
			encrypted = DumbfoxEncryption.RsaEncryptPkcs1(rsa, aesKey);
		}

		string ekB64 = Convert.ToBase64String(encrypted);

		session.AesKey = aesKey;

		SFSObject resPayload = new SFSObject();
		resPayload.PutUtfString("ek", ekB64);

		await SendExtensionResponse(session, "encryption.get", roomId, resPayload);
	}

	private static async Task HandleLocomotionAction(Session sender, int roomId, SFSObject req)
	{
		SFSObject data = new SFSObject();

		data.PutFloatArray("p", req.GetFloatArray("p"));
		data.PutByte("a", req.GetByte("a"));

		data.PutInt("senderId", sender.UserId);
		data.PutLong("t", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

		if (req.ContainsKey("d"))
			data.PutFloatArray("d", req.GetFloatArray("d"));
		if (req.ContainsKey("v"))
			data.PutFloatArray("v", req.GetFloatArray("v"));
		if (req.ContainsKey("o"))
			data.PutUtfString("o", req.GetUtfString("o"));

		if (!World.RoomsById.TryGetValue(roomId, out Room? room))
		{
			await SendExtensionResponse(sender, "l.a", roomId, data);
			return;
		}

		foreach (KeyValuePair<int, Session> kv in room.Users)
		{
			await SendExtensionResponse(kv.Value, "l.a", roomId, data);
		}

		float[]? pos = req.GetFloatArray("p");
		if (pos is { Length: >= 3 })
		{
			sender.PosX = pos[0];
			sender.PosY = pos[1];
			sender.PosZ = pos[2];
		}
	}

	private static async Task HandleRoomLeave(Session session, int roomId)
	{
		World.RoomsById[roomId].Users.TryRemove(session.UserId, out _);

		SFSObject res = new SFSObject();
		res.PutText("zn", "ClubPenguin");
		await SfsWire.WriteMessageAsync(session.Socket!, 0, 2, res);

		SFSObject userCountPayload = new SFSObject();
		userCountPayload.PutInt("r", roomId);
		userCountPayload.PutShort("uc", (short)Math.Clamp(World.RoomsById[roomId].Users.Count, 0, short.MaxValue));

		SFSObject setUserPosition = new SFSObject();
		setUserPosition.PutInt("r", roomId);
		setUserPosition.PutIntArray("m", [session.UserId]);
		foreach (KeyValuePair<int, Session> ji in World.RoomsById[roomId].Users)
			await SfsWire.WriteMessageAsync(ji.Value.Socket!, 0, 30, setUserPosition);

		foreach (KeyValuePair<int, Session> ji in World.RoomsById[roomId].Users)
			await SfsWire.WriteMessageAsync(ji.Value.Socket!, 0, 1001, userCountPayload);


	}

	private static async Task HandleChatActivity(Session session, int roomId, SFSObject parameters)
	{

		SFSObject data = new SFSObject();
		data.PutInt("senderId", session.UserId);

		if (!World.RoomsById.TryGetValue(roomId, out Room? room)) return;

		foreach (KeyValuePair<int, Session> kv in room.Users)
		{
			Session target = kv.Value;
			await SendExtensionResponse(target, "chat.activity", roomId, data);
		}
	}

	private static async Task HandleChatActivityCancel(Session session, int roomId, SFSObject parameters)
	{
		SFSObject data = new SFSObject();
		data.PutInt("senderId", session.UserId);

		if (!World.RoomsById.TryGetValue(roomId, out Room? room)) return;

		foreach (KeyValuePair<int, Session> kv in room.Users)
		{
			Session target = kv.Value;
			await SendExtensionResponse(target, "chat.activity_cancel", roomId, data);
		}
	}
	private static async Task HandleChatMessage(Session sender, int roomId, SFSObject req)
	{
		Console.WriteLine($"[CHAT] Received from {sender.UserName}: {req.ToJson()}");

		if (sender.AesKey is null) return;
		string decrypted = DumbfoxEncryption.DecryptIvPrependedBase64(req.GetUtfString("msg"), sender.AesKey);
		Console.WriteLine($"[CHAT] Decrypted: {decrypted}");
		ISFSObject reqData = SFSObject.NewFromJsonData(decrypted).GetSFSObject("data");

		int emotion = reqData.GetInt("emotion");
		bool moderated = reqData.GetBool("moderated");
		string message = reqData.GetUtfString("message");

		if (message.StartsWith('!'))
		{
			string[] args = message.Split(" ");
			string cmd = args[0];
			args = args.Skip(1).ToArray();
			await CommandHandler.HandleCommandAsync(cmd, args, sender);
			return;
		}

		SFSObject resData = new SFSObject();
		SFSObject resMsg = new SFSObject();
		resMsg.PutLong("senderSessionId", sender.SessionId);
		resMsg.PutInt("emotion", emotion);
		resMsg.PutBool("moderated", moderated);
		resMsg.PutUtfString("message", message);
		resData.PutUtfString("msg", resMsg.ToJson());
		resData.PutInt("senderId", sender.UserId);

		if (!World.RoomsById.TryGetValue(roomId, out Room? room)) return;

		foreach (KeyValuePair<int, Session> kv in room.Users)
		{
			Session target = kv.Value;
			await SendExtensionResponse(target, "chat.msg", roomId, resData);
		}
	}

	private static async Task HandleTaskPickup(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Task Pickedup] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Task Pickedup] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
		SFSObject fakesignedresponse = new SFSObject();

		SFSObject collectivesBody = new SFSObject();
		SFSObject collectives = new SFSObject();
		collectives.PutLong("/Collectibles/Exploration Bits Container/SeaShells/explorationCollectible_SeaShell_FINAL", 1540286340239L);
		collectives.PutLong("/Collectibles/Exploration Bits Container/SeaShells/explorationCollectible_SeaShell_FINAL (24)", 1540286586641L);
		collectives.PutLong("/Collectibles/Exploration Bits Container/SeaShells/explorationCollectible_SeaShell_FINAL (23)", 1540636009143L);
		collectives.PutLong("/Collectibles/Exploration Bits Container/Seaweed/explorationCollectible_Seaweed (11)", 1539929560017L);
		collectives.PutLong("/Collectibles/Exploration Bits Container/SeaShells/explorationCollectible_SeaShell_FINAL (22)", 1540636009058L);
		collectives.PutLong("/Collectibles/Exploration Bits Container/SeaShells/explorationCollectible_SeaShell_FINAL (8)", 1540286587766L);
		collectives.PutLong("/Collectibles/Exploration Bits Container/Seaweed/explorationCollectible_Seaweed (7)", 1539929557830L);
		collectives.PutLong("/Collectibles/Exploration Bits Container/SeaShells/explorationCollectible_SeaShell_FINAL (7)", 1540286586046L);
		collectives.PutLong("/Collectibles/Exploration Bits Container/SeaShells/explorationCollectible_SeaShell_FINAL (6)", 1540286587907L);
		collectives.PutLong("/Collectibles/Exploration Bits Container/SeaShells/explorationCollectible_SeaShell_FINAL (1)", 1540285971405L);
		collectives.PutLong("/Collectibles/Exploration Bits Container/SeaShells/explorationCollectible_SeaShell_FINAL (25)", 1540636009416L);
		collectivesBody.PutUtfString("room", "Boardwalk");
		collectivesBody.PutSFSObject("collected", collectives);

		SFSObject reward = new SFSObject();

		fakesignedresponse.PutLong("goodUntil", 0L);
		fakesignedresponse.PutNull("signature");
		fakesignedresponse.PutNull("swid");
		fakesignedresponse.PutSFSObject("data", collectivesBody);
		reward.PutUtfString("reward", fakesignedresponse.ToJson());

		await SendExtensionResponse(session, "reward.room", roomId, reward);
	}
	private static async Task HandleQuestCompleteObjective(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Quest Complete Objectiv] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Quest Complete Objectiv] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
		SFSObject data = new SFSObject();
		SFSObject fakesignedresponse = new SFSObject();

		fakesignedresponse.PutLong("goodUntil", 0L);
		fakesignedresponse.PutNull("signature");
		fakesignedresponse.PutNull("swid");
		fakesignedresponse.PutUtfStringArray("data", []);

		data.PutUtfString("data", fakesignedresponse.ToJson());

		await SendExtensionResponse(session, "quest.s_obj", roomId, data);

		if (!World.RoomsById.TryGetValue(roomId, out Room? room)) return;

		foreach (KeyValuePair<int, Session> kv in room.Users)
		{
			Session target = kv.Value;
		}
	}

	private static async Task HandleQuestStateStates(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Quest Set States] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Quest Set States] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
		SFSObject data = new SFSObject();

		await SendExtensionResponse(session, "quest.set", roomId, data);

		if (!World.RoomsById.TryGetValue(roomId, out Room? room)) return;

		foreach (KeyValuePair<int, Session> kv in room.Users)
		{
			Session target = kv.Value;
		}
	}

	private static async Task HandleEquipDurable(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Equip Durable] Received from {session.UserName}: {parameters.ToJson()}");
		ISFSObject reqData2 = SFSObject.NewFromJsonData(parameters.GetUtfString("type"));


		Dictionary<int, string> gearNames = new Dictionary<int, string>
		{
			{11, "GearDrums"},
			{12, "GearFirstAidKit"},
			{13, "GearFishDogMultiTool"},
			{14, "GearGuitar"},
			{15, "GearJackhammer"},
			{16, "GearKeytar"},
			{17, "GearPirateKit"},
			{18, "GearScepter"},
			{19, "GearShield"},
			{20, "GearSword"},
			{66, "GearTrafficSign"},
			{71, "GearPomPomsBlue"},
			{72, "GearPomPomsRed"},
			{73, "GearGaryGear"},
			{77, "GearCPSNCamera"},
			{86, "GearDance"},
			{101, "AcousticGuitarGear"},
			{103, "MicrophoneGear"},
			{105, "BongoDrumsGear"},
			{129, "GearFlowerWand"},
			{154, "MedievalGear"}
		};

		gearNames.TryGetValue(reqData2.GetSFSObject("data").GetInt("propId"), out string? gearName);
		gearName ??= "GearSword";
		SFSObject data = new SFSObject();
		data.PutUtfString("name", gearName);
		data.PutUtfString("type", "durable");

		UserVariable con = new SFSUserVariable("con", data);
		await UserVarHandler.SendUserVarUpdate(session, con);
	}

	private static async Task HandleEquipDispensable(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Equip Dispensable] Received from {session.UserName}: {parameters.ToJson()}");

		Dictionary<int, string> gearNames = new Dictionary<int, string>
		{
   			{4, "Dance"},
   			{5, "Drums"},
 		    {7, "FirstAidKit"},
	        {8, "FishDogMultiTool"},
            {9, "FishingRod"},
            {43, "Sword"},
    		{21, "Guitar"},
    		{26, "Keytar"},
			{31, "PirateKit"},
    		{36, "ScepterProp"},
    		{37, "Shield"},
			{57, "JackHammer"},
			{70, "CPSNCamera"},
    		{74, "PomPomsRed"},
    		{75, "PomPomsBlue"},
    		{76, "GaryGear"},
    		{78, "YetiRescueKit"},
    		{100, "AcousticGuitar"},
    		{102, "Microphone"},
    		{104, "BongoDrums"},
			{154, "MedievalGear"}
		};

		int type = (int)parameters.GetLong("type");

		gearNames.TryGetValue(type, out string? gearName);
		gearName ??= "JackHammer";
		SFSObject data = new SFSObject();
		data.PutUtfString("name", gearName);
		data.PutUtfString("type", "durable");

		UserVariable con = new SFSUserVariable("con", data);
		await UserVarHandler.SendUserVarUpdate(session, con);
	}

private static async Task HandleConsumableUse(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Consumable use] Received from {session.UserName}: {parameters.ToJson()}");
		ISFSObject reqData2 = SFSObject.NewFromJsonData(parameters.GetUtfString("type"));
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Consumable use] WARN: The Shareables(group ones like PizzaBox etc) not working yet!!!!");
		Console.ResetColor();


		string cmd = reqData2.GetSFSObject("data").GetUtfString("type");

		SFSObject data = new SFSObject();
		data.PutInt("senderId", session.UserId);
		data.PutInt("partialCount", 3);
		data.PutUtfString("type", cmd);

		if (!World.RoomsById.TryGetValue(roomId, out Room? room)) return;

		foreach (KeyValuePair<int, Session> kv in room.Users)
		{
			Session target = kv.Value;
			await SendExtensionResponse(target, "con.use", roomId, data);
		}
	}

	private static async Task HandleConsumableReuse(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Consumable Reuse] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Consumable Reuse] WARN: The Shareables(group ones like PizzaBox etc) not working yet!!!!");
		Console.WriteLine($"[Consumable Reuse] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
	}

	private static async Task HandleConsumableInventorySet(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Consumable Inventory Set] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Consumable Inventory Set] WARN: The Shareables(group ones like PizzaBox etc) not working yet!!!!");
		Console.WriteLine($"[Consumable Inventory Set] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
	}

	private static async Task HandlePrototype(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Prototype] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Prototype] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
	}

	private static async Task HandleGetSignedState(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Get Signed State] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Get Signed State] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
	}

	private static async Task HandleMembershipRefresh(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Membership Refresh] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Membership Refresh] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
	}

	private static async Task HandleSendEarnedRewards(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Send Earned Rewards] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Send Earned Rewards] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
	}

	private static async Task HandleGetPenguinLocation(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Get Penguin Location] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Get Penguin Location] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
	}

	private static async Task HandlePartyGameMessage(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Party Game Message] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Party Game Message] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
	}

	private static async Task HandleIglooUpdate(Session session, int roomId, SFSObject parameters)
	{
		Console.WriteLine($"[Igloo Update] Received from {session.UserName}: {parameters.ToJson()}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[Igloo Update] WARN: Is not code properly/ or implated yet!!!!");
		Console.ResetColor();
	}

	public static async Task SendExtensionResponse(Session session, string cmd, int roomId, SFSObject payload)
	{
		SFSObject res = new SFSObject();
		res.PutUtfString("c", cmd);
		res.PutInt("r", roomId);
		res.PutSFSObject("p", payload);

		await SfsWire.WriteMessageAsync(session.Socket!, controller: 1, action: (short)RequestType.CallExtension, parameters: res);
	}
}
