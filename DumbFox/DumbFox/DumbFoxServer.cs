using System.Net;
using System.Net.Sockets;

namespace DumbFox;

public static class DumbFoxServer
{
	public static async Task RunAsync(int port, CancellationToken ct = default)
	{
		TcpListener listener = new TcpListener(IPAddress.Any, port);
		listener.Start();
		Console.WriteLine($"DumbFox SFS2x API TCP Server started on port {port}");
        Console.WriteLine($" ");

        try
		{
			while (!ct.IsCancellationRequested)
			{
				TcpClient client = await listener.AcceptTcpClientAsync(ct);
				_ = ClientManager.HandleClient(client, ct); // no Task.Run
			}
		}
		catch (OperationCanceledException)
		{
			// expected
		}
		finally
		{
			listener.Stop();
			Console.WriteLine("DumbFox Server stopped");
		}
	}
}