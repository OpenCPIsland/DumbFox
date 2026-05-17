using Common.Utils;
using DumbFox;
using DumbFoxWS;
using MySqlConnector;
using WebAPI;

using CancellationTokenSource cts = new CancellationTokenSource();

DatabaseManager.Configure("Server=localhost;Port=3306;Database=opencpidb;User Id=root;Password=;");
//await using var conn = await DatabaseManager.OpenConnectionAsync();

Console.CancelKeyPress += (_, e) =>
{
	e.Cancel = true; // graceful shutdown
	SafeCancel();
};

//TODO: handle better running of clients, use a config to determine what clients to run
Task webTask = WebApiServer.RunAsync(cts.Token); //ALL in One uses port 80 for you local testing

//Task jgcTask = JgcApiServer.RunAsync(cts.Token); // for public server under NGINX or Apache or other
//Task mixTask = MixApiServer.RunAsync(cts.Token); // for public server under NGINX or Apache or other
//Task gameTask = GameApiServer.RunAsync(cts.Token); // for public server under NGINX or Apache or other

Task tcpTask = DumbFoxServer.RunAsync(9933, cts.Token);
Task wsTask = DumbFoxWSServer.RunAsync(8080, cts.Token);

try
{
	DatabaseCleaner();
    //await Task.WhenAll(jgcTask, mixTask, gameTask, wsTask, tcpTask);
    await Task.WhenAll(webTask, wsTask, tcpTask);
}
catch (OperationCanceledException)
{
	// expected
}
return;

async void DatabaseCleaner()
{
    await using var conn = await DatabaseManager.OpenConnectionAsync();
    await using var cmd = new MySqlCommand("DELETE FROM sessions;", conn);
    await cmd.ExecuteNonQueryAsync();
    await using var cmd0 = new MySqlCommand("DELETE FROM rooms;", conn);
    await cmd0.ExecuteNonQueryAsync();
    await using var cmd1 = new MySqlCommand("DELETE FROM room_users;", conn);
    await cmd1.ExecuteNonQueryAsync();
}

void SafeCancel()
{
	try
	{
		if (!cts.IsCancellationRequested)
			cts.Cancel();
		DatabaseCleaner();


    }
	catch (ObjectDisposedException)
	{
		// process is already tearing down
	}
}
