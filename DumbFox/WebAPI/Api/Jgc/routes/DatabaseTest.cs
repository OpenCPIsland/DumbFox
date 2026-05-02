using Common.Utils;
using Microsoft.AspNetCore.Http;
using MySqlConnector;
using System.Net.Sockets;
using System.Security.Cryptography;
namespace WebAPI.Api.Jgc.routes;


public static class DatabaseTest
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI] Got API key request");

        await using var conn = await DatabaseManager.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM accounts;", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var accounts = new List<object>();

        while (await reader.ReadAsync())
        {
            accounts.Add(new
            {
                id = reader["id"],
                swid = reader["swid"],
                referenceid = reader["referenceid"],
                email = reader["email"],
                penguinword = reader["penguinword"],
                status = reader["status"],
                displayname = reader["displayname"],
                proposedstatus = reader["proposedstatus"],
                registeredday = reader["registeredday"]

            });
        }

        await ctx.Response.WriteAsJsonAsync(accounts);
    }
}

