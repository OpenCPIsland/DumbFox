using Common.Utils;
using DumbFox.Utils;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
namespace DumbFox.Handlers;

public static class UserVarHandler
{
	public static async Task HandleUserVarUpdate(Session session, UserVariable var)
	{
		string name = var.Name;
		await (name switch
		{
			"loc" or "awayfromkeyboard" or "bubble" or "con" => BasicVar(session, var),
			"tube" => TubeVar(session, var),
			"colour" => ColourVar(session, var),
			"outfit" => OutfitVar(session, var),
			_ => Task.Run(() => Console.WriteLine($"Unhandled user var: {name}"))
		});
	}

	private static async Task BasicVar(Session session, UserVariable var)
	{
		await SendUserVarUpdate(session, var);
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

	public static async Task SendUserVarUpdate(Session session, UserVariable var)
	{
		SFSObject payload = new SFSObject();
		payload.PutInt("u", session.UserId);
		payload.PutSFSArray("vl", UserVariables.MakeUserVarList(var));
		foreach (KeyValuePair<int, Session> kv in World.RoomsById[session.CurrentRoomId ?? 0].Users)
			await SfsWire.WriteMessageAsync(kv.Value.Stream, 0, (short)RequestType.SetUserVariables, payload);
		session.SetUserVar(new SFSUserVariable(var.Name, var.Value));
	}
}
