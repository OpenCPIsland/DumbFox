using Common.Utils;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using System.Text.Json;

namespace DumbFox.Utils;

public static class RoomUtils
{
	public static SFSArray FromRoomState(Room room, string groupId, short maxUsers, short maxSpectators)
	{
		SFSArray desc = new SFSArray();
		desc.AddInt(room.RoomId);
		desc.AddUtfString(room.Name);
		desc.AddUtfString(groupId);
		
		desc.AddBool(false);
		desc.AddBool(false);
		desc.AddBool(false);
		
		desc.AddShort((short)room.Users.Count);
		desc.AddShort(maxUsers);

		desc.AddSFSArray(new SFSArray());

		desc.AddShort(0);
		desc.AddShort(maxSpectators);
		
		desc.AddFloatArray([900, 900, 900]);
		desc.AddFloatArray([-900, -900, -900]);
		desc.AddFloatArray([900, 900, 900]);
		
		return desc;
	}
	
	internal static (string? roomName, long? sessionId) TryGetJoinRoomData(SFSObject req)
	{
		try
		{
			SFSObject custom = (SFSObject)req.GetSFSObject("p");
			if (!custom.ContainsKey("joinRoomData")) return (null, null);

			string j = custom.GetUtfString("joinRoomData");
			using JsonDocument doc = JsonDocument.Parse(j);

			if (!doc.RootElement.TryGetProperty("data", out JsonElement data)) return (null, null);

			string? room = data.TryGetProperty("room", out JsonElement roomEl) ? roomEl.GetString() : null;
			long? sessionId = data.TryGetProperty("sessionId", out JsonElement sidEl) && sidEl.ValueKind == JsonValueKind.Number
				? sidEl.GetInt64()
				: null;

			return (room, sessionId);
		}
		catch
		{
			return (null, null);
		}
	}
	
	public static async Task SendJoinRoom(Room room, string groupId, Session session)
    {
        if (session.CurrentRoomId is int previousRoomId
            && previousRoomId != room.RoomId
            && World.RoomsById.TryGetValue(previousRoomId, out Room? previousRoom))
        {
            await RemoveSessionFromRoom(previousRoom, session);
        }

        session.CurrentRoomId = room.RoomId;

        bool isNewMember = room.Users.TryAdd(session.UserId, session);
        if (!isNewMember)
            room.Users[session.UserId] = session;
        
        SFSArray roomDesc = RoomUtils.FromRoomState(room, groupId, maxUsers: 39, maxSpectators: 100);

        SFSObject content = new SFSObject();
        content.PutSFSArray("r", roomDesc);
        content.PutSFSArray("ul", new SFSArray());

        await SfsWire.WriteMessageAsync(session, controller: 0, action: 4, parameters: content);

        foreach (KeyValuePair<int, Session> kv in room.Users)
            await UserVariables.SendUserVariablesTo(session, kv.Value);

        await SendOnUserJoinedRoom(room, session, isNewMember);
    }

    public static async Task RemoveSessionFromRoom(Session session)
    {
        if (session.CurrentRoomId is int roomId && World.RoomsById.TryGetValue(roomId, out Room? room))
            await RemoveSessionFromRoom(room, session);
    }

    public static async Task RemoveSessionFromRoom(Room room, Session session)
    {
        if (!room.Users.TryRemove(session.UserId, out _)) return;

        session.CurrentRoomId = null;
        SFSObject removePayload = new SFSObject();
        removePayload.PutInt("r", room.RoomId);
        removePayload.PutIntArray("m", [session.UserId]);

        SFSObject userCountPayload = new SFSObject();
        userCountPayload.PutInt("r", room.RoomId);
        userCountPayload.PutShort("uc", (short)Math.Clamp(room.Users.Count, 0, short.MaxValue));

        foreach (Session target in room.Users.Values)
        {
            await SfsWire.WriteMessageAsync(target, 0, (short)RequestType.SetUserPosition, removePayload);
            await SfsWire.WriteMessageAsync(target, 0, 1001, userCountPayload);
        }
    }

    private static async Task SendOnUserJoinedRoom(Room room, Session joined, bool isNewMember)
    {
        SFSArray positionsPayload = new SFSArray();
        foreach (KeyValuePair<int, Session> ji in room.Users)
            positionsPayload.AddSFSArray(UserVariables.BuildUserArray(ji.Value));
        SFSObject setUserPositionRequest = new SFSObject();
        setUserPositionRequest.PutSFSArray("p", positionsPayload);
        setUserPositionRequest.PutInt("r", room.RoomId);
		
        await SfsWire.WriteMessageAsync(joined, 0, (short)RequestType.SetUserPosition, setUserPositionRequest);
        
        await HandleUserJoinedNotifications(room, joined, isNewMember);
    }
    
    private static async Task HandleUserJoinedNotifications(Room room, Session joined, bool isNewMember)
    {
        SFSObject joinedPayload = new SFSObject();
        joinedPayload.PutInt("r", room.RoomId);
        joinedPayload.PutSFSArray("u", UserVariables.BuildUserArray(joined));

        SFSObject userCountPayload = new SFSObject();
        userCountPayload.PutInt("r", room.RoomId);
        userCountPayload.PutShort("uc", (short)Math.Clamp(room.Users.Count, 0, short.MaxValue));

        foreach (KeyValuePair<int, Session> kv in room.Users)
        {
            Session target = kv.Value;

            SFSArray newPlayerPositionPayload = new SFSArray();
            newPlayerPositionPayload.AddSFSArray(UserVariables.BuildUserArray(joined));
            SFSObject syncNewPlayerPosRequest = new SFSObject();
            syncNewPlayerPosRequest.PutSFSArray("p", newPlayerPositionPayload);
            syncNewPlayerPosRequest.PutInt("r", room.RoomId);
		
            if (target.UserId == joined.UserId || !isNewMember) continue;
            await SfsWire.WriteMessageAsync(target, 0, (short)RequestType.SetUserPosition, syncNewPlayerPosRequest);
            await SfsWire.WriteMessageAsync(target, 0, 1000, joinedPayload);
            await SfsWire.WriteMessageAsync(target, 0, 1001, parameters: userCountPayload);

            await UserVariables.SendUserVariablesTo(target, joined);
        }

    }
}