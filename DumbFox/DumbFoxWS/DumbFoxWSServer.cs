using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net.WebSockets;

namespace DumbFoxWS;

public static class DumbFoxWSServer
{
	public static async Task RunAsync(int port, int portwss,  CancellationToken ct = default)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		//builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        
		builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port); // ws://

            options.ListenAnyIP(portwss, listenOptions =>
            {
                listenOptions.UseHttps(); // wss://
            });
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

			WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
			await ClientManager.HandleClient(socket, ct);
		});

		using CancellationTokenRegistration stopRegistration = ct.Register(() => app.Lifetime.StopApplication());

		Console.WriteLine($"DumbFox WebSocket Sfs2x API server started on ws://0.0.0.0:{port}");
        Console.WriteLine($"DumbFox WebSocket Secure Sfs2x API server started on wss://0.0.0.0:{portwss}");
        Console.WriteLine($" ");
        try
		{
			await app.RunAsync(ct);
		}
		catch (OperationCanceledException)
		{
			// expected
		}
		finally
		{
			Console.WriteLine("DumbFox WebSocket server stopped");
		}
	}
}

