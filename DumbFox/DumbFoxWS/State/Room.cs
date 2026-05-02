using System.Collections.Concurrent;

namespace DumbFoxWS.State;

public sealed class Room(int id, string name)
{
	private static int _nextId = 0;
	public static int NextId => Interlocked.Increment(ref _nextId);
	
	public int RoomId { get; } = id;
	public string Name { get; } = name;
	public ConcurrentDictionary<int, Session> Users { get; } = new();

}