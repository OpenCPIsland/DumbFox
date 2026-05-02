using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net.WebSockets;

namespace DumbFoxWS;

public static class DumbFoxWSServer
{
	public static async Task RunAsync(int port, CancellationToken ct = default)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		//builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        
		builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port); // ws://

            options.ListenAnyIP(8443, listenOptions =>
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

		Console.WriteLine($"DumbFox WebSocket server started on http://0.0.0.0:{port}");
        Console.WriteLine($"DumbFox WebSocket Secure server started on http://0.0.0.0:{8443}");
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

