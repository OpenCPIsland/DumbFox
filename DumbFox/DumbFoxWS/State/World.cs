using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace DumbFoxWS.State;

public static class World
{
	public static List<Session> Sessions = [];
    public static ConcurrentDictionary<int, Session> SessionsByUserId { get; } = new();

    public static ConcurrentDictionary<int, Room> RoomsById { get; } = new();
    public static Room GetOrCrateRoom(string name)
    {

        Room? room = RoomsById.FirstOrDefault((roomPair) => roomPair.Value.Name == name).Value;
        if (room != null) return room;

        room = new Room(Room.NextId, name);
        RoomsById.TryAdd(room.RoomId, room);
        return room;
    }
}
