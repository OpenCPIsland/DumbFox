using Common.Utils;
using DumbFox;
using DumbFoxWS;
using FrontEnd;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using WebAPI;

using CancellationTokenSource cts = new();

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

AppConfig config = configuration.Get<AppConfig>()!;

DatabaseManager.Configure(config.Database.ConnectionString);

List<Task> tasks = new();

if (config.Servers.WebApi.Enabled)
{
    tasks.Add(WebApiServer.RunAsync(config.Servers.WebApi.HttpPort,config.Servers.WebApi.HttpsPort,cts.Token)); //ALL in One for your local testing
}

if (config.Servers.PublicApi.Enabled)
{
    tasks.Add(JgcApiServer.RunAsync(config.Servers.PublicApi.JgcPort,cts.Token));// For public server under NGINX, Apache, or other
    tasks.Add(MixApiServer.RunAsync(config.Servers.PublicApi.MixPort,cts.Token));// For public server under NGINX, Apache, or other
    tasks.Add(GameApiServer.RunAsync(config.Servers.PublicApi.GamePort,cts.Token));// For public server under NGINX, Apache, or other
}

if (config.Servers.Tcp.Enabled)
{
    tasks.Add(DumbFoxServer.RunAsync(config.Servers.Tcp.Port,cts.Token));
}

if (config.Servers.WebSocket.Enabled)
{
    tasks.Add(DumbFoxWSServer.RunAsync(config.Servers.WebSocket.WsPort,config.Servers.WebSocket.WssPort,cts.Token));
}

await using var conn = await DatabaseManager.OpenConnectionAsync();
await using var cmd0 = new MySqlCommand("UPDATE worlds SET host = @host, tcpport = @tcpPort, wssport = @WssPort, wsport = @WsPort", conn);
cmd0.Parameters.AddWithValue("@host", config.Servers.hostname);
cmd0.Parameters.AddWithValue("@tcpPort", config.Servers.Tcp.Port);
cmd0.Parameters.AddWithValue("@WssPort", config.Servers.WebSocket.WssPort);
cmd0.Parameters.AddWithValue("@WsPort", config.Servers.WebSocket.WsPort);
await cmd0.ExecuteNonQueryAsync();

try
{
    await DatabaseCleaner();
    await Task.WhenAll(tasks);
}
catch (OperationCanceledException)
{
    // expected
}

async Task DatabaseCleaner()
{
    await using var conn = await DatabaseManager.OpenConnectionAsync();

    if (config.Servers.IsIntimate)
    {
        await using var cmd = new MySqlCommand("DELETE FROM sessions;", conn);
        await cmd.ExecuteNonQueryAsync();
    }
    await using var cmd0 = new MySqlCommand("DELETE FROM rooms;", conn);
    await cmd0.ExecuteNonQueryAsync();
    await using var cmd1 = new MySqlCommand("DELETE FROM room_users;", conn);
    await cmd1.ExecuteNonQueryAsync();
}

bool shuttingDown = false;

void SafeShutdownSync()
{
    try
    {
        if (shuttingDown)
            return;

        shuttingDown = true;

        Console.WriteLine("Shutting down...");

        if (!cts.IsCancellationRequested)
            cts.Cancel();

        DatabaseCleaner()
            .GetAwaiter()
            .GetResult();

        Console.WriteLine("Shutdown complete.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Shutdown error: {ex}");
    }
}

async Task SafeShutdownAsync()
{
    try
    {
        if (shuttingDown)
            return;

        shuttingDown = true;

        Console.WriteLine("Shutting down...");

        if (!cts.IsCancellationRequested)
            cts.Cancel();

        await DatabaseCleaner();

        Console.WriteLine("Shutdown complete.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Shutdown error: {ex}");
    }
}

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shuttingDown = true;
    _ = SafeShutdownAsync();
};

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    if (!shuttingDown)
    {
        shuttingDown = true;

        SafeShutdownSync();
    }
};

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    Console.WriteLine($"Fatal error: {e.ExceptionObject}");
    shuttingDown = true;
    SafeShutdownSync();
};
