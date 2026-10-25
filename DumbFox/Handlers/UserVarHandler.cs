using Common.Utils;
using Common.Utils.Game;
using DumbFox.Utils;
using Microsoft.Extensions.Options;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace DumbFox.Handlers;

public static class UserVarHandler
{
	public static async Task HandleUserVarUpdate(Session session, UserVariable var)
	{
		string name = var.Name;
		await (name switch
		{
			"loc" or "awayfromkeyboard" or "bubble" or "tempheadstatus" => BasicVar(session, var),
			"tube" => TubeVar(session, var),
			"colour" => ColourVar(session, var),
			"outfit" => OutfitVar(session, var),
            "con" => ConVar(session, var),
            _ => Task.Run(() => Console.WriteLine($"Unhandled user var: {name}"))
		});
	}

	private static async Task BasicVar(Session session, UserVariable var)
	{
		await SendUserVarUpdate(session, var);
	}

    private static async Task ConVar(Session session, UserVariable var)
    {
        await session.ConsumableGate.WaitAsync();
        try
        {
            if (var.Value == null)
            {
                StoreActiveShareableConsumable(session);
                session.ShareableConsumableExhausted = false;
                await SendUserVarConUpdate(session, var, 0, "sword");
                return;
            }

            string? equippedType = GetConsumableType(var.Value);
            if (session.ActiveShareableConsumable is string activeType)
            {
                if (string.Equals(activeType, equippedType, StringComparison.Ordinal))
                {
                    await SendUserVarConUpdate(session, var, 0, activeType, session.ShareableConsumableCount);
                    return;
                }

                StoreActiveShareableConsumable(session);
            }

            if (equippedType != null && (session.ShareableConsumableExhausted || IsExhaustedShareableConsumable(session, equippedType)))
            {
                UserVariable clearedCon = new SFSUserVariable("con", null!, (int)VariableType.NULL);
                await SendUserVarUpdate(session, clearedCon);
                session.ShareableConsumableExhausted = false;
                await Task.Delay(1000);
            }

            if (var.Value is string signedValue && signedValue.Contains("data", StringComparison.Ordinal))
            {
                Console.WriteLine(signedValue);
                ISFSObject originalValue = SFSObject.NewFromJsonData(signedValue);
                ISFSObject data = originalValue.GetSFSObject("data");
                await SendUserVarConUpdate(session, var, data.GetInt("propId"), "sword");
                return;
            }

            await SendUserVarConUpdate(session, var, 0, "sword");
        }
        finally
        {
            session.ConsumableGate.Release();
        }
    }

    private static async Task ColourVar(Session session, UserVariable var)
	{
		ISFSObject originalValue = SFSObject.NewFromJsonData((string)var.Value);
		ISFSObject data = originalValue.GetSFSObject("data");
		UserVariable colour = new SFSUserVariable("colour", data.GetInt("colour"));
		await SendUserVarUpdate(session, colour);
	}

	private static async Task TubeVar(Session session, UserVariable var)
	{
		ISFSObject originalValue = SFSObject.NewFromJsonData((string)var.Value);
		ISFSObject data = originalValue.GetSFSObject("data");
		UserVariable colour = new SFSUserVariable("tube", data.GetInt("tube"));
		await SendUserVarUpdate(session, colour);
	}

    private static async Task OutfitVar(Session session, UserVariable var)
	{
		ISFSObject originalValue = SFSObject.NewFromJsonData((string)var.Value);
		UserVariable outfit = new SFSUserVariable("outfit", originalValue.GetSFSObject("data").ToJson());
		await SendUserVarUpdate(session, outfit);
	}



    public static async Task SendUserVarConUpdate(Session session, UserVariable var, int propId, string cmd, int? actionCount = null)
    {
        SFSObject payload = new SFSObject();
        payload.PutInt("u", session.UserId);

        SFSArray vl = UserVariables.MakeUserVarList(var);
        SFSArray conProp = new SFSArray();

        if (ShareableConsumables.TryGet(cmd, out ShareableConsumableDefinition? definition))
        {
            conProp.AddUtfString("con_prop");
            conProp.AddByte(4);
            SFSObject propData = new SFSObject();
            propData.PutInt("actionCount", actionCount ?? definition.ActionCount);
            conProp.AddUtfString(propData.ToJson());
            conProp.AddBool(false);

            session.SetUserVar(new SFSUserVariable("con_prop", propData.ToJson()));
        }
        else
        {
            conProp.AddUtfString("con_prop");
            conProp.AddByte(0);
            conProp.AddNull();
            conProp.AddBool(false);
            session.SetUserVar(new SFSUserVariable("con_prop", null!, (int)VariableType.NULL));
        }


        vl.AddSFSArray(conProp);
        payload.PutSFSArray("vl", vl);

        foreach (KeyValuePair<int, Session> kv in World.RoomsById[session.CurrentRoomId ?? 0].Users)
            await SfsWire.WriteMessageAsync(kv.Value, 0, (short)RequestType.SetUserVariables, payload);
        session.SetUserVar(new SFSUserVariable(var.Name, var.Value));
    }

