using Sfs2X.Entities.Variables;
using System.Net.WebSockets;
using System.Net.Sockets;
namespace Common.Utils;

public sealed class Session(TcpClient? client)
{
    public TcpClient? Client { get; } = client;
    public NetworkStream? Stream { get; } = client != null ? client.GetStream() : null;
    public WebSocket? Socket { get; }

    public Session(WebSocket socket) : this((TcpClient?)null)
    {
        Socket = socket;
    }

    public int UserId { get; set; }
	public string Swid { get; set; } = "";
	public byte[]? AesKey { get; set; }
	public string UserName { get; set; } = "";
    public long SessionId { get; set; } = 1;

    public bool IsLoggedIn { get; set; }
    public int? CurrentRoomId { get; set; }
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
