using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace DumbFox;

public static class DumbFoxServer
{
	private const string DefaultCertPath = "/etc/letsencrypt/live/iclubpenguinisland.chaosfu.net/fullchain.pem";
	private const string DefaultKeyPath = "/etc/letsencrypt/live/iclubpenguinisland.chaosfu.net/privkey.pem";

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
				_ = ClientManager.HandleClient(client, ct);
			}
		}
		catch (OperationCanceledException)
		{
			// expected
		}
		finally
		{
			listener.Stop();
			Console.WriteLine("DumbFox TCP server stopped");
		}
	}

	public static async Task RunWebSocketAsync(int wsPort, int wssPort, CancellationToken ct = default)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(DumbFoxServer).Assembly.FullName,
			ContentRootPath = AppContext.BaseDirectory,
		});
		builder.Logging.SetMinimumLevel(LogLevel.Warning);

		X509Certificate2? tlsCert = TryLoadCertificate(DefaultCertPath, DefaultKeyPath);

		builder.WebHost.ConfigureKestrel(options =>
		{
			options.ListenAnyIP(wsPort);

			if (wssPort > 0 && tlsCert is not null)
			{
				options.ListenAnyIP(wssPort, listenOptions => listenOptions.UseHttps(tlsCert));
			}
		});

		WebApplication app = builder.Build();
		app.UseWebSockets();

		app.Run(async context =>
		{
			if (!context.WebSockets.IsWebSocketRequest)
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				await context.Response.WriteAsync("WebSocket request required.");
				return;
			}

			var socket = await context.WebSockets.AcceptWebSocketAsync();
			await ClientManager.HandleClient(socket, ct);
		});

		await app.StartAsync(ct);

		Console.WriteLine($"DumbFox WebSocket Sfs2x API server started on ws://0.0.0.0:{wsPort}");
		if (wssPort > 0 && tlsCert is not null)
			Console.WriteLine($"DumbFox WebSocket Secure Sfs2x API server started on wss://0.0.0.0:{wssPort}");
		else if (wssPort > 0)
			Console.WriteLine($"DumbFox WSS disabled (no TLS certificate found at {DefaultCertPath})");
		Console.WriteLine($" ");

		try
		{
			await Task.Delay(Timeout.InfiniteTimeSpan, ct);
		}
		catch (OperationCanceledException)
		{
			// expected
		}
		finally
		{
			await app.StopAsync(CancellationToken.None);
			Console.WriteLine("DumbFox WebSocket server stopped");
		}
	}

	private static X509Certificate2? TryLoadCertificate(string certPath, string keyPath)
	{
		try
		{
			if (!File.Exists(certPath) || !File.Exists(keyPath))
				return null;

			return X509Certificate2.CreateFromPemFile(certPath, keyPath);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"DumbFox WSS certificate load failed: {ex.Message}");
			return null;
		}
	}
}
