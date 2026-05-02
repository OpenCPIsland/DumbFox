using Sfs2X.Entities.Variables;
using System.Net.WebSockets;
using System.Net.Sockets;
namespace DumbFoxWS.State;

public sealed class Session(WebSocket socket)
{
    public WebSocket Socket { get; } = socket;

    public int UserId { get; set; }
    public string? UserName { get; set; }

    public int? CurrentRoomId { get; set; }

    public long SessionId { get; set; }
    public string? Swid { get; set; }

    public bool IsLoggedIn { get; set; }

    public byte[]? AesKey { get; set; }

    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }

    public List<SFSUserVariable> UserVars { get; set; } = [];

    public void Cleanup()
    {
        if (UserId != 0)
        {
            World.SessionsByUserId.TryRemove(UserId, out _);
        }

        if (CurrentRoomId is not { } roomId || !World.RoomsById.TryGetValue(roomId, out Room? room))
            return;

        room.Users.TryRemove(UserId, out _);
        if (room.Users.IsEmpty)
        {
            World.RoomsById.TryRemove(roomId, out _);
        }
    }

    public void SetUserVar(SFSUserVariable var)
    {
        for (int i = 0; i < UserVars.Count; i++)
        {
            if (UserVars[i].Name != var.Name) continue;

            UserVars[i] = var;
            return;
        }
        UserVars.Add(var);
    }

    public SFSUserVariable GetUserVar(string name)
    {
        return UserVars.First(v => v.Name == name);
    }
}
