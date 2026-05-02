using Common.Utils;
using Common.Utils.Jgc;
using Microsoft.AspNetCore.Http;
using MySqlConnector;
using System.Net.Sockets;
using System.Security.Cryptography;
namespace WebAPI.Api.Jgc.routes;

public static class GetApiKeyImmediately
{
	public static async Task Handle(HttpContext ctx)
	{
		Console.WriteLine("[WebAPI][JGC] Got API key request");

		string apiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(86));

		ctx.Response.Headers["X-Cnection"] = "close";
		ctx.Response.Headers["api-key"] = apiKey;
        await using var conn = await DatabaseManager.OpenConnectionAsync();
        await using var cmd = new MySqlCommand(
            "INSERT INTO sessions (session_id, expires_at) VALUES (@apikey, DATE_ADD(NOW(), INTERVAL 1440 MINUTE));",
            conn);
        cmd.Parameters.AddWithValue("@apikey", apiKey);
        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        Console.WriteLine($"[WebAPI][JGC] So many Rows got Affected x{rowsAffected}");
        /*
		 var session = await GetSessionAccessToken("myToken");

if (session is null)
    return;
		*/

		await ctx.Response.WriteAsJsonAsync(new DefaultBaseResponse { });
	}
}
