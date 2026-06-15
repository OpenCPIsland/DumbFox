using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using Common.Utils;
namespace DumbFox.Utils;

public static class UserVariables
{
	public const string DefaultOutfit = "{\"parts\":[]}";
	
	public static SFSArray MakeUserVarList(params UserVariable[] vars)
	{
		var list = new SFSArray();
		foreach (var v in vars)
		{
			// Each element must be an SFSArray descriptor (because client calls FromSFSArray)
			list.AddSFSArray((SFSArray)v.ToSFSArray());
		}
		return list;
	}

	public static Task SendSetUserVariables(Session session, int targetUserId, params UserVariable[] vars)
	{
		var content = new SFSObject();

		// THESE are the important lines:
		content.PutInt(SetUserVariablesRequest.KEY_USER, targetUserId);
		content.PutSFSArray(SetUserVariablesRequest.KEY_VAR_LIST, MakeUserVarList(vars));

		// controller=0, action=12 (SetUserVariables)
		return SfsWire.WriteMessageAsync(session, controller: 0, action: (short)RequestType.SetUserVariables, parameters: content);
	}
	
	public static Task SendUserVariablesTo(Session receiver, Session target) =>
		SendSetUserVariables(receiver, target.UserId, target.UserVars.ToArray());
	
	public static SFSArray BuildUserArray(Session session)
	{
		SFSArray u = new SFSArray();
		u.AddInt(session.UserId);
		u.AddUtfString(session.UserName ?? "");
		u.AddShort(0); // privilegeId
		u.AddShort(0); // playerId (room-scoped)
		u.AddSFSArray(MakeUserVarList(session.UserVars.ToArray()));
		u.AddFloatArray([session.PosX, session.PosY, session.PosZ]);
		return u;
	}

	public static SFSUserVariable[] BuildUserVariablesTemp(Session session) =>
	[
		new SFSUserVariable("colour", 5),
		new SFSUserVariable("tube", 2),
		new SFSUserVariable("outfit", DefaultOutfit),
		new SFSUserVariable("sess", session.SessionId.ToString()),
	];
}
