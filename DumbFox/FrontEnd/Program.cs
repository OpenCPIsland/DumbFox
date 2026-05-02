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
Task webTask = WebApiServer.RunAsync(cts.Token);
Task tcpTask = DumbFoxServer.RunAsync(9933, cts.Token);
Task wsTask = DumbFoxWSServer.RunAsync(8080, cts.Token);

try
{
	await Task.WhenAll(webTask, tcpTask, wsTask);
}
catch (OperationCanceledException)
{
	// expected
}
return;

async void DatabaseCleaner()
{
    await using var conn = await DatabaseManager.OpenConnectionAsync();
   // await using var cmd = new MySqlCommand("DELETE FROM sessions;", conn);
  //  await cmd.ExecuteNonQueryAsync();
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
