using Common.Utils;
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
        if (var.Value == null)
        {
            await SendUserVarConUpdate(session, var, 0, "sword");
            return;
        }

		if (var.Value.ToString().Contains("data"))
		{
			Console.WriteLine(var.Value.ToString());
            ISFSObject originalValue = SFSObject.NewFromJsonData((string)var.Value);
            ISFSObject data = originalValue.GetSFSObject("data");
            await SendUserVarConUpdate(session, var, data.GetInt("propId"), "sword");
            return;
		}
        await SendUserVarConUpdate(session, var, 0, "sword");
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



    public static async Task SendUserVarConUpdate(Session session, UserVariable var, int propId, string cmd)
    {
        SFSObject payload = new SFSObject();
        payload.PutInt("u", session.UserId);

        SFSArray vl = UserVariables.MakeUserVarList(var);
        SFSArray conProp = new SFSArray();

        if (cmd == "PizzaPlanetGroup")
		{
            conProp.AddUtfString("con_prop");
            conProp.AddByte(4);
            SFSObject fakereward = new SFSObject();
			fakereward.PutInt("actionCount", 6);
            conProp.AddUtfString(fakereward.ToJson());
            conProp.AddBool(false);

            session.SetUserVar(new SFSUserVariable("con_prop", fakereward.ToJson()));
		}
		else
		{
            conProp.AddUtfString("con_prop");
            conProp.AddByte(0);
            conProp.AddNull();
            conProp.AddBool(false);
        }


			vl.AddSFSArray(conProp);
        payload.PutSFSArray("vl", vl);

        foreach (KeyValuePair<int, Session> kv in World.RoomsById[session.CurrentRoomId ?? 0].Users)
            await SfsWire.WriteMessageAsync(kv.Value, 0, (short)RequestType.SetUserVariables, payload);
        session.SetUserVar(new SFSUserVariable(var.Name, var.Value));
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
