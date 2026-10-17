using Common.Utils.Game;
using Sfs2X.Entities.Variables;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
namespace Common.Utils;

public sealed class Session(TcpClient? client)
{
    public TcpClient? Client { get; } = client;
    public NetworkStream? Stream { get; } = client != null ? client.GetStream() : null;
    public WebSocket? Socket { get; }
    public IPEndPoint? RemoteEndPoint { get; set; }
    public IPEndPoint? LocalEndPoint { get; set; }

    public DateTime LastPongUtc { get; set; } = DateTime.UtcNow;
    public bool IsDisconnected { get; set; }

    private int _cleanupStarted;

    public Session(WebSocket socket) : this((TcpClient?)null)
    {
        Socket = socket;
    
    }

    public int UserId { get; set; }
	public string Swid { get; set; } = "";
	public byte[]? AesKey { get; set; }
	public string UserName { get; set; } = "";
    public string ActivQuest { get; set; } = "";
    public List<QuestStateCollection> Quest { get; set; } = [
        new QuestStateCollection{
        questId = "AAC001Q001LeakyShip",
        status = (int)QuestStatus.COMPLETED,
        completedObjectives = ["Obj0FSM", "Obj2FSM ", "Obj3FSM ", "Obj4FSM", "Obj5FSM", "Obj6FSM", "Obj8FSM", "Obj9FSM", "Obj10FSM", "Obj11FSM"],
        timesCompleted = 1
    },new QuestStateCollection{
        questId = "RHC001Q001TreasureQuest",
        status = (int)QuestStatus.AVAILABLE,
    },new QuestStateCollection{
        questId = "RKC001Q001Drop",
        status = (int)QuestStatus.AVAILABLE,
    },new QuestStateCollection{
        questId = "DJC001Q001Plan",
        status = (int)QuestStatus.AVAILABLE,
    }
        ];
    public Dictionary<string, long> earnedRewards = new Dictionary<string, long>();
    public long SessionId { get; set; } = 1;

    public bool IsLoggedIn { get; set; }
    public int? CurrentRoomId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }

    public List<SFSUserVariable> UserVars { get; set; } = [];

    public void Cleanup()
    {
        if (Interlocked.Exchange(ref _cleanupStarted, 1) != 0)
            return;

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
