using Common.Utils;
using Common.Utils.Game;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DumbFox.Handlers;

public static class ExtensionHandler
{
    private static long _lastPartyBlasterId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
    
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

    private static string GetBaseRoomName(string roomName)
    {
        string clean = roomName.Split("::")[0];
        string[] parts = clean.Split(':');

        if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
            return parts[2];

        return parts[0];
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
        SFSObject data = new();

        float[]? pos = req.GetFloatArray("p");



        data.PutFloatArray("p", pos);
        data.PutByte("a", req.GetByte("a"));

        data.PutInt("senderId", sender.UserId);
        data.PutLong("t", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        if (!World.RoomsById.TryGetValue(roomId, out Room? room2)) return;
        string currentRoom = GetBaseRoomName(room2.Name);

        if (req.ContainsKey("d"))
            data.PutFloatArray("d", req.GetFloatArray("d"));

        if (req.ContainsKey("v"))
            data.PutFloatArray("v", req.GetFloatArray("v"));

        InteractionData? playerInteraction = null;
        if (req.ContainsKey("o"))
        {
            data.PutUtfString("o", req.GetUtfString("o"));
            string oJson = req.GetUtfString("o");
            var interaction = JsonSerializer.Deserialize<InteractionData>(oJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            playerInteraction = interaction;

            Console.WriteLine("[Task Pickedup] Received id: " + playerInteraction.Id+ " Type: "+ playerInteraction.Type + " Tag: "+ playerInteraction.Tag);

            if (interaction != null)
            {
                var pickup = PickupableManager.Pickupables
                    .FirstOrDefault(x => string.Equals(x.Path, interaction.Id, StringComparison.OrdinalIgnoreCase));

                if (pickup != null && pos is { Length: >= 25 })
                {
                    if (IsNearPickup(pos, pickup))
                    {
                        SFSObject fakesignedresponse = new SFSObject();
                        SFSObject collectivesBody = new SFSObject();
                        string taskId = interaction.Id;
                        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        sender.earnedRewards[taskId] = timestamp;

                        SFSObject collectives = new SFSObject();
                        foreach (var kvp in sender.earnedRewards)
                        {
                            collectives.PutLong(kvp.Key, kvp.Value);
                        }

                        collectivesBody.PutUtfString("room", currentRoom);
                        collectivesBody.PutSFSObject("collected", collectives);

                        SFSObject reward = new SFSObject();

                        fakesignedresponse.PutLong("goodUntil", 0L);
                        fakesignedresponse.PutNull("signature");
                        fakesignedresponse.PutNull("swid");
                        fakesignedresponse.PutSFSObject("data", collectivesBody);

                        reward.PutUtfString("reward", fakesignedresponse.ToJson());

                        await SendExtensionResponse(sender, "reward.room", roomId, reward);
                        Console.WriteLine("[Task Pickedup] Received from " + sender.UserName + ": {\"id\":\"" + pickup.Path + "\",\"t\":\"" + pickup.Tag + "\",\"p\":[" + pos[0] + "," + pos[1] + "," + pos[2] + "]}");
                    }
                }
            }
        }

        if (!World.RoomsById.TryGetValue(roomId, out Room? room))
        {
            await SendExtensionResponse(sender, "l.a", roomId, data);
            return;
        }

        if (pos is { Length: >= 25 })
        {
            sender.PosX = pos[0];
            sender.PosY = pos[1];
            sender.PosZ = pos[2];
        }

        if (req.GetByte("a") == (byte)GameLocomotionAction.Interact
            && playerInteraction?.Type == (int)InteractionObjectType.Player)
        {
            Console.WriteLine($"[Consumable interaction] Player {sender.UserName} interacted with player id={playerInteraction.Id} in room {roomId}.");
            if (playerInteraction.Id == sender.SessionId.ToString())
                await HandleServerAddedConsumableUse(sender, room, playerInteraction);
            else
                await HandleShareableConsumableInteraction(sender, room, playerInteraction);
        }

        foreach (Session session in room.Users.Values)
            await SendExtensionResponse(session, "l.a", roomId, data);

    }
    private static bool IsNearPickup(float[] pos, PickupableData pickup)
    {
        float dx = pos[0] - pickup.MinBounds.x;
        float dy = pos[1] - pickup.MinBounds.y;
        float dz = pos[2] - pickup.MinBounds.z;

        return dx * dx + dy * dy + dz * dz <= 4.0f; // 2 unit radius
    }

    private static async Task HandleRoomLeave(Session session, int roomId)
	{
        World.RoomsById[roomId].Users.TryRemove(session.UserId, out _);

        SFSObject res = new SFSObject();
		res.PutText("zn", "ClubPenguin");
		await SfsWire.WriteMessageAsync(session, 0, 2, res);

		SFSObject userCountPayload = new SFSObject();
		userCountPayload.PutInt("r", roomId);
		userCountPayload.PutShort("uc", (short)Math.Clamp(World.RoomsById[roomId].Users.Count, 0, short.MaxValue));

		SFSObject setUserPosition = new SFSObject();
		setUserPosition.PutInt("r", roomId);
		setUserPosition.PutIntArray("m", [session.UserId]);
		foreach (KeyValuePair<int, Session> ji in World.RoomsById[roomId].Users)
			await SfsWire.WriteMessageAsync(ji.Value, 0, 30, setUserPosition);

		foreach (KeyValuePair<int, Session> ji in World.RoomsById[roomId].Users)
			await SfsWire.WriteMessageAsync(ji.Value, 0, 1001, userCountPayload);

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

		string decrypted = DumbfoxEncryption.DecryptIvPrependedBase64(req.GetUtfString("msg"), sender.AesKey);
		Console.WriteLine($"[CHAT] Decrypted: {decrypted}");
		ISFSObject reqData = SFSObject.NewFromJsonData(decrypted).GetSFSObject("data");

		int emotion = reqData.GetInt("emotion");
		bool moderated = reqData.GetBool("moderated");
		string message = reqData.GetUtfString("message");

		if (message.StartsWith(':'))
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
        SFSObject fakesignedresponse = new SFSObject();
        SFSObject collectivesBody = new SFSObject();
        string taskId = parameters.GetUtfString("id");
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        session.earnedRewards[taskId] = timestamp;

        SFSObject collectives = new SFSObject();
        foreach (var kvp in session.earnedRewards)
        {
            collectives.PutLong(kvp.Key, kvp.Value);
        }

        if (!World.RoomsById.TryGetValue(roomId, out Room? room)) return;
        string currentRoom = GetBaseRoomName(room.Name);

        collectivesBody.PutUtfString("room", currentRoom);
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

        foreach (var quest in session.Quest)
        {
            if (quest.status == 1)
            {
                quest.completedObjectives =
				(quest.completedObjectives ?? Array.Empty<string>())
				.Append(parameters.GetUtfString("obj"))
				.ToArray();
                fakesignedresponse.PutUtfStringArray("data", quest.completedObjectives);
            }
        }


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
		SFSObject data = new SFSObject();

        /*string json = parameters.GetUtfString("data");

        var response = JsonSerializer.Deserialize<QuestSetResponse>(json);

        if (response?.data != null)
        {
            foreach (var incomingQuest in response.data)
            {
                var existingQuest = session.Quest
                    .FirstOrDefault(q => q.questId == incomingQuest.questId);

                if (existingQuest != null)
                {
                    existingQuest.status = incomingQuest.status;
                    existingQuest.completedObjectives = incomingQuest.completedObjectives;
                }
            }
        }*/

        string json = parameters.GetUtfString("data");
        using JsonDocument doc = JsonDocument.Parse(json);

        var incomingQuests = JsonSerializer.Deserialize<List<QuestStateCollection>>(
            doc.RootElement.GetProperty("data").GetRawText()
        );

        if (incomingQuests != null)
        {
            foreach (var incomingQuest in incomingQuests)
            {
                var existingQuest = session.Quest
                    .FirstOrDefault(q => q.questId == incomingQuest.questId);

                if (existingQuest != null)
                {
                    existingQuest.status = incomingQuest.status;
                    existingQuest.completedObjectives = incomingQuest.completedObjectives;
                    existingQuest.unlockTime = incomingQuest.unlockTime;
                    existingQuest.timesCompleted = incomingQuest.timesCompleted;

                    if (existingQuest.status == 1)
                    {
                        session.ActivQuest = existingQuest.questId;
                        if (session.ActivQuest.Contains("DJC")) session.SetUserVar(new SFSUserVariable("on_quest", "DJCadence"));
                        if (session.ActivQuest.Contains("RHC")) session.SetUserVar(new SFSUserVariable("on_quest", "Rockhopper"));
                        if (session.ActivQuest.Contains("AAC")) session.SetUserVar(new SFSUserVariable("on_quest", "AuntArctic")); 
                        if (session.ActivQuest.Contains("RKC")) session.SetUserVar(new SFSUserVariable("on_quest", "Rookie"));
					}
					else
					{
                        session.SetUserVar(new SFSUserVariable("on_quest", null));
                    }
                }
                else
                {

                    session.Quest.Add(incomingQuest);

                    if (incomingQuest.status == 1)
                    {
                        session.ActivQuest = incomingQuest.questId;
                        if (session.ActivQuest.Contains("DJC")) session.SetUserVar(new SFSUserVariable("on_quest", "DJCadence"));
                        if (session.ActivQuest.Contains("RHC")) session.SetUserVar(new SFSUserVariable("on_quest", "Rockhopper"));
                        if (session.ActivQuest.Contains("AAC")) session.SetUserVar(new SFSUserVariable("on_quest", "AuntArctic"));
                        if (session.ActivQuest.Contains("RKC")) session.SetUserVar(new SFSUserVariable("on_quest", "Rookie"));
                    }
                }
            }
        }

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
		await UserVarHandler.SendUserVarConUpdate(session, con, data.GetInt("propId"), gearName);
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
        await UserVarHandler.SendUserVarConUpdate(session, con, type, gearName);
    }

    private static async Task HandleConsumableUse(Session session, int roomId, SFSObject parameters)
    {
        Console.WriteLine($"[Consumable use] Received from {session.UserName}: {parameters.ToJson()}");
        ISFSObject request = SFSObject.NewFromJsonData(parameters.GetUtfString("type"));
        ISFSObject useData = request.GetSFSObject("data");
        string type = useData.GetUtfString("type");
        string? authorizationToken = request.ContainsKey("signature") ? request.GetUtfString("signature") : null;
        bool isSharedProp = ShareableConsumables.TryGet(type, out ShareableConsumableDefinition definition)
            && !string.IsNullOrEmpty(definition.RecipientType);

        SFSObject data = new SFSObject();
        data.PutInt("senderId", session.UserId);
        data.PutUtfString("type", isSharedProp ? session.SessionId.ToString() : type);

        if (!World.RoomsById.TryGetValue(roomId, out Room? room)) return;

        if (PartyBlasters.TryGet(type, out PartyBlasterDefinition partyBlaster))
        {
            bool authorized;
            if (!string.IsNullOrEmpty(authorizationToken))
            {
                authorized = await DatabaseManager.ConsumeConsumableUseAuthorization(session.Swid, type, authorizationToken);
            }
            else
            {
                await session.ConsumableGate.WaitAsync();
                try
                {
                    authorized = session.Consumables.TryGetValue(type, out InventoryItemStock? stock)
                        && stock.GetItemCount() > 0;
                    if (authorized)
                    {
                        if (stock!.partialCount > 0)
                            stock.partialCount = 0;
                        else
                            stock.itemCount--;
                    }
                }
                finally
                {
                    session.ConsumableGate.Release();
                }
            }

            if (!authorized)
            {
                Console.WriteLine($"[Party blaster] Rejected {type} for {session.UserName}: no inventory stock or invalid authorization.");
                return;
            }

            long experienceId = Interlocked.Increment(ref _lastPartyBlasterId);
            data.PutUtfString("type", experienceId.ToString());
            foreach (Session target in room.Users.Values)
                await SendExtensionResponse(target, "con.use", roomId, data);

            (float x, float y, float z) = GetPartyBlasterPosition(session, parameters);
            _ = CompletePartyBlasterAsync(roomId, session.UserId, experienceId, x, y, z, partyBlaster);
            return;
        }

        if (isSharedProp)
        {
            await session.ConsumableGate.WaitAsync();
            try
            {
                if (session.ShareableConsumableCount > 0)
                {
                    Console.WriteLine($"[Consumable use] Rejected {type} for {session.UserName}: another shared consumable is active.");
                    return;
                }

                if (!session.Consumables.TryGetValue(type, out InventoryItemStock? stock)
                    || stock.GetItemCount() <= 0)
                {
                    Console.WriteLine($"[Consumable use] Rejected {type} for {session.UserName}: no inventory stock.");
                    return;
                }

                if (!string.IsNullOrEmpty(authorizationToken)
                    && !await DatabaseManager.ConsumeConsumableUseAuthorization(session.Swid, type, authorizationToken))
                {
                    Console.WriteLine($"[Consumable use] Rejected {type} for {session.UserName}: invalid or expired authorization.");
                    return;
                }

                int servings = stock.partialCount is > 0 && stock.partialCount <= definition.ActionCount
                    ? stock.partialCount
                    : definition.ActionCount;
                if (stock.partialCount > 0)
                    stock.partialCount = 0;
                else
                    stock.itemCount--;

                session.ActiveShareableConsumable = type;
                session.ShareableConsumableCount = servings;
                await UserVarHandler.SendShareableConsumableCount(session, servings);
            }
            finally
            {
                session.ConsumableGate.Release();
            }
        }

        foreach (Session target in room.Users.Values)
            await SendExtensionResponse(target, "con.use", roomId, data);
    }

    private static (float X, float Y, float Z) GetPartyBlasterPosition(Session session, SFSObject parameters)
    {
        if (parameters.ContainsKey("prop"))
        {
            try
            {
                using JsonDocument prop = JsonDocument.Parse(parameters.GetUtfString("prop"));
                JsonElement root = prop.RootElement;
                return (
                    root.GetProperty("x").GetSingle(),
                    root.GetProperty("y").GetSingle(),
                    root.GetProperty("z").GetSingle());
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
            {
                Console.WriteLine($"[Party blaster] Invalid placement from {session.UserName}: {ex.Message}; using the player's current position.");
            }
        }

        return (session.PosX, session.PosY, session.PosZ);
    }

    private static async Task CompletePartyBlasterAsync(
        int roomId,
        int ownerId,
        long experienceId,
        float x,
        float y,
        float z,
        PartyBlasterDefinition definition)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(definition.TimeToLiveSeconds));
            if (World.RoomsById.TryGetValue(roomId, out Room? room))
                await AwardPartyBlaster(room, ownerId, experienceId, x, y, z, definition);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Party blaster] Failed to process reward for item {experienceId}: {ex}");
        }
    }

    private static async Task AwardPartyBlaster(
        Room room,
        int ownerId,
        long experienceId,
        float x,
        float y,
        float z,
        PartyBlasterDefinition definition)
    {
        Session[] recipients = room.Users.Values
            .Where(user =>
            {
                float dx = user.PosX - x;
                float dy = user.PosY - y;
                float dz = user.PosZ - z;
                return dx * dx + dy * dy + dz * dz <= definition.RewardRadius * definition.RewardRadius;
            })
            .GroupBy(user => user.Swid, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(user => user.SessionId)
            .ToArray();

        if (recipients.Length == 0)
            return;

        int baseCoins = Math.Max(1, definition.TotalCoins / recipients.Length);
        int remainder = Math.Max(0, definition.TotalCoins - baseCoins * recipients.Length);
        Dictionary<Session, int> coinAwards = recipients
            .Select((recipient, index) => new
            {
                Session = recipient,
                Coins = baseCoins + (index < remainder ? 1 : 0)
            })
            .ToDictionary(item => item.Session, item => item.Coins);

        Dictionary<long, long> balances = await DatabaseManager.AwardPartyBlasterRewards(
            coinAwards.Select(award => (
                award.Key.Swid,
                award.Key.SessionId,
                award.Key.InitialCoins,
                (long)award.Value)));

        foreach (KeyValuePair<Session, int> award in coinAwards)
        {
            string reward = JsonSerializer.Serialize(new
            {
                goodUntil = 0L,
                swid = (string?)null,
                data = new
                {
                    source = 1,
                    sourceId = experienceId.ToString(),
                    rewards = new Dictionary<string, object>
                    {
                        [award.Key.SessionId.ToString()] = new { currency = new { coins = award.Value } }
                    }
                },
                signature = (string?)null,
                assets = new
                {
                    coins = checked((int)balances[award.Key.SessionId])
                }
            });

            SFSObject payload = new SFSObject();
            payload.PutInt("senderId", ownerId);
            payload.PutUtfString("reward", reward);
            await SendExtensionResponse(award.Key, "reward.add", room.RoomId, payload);
        }

        Console.WriteLine($"[Party blaster] Credited and notified {recipients.Length} player(s) for item {experienceId}.");
    }

    private static async Task HandleConsumableReuse(Session session, int roomId, SFSObject parameters)
    {
        Console.WriteLine($"[Consumable Reuse] Received from {session.UserName}: {parameters.ToJson()}");
        if (!parameters.ContainsKey("type") || !parameters.ContainsKey("prop"))
        {
            Console.WriteLine($"[Consumable Reuse] Rejected malformed request from {session.UserName}.");
            return;
        }

        SFSObject response = new SFSObject();
        response.PutUtfString("type", parameters.GetUtfString("type"));
        response.PutUtfString("prop", parameters.GetUtfString("prop"));
        await SendExtensionResponse(session, "con.reuse_error", roomId, response);
    }

    private static async Task HandleConsumableInventorySet(Session session, int roomId, SFSObject parameters)
    {
        Console.WriteLine($"[Consumable Inventory Set] Received from {session.UserName}: {parameters.ToJson()}");
        if (!parameters.ContainsKey("data"))
        {
            Console.WriteLine($"[Consumable Inventory Set] Rejected request from {session.UserName}: missing inventory data.");
            return;
        }

        Dictionary<string, InventoryItemStock> inventory;
        try
        {
            SignedResponse<ConsumableInventory>? response = JsonSerializer.Deserialize<SignedResponse<ConsumableInventory>>(
                parameters.GetUtfString("data"));
            inventory = response?.data?.inventoryMap
                ?? throw new JsonException("Consumable inventory map is missing.");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[Consumable Inventory Set] Rejected malformed inventory from {session.UserName}: {ex.Message}");
            return;
        }

        foreach (InventoryItemStock stock in inventory.Values)
        {
            stock.itemCount = Math.Max(0, stock.itemCount);
            stock.partialCount = Math.Max(0, stock.partialCount);
        }

        await session.ConsumableGate.WaitAsync();
        try
        {
            session.Consumables = inventory;
        }
        finally
        {
            session.ConsumableGate.Release();
        }
    }

    private static async Task HandleServerAddedConsumableUse(Session user, Room room, InteractionData interaction)
    {
        UserVariable? con = user.UserVars.FirstOrDefault(var => var.Name == "con");
        string? type = UserVarHandler.GetConsumableType(con?.Value);
        ShareableConsumableDefinition? definition = ShareableConsumables.All
            .FirstOrDefault(item => string.Equals(item.RecipientType, type, StringComparison.Ordinal));
        if (definition == null)
            return;

        SFSObject used = new SFSObject();
        used.PutInt("senderId", user.UserId);
        used.PutUtfString("type", definition.RecipientType);
        foreach (Session target in room.Users.Values)
            await SendExtensionResponse(target, "con.use", room.RoomId, used);

        UserVariable clearedCon = new SFSUserVariable("con", null!, (int)VariableType.NULL);
        await UserVarHandler.SendUserVarConUpdate(user, clearedCon, 0, "sword");
        Console.WriteLine($"[Consumable use] {user.UserName} consumed {definition.RecipientType}.");
    }

    private static async Task HandleShareableConsumableInteraction(Session recipient, Room room, InteractionData interaction)
    {
        if (!long.TryParse(interaction.Id, out long ownerSessionId)
            || room.Users.Values.FirstOrDefault(user => user.SessionId == ownerSessionId) is not Session owner
            || ReferenceEquals(recipient, owner))
        {
            Console.WriteLine($"[Consumable interaction] No matching prop owner for {recipient.UserName}: type={interaction.Type}, id={interaction.Id}.");
            return;
        }

        ShareableConsumableDefinition? definition;
        int remaining;
        await owner.ConsumableGate.WaitAsync();
        try
        {
            if (owner.ActiveShareableConsumable is not string type
                || !ShareableConsumables.TryGet(type, out definition)
                || owner.ShareableConsumableCount <= 0)
            {
                Console.WriteLine($"[Consumable interaction] {owner.UserName} has no active shared prop for {recipient.UserName}.");
                return;
            }

            float dx = recipient.PosX - owner.PosX;
            float dy = recipient.PosY - owner.PosY;
            float dz = recipient.PosZ - owner.PosZ;
            if (dx * dx + dy * dy + dz * dz > 16f)
            {
                Console.WriteLine($"[Consumable interaction] {recipient.UserName} is too far from {owner.UserName} to take {type}.");
                return;
            }

            remaining = owner.ShareableConsumableCount - 1;
            owner.ShareableConsumableCount = remaining;
            if (remaining == 0)
                owner.ActiveShareableConsumable = null;
            if (owner.Consumables.TryGetValue(definition.Type, out InventoryItemStock? stock))
            {
                if (remaining == 0 && stock.itemCount <= 0)
                    owner.Consumables.Remove(definition.Type);
                else
                    stock.partialCount = remaining;
            }

            await UserVarHandler.GrantServerAddedConsumable(recipient, definition.RecipientType);
            await UserVarHandler.SendShareableConsumableCount(owner, remaining);
            Console.WriteLine($"[Consumable interaction] {recipient.UserName} took {definition.RecipientType} from {owner.UserName}; {remaining} servings remain.");

            SFSObject partial = new SFSObject();
            partial.PutUtfString("data", JsonSerializer.Serialize(new SignedResponse<UsedConsumable>
            {
                swid = owner.Swid,
                data = new UsedConsumable
                {
                    type = definition.Type,
                    partialCount = remaining
                }
            }));
            await SendExtensionResponse(owner, "con.partial", room.RoomId, partial);
        }
        finally
        {
            owner.ConsumableGate.Release();
        }
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

		await SfsWire.WriteMessageAsync(session, controller: 1, action: (short)RequestType.CallExtension, parameters: res);
	}
}