    private static void StoreActiveShareableConsumable(Session session)
    {
        if (session.ActiveShareableConsumable is string type
            && session.ShareableConsumableCount > 0
            && session.Consumables.TryGetValue(type, out InventoryItemStock? stock))
        {
            stock.partialCount = session.ShareableConsumableCount;
        }

        session.ActiveShareableConsumable = null;
        session.ShareableConsumableCount = 0;
    }

    public static string? GetConsumableType(object? value)
    {
        if (value is ISFSObject consumable && consumable.ContainsKey("name"))
            return consumable.GetUtfString("name");

        if (value is string json)
        {
            ISFSObject response = SFSObject.NewFromJsonData(json);
            if (response.ContainsKey("data"))
            {
                ISFSObject data = response.GetSFSObject("data");
                if (data.ContainsKey("type"))
                    return data.GetUtfString("type");
            }
        }

        return null;
    }

    private static bool IsExhaustedShareableConsumable(Session session, string equippedType)
    {
        UserVariable? con = session.UserVars.FirstOrDefault(userVar => userVar.Name == "con");
        UserVariable? conProp = session.UserVars.FirstOrDefault(userVar => userVar.Name == "con_prop");
        if (!string.Equals(GetConsumableType(con?.Value), equippedType, StringComparison.Ordinal)
            || conProp?.Value is not string properties)
        {
            return false;
        }

        ISFSObject propData = SFSObject.NewFromJsonData(properties);
        return propData.ContainsKey("actionCount") && propData.GetInt("actionCount") == 0;
    }

    public static async Task SendShareableConsumableCount(Session session, int count)
    {
        SFSObject propData = new SFSObject();
        propData.PutInt("actionCount", count);
        SFSArray conProp = new SFSArray();
        conProp.AddUtfString("con_prop");
        conProp.AddByte(4);
        conProp.AddUtfString(propData.ToJson());
        conProp.AddBool(false);
        SFSArray vars = new SFSArray();
        vars.AddSFSArray(conProp);

        SFSObject payload = new SFSObject();
        payload.PutInt("u", session.UserId);
        payload.PutSFSArray("vl", vars);

        if (session.CurrentRoomId is int roomId && World.RoomsById.TryGetValue(roomId, out Room? room))
        {
            foreach (Session target in room.Users.Values)
                await SfsWire.WriteMessageAsync(target, 0, (short)RequestType.SetUserVariables, payload);
        }

        session.SetUserVar(new SFSUserVariable("con_prop", propData.ToJson()));
        session.ShareableConsumableExhausted = count == 0;
    }

    public static async Task GrantServerAddedConsumable(Session session, string type)
    {
        SFSObject consumable = new SFSObject();
        consumable.PutUtfString("name", type);
        consumable.PutUtfString("type", "consum");
        string properties = JsonSerializer.Serialize(new { actionCount = 1 });
        SFSUserVariable con = new("con", consumable);
        SFSUserVariable conProp = new("con_prop", properties);
        SFSObject payload = new();
        payload.PutInt("u", session.UserId);
        payload.PutSFSArray("vl", UserVariables.MakeUserVarList(con, conProp));

        if (session.CurrentRoomId is int roomId && World.RoomsById.TryGetValue(roomId, out Room? room))
        {
            foreach (Session target in room.Users.Values)
                await SfsWire.WriteMessageAsync(target, 0, (short)RequestType.SetUserVariables, payload);
        }

        session.SetUserVar(con);
        session.SetUserVar(conProp);
    }

    public static async Task SendUserVarUpdate(Session session, UserVariable var)
	{
		SFSObject payload = new SFSObject();
		payload.PutInt("u", session.UserId);
		payload.PutSFSArray("vl", UserVariables.MakeUserVarList(var));
		foreach (KeyValuePair<int, Session> kv in World.RoomsById[session.CurrentRoomId ?? 0].Users)
			await SfsWire.WriteMessageAsync(kv.Value, 0, (short)RequestType.SetUserVariables, payload);
		session.SetUserVar(new SFSUserVariable(var.Name, var.Value));
	}
}
