using Common.Utils;
using DumbFox;
using DumbFox.Handlers;
using FrontEnd;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using WebAPI;

using CancellationTokenSource cts = new();
bool shuttingDown = false;
bool restartRequested = false;
World.RequestShutdown = () =>
{
    shuttingDown = true;
    restartRequested = true;
    cts.Cancel();
};

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

AppConfig config = configuration.Get<AppConfig>()!;

DatabaseManager.Configure(config.Database.ConnectionString);

PickupableManager.Load("Config/pickupables.json");

List<Task> tasks = new();

if (config.Servers.WebApi.Enabled)
{
    tasks.Add(WebApiServer.RunAsync(config.Servers.WebApi.HttpPort, config.Servers.WebApi.HttpsPort, cts.Token)); //ALL in One for your local testing
}

if (config.Servers.PublicApi.Enabled)
{
    tasks.Add(JgcApiServer.RunAsync(config.Servers.PublicApi.JgcPort, cts.Token));// For public server under NGINX, Apache, or other
    tasks.Add(MixApiServer.RunAsync(config.Servers.PublicApi.MixPort, cts.Token));// For public server under NGINX, Apache, or other
    tasks.Add(GameApiServer.RunAsync(config.Servers.PublicApi.GamePort, cts.Token));// For public server under NGINX, Apache, or other
}

if (config.Servers.Tcp.Enabled)
{
    tasks.Add(DumbFoxServer.RunAsync(config.Servers.Tcp.Port, cts.Token));
    tasks.Add(DumbFoxServer.RunUdpAsync(config.Servers.Tcp.Port, cts.Token));
}

if (config.Servers.WebSocket.Enabled)
{
    tasks.Add(DumbFoxServer.RunWebSocketAsync(config.Servers.WebSocket.WsPort, config.Servers.WebSocket.WssPort, cts.Token));
}

await using var conn = await DatabaseManager.OpenConnectionAsync();
await using var cmd0 = new MySqlCommand("UPDATE worlds SET host = @host, tcpport = @tcpPort, wssport = @WssPort, wsport = @WsPort, wshost = @wshost", conn);
cmd0.Parameters.AddWithValue("@host", config.Servers.hostname);
cmd0.Parameters.AddWithValue("@tcpPort", config.Servers.Tcp.Port);
cmd0.Parameters.AddWithValue("@WssPort", config.Servers.WebSocket.WssPort);
cmd0.Parameters.AddWithValue("@WsPort", config.Servers.WebSocket.WsPort);
cmd0.Parameters.AddWithValue("@wshost", config.Servers.WSHostname);
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

if (restartRequested)
{
    await DatabaseCleaner();
    StartReplacementProcess();
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

void StartReplacementProcess()
{
    string executablePath = Environment.ProcessPath
        ?? throw new InvalidOperationException("Cannot determine the current FrontEnd executable path.");

    ProcessStartInfo startInfo = new(executablePath)
    {
        WorkingDirectory = Environment.CurrentDirectory,
        UseShellExecute = false
    };

    if (string.Equals(Path.GetFileNameWithoutExtension(executablePath), "dotnet", StringComparison.OrdinalIgnoreCase))
    {
        string assemblyPath = Assembly.GetEntryAssembly()?.Location
            ?? throw new InvalidOperationException("Cannot determine the FrontEnd assembly path for relaunch.");
        startInfo.ArgumentList.Add(assemblyPath);
    }

    Process replacement = Process.Start(startInfo)
        ?? throw new InvalidOperationException("The replacement FrontEnd process could not be started.");
    Console.WriteLine($"Started replacement FrontEnd process (PID {replacement.Id}).");
    replacement.Dispose();
}

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

        Console.WriteLine("Sending restart notice to connected players...");

        await ExtensionHandler.BroadcastServerRestartMessageAsync();
        Console.WriteLine("Shutting down in 5 seconds...");
        await Task.Delay(TimeSpan.FromSeconds(5));

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
    SafeShutdownAsync().GetAwaiter().GetResult();
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