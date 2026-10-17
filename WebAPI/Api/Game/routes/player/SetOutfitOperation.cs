using Common.Utils;
using Common.Utils.Game;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Api.Game.routes.player;

public static class SetOutfitOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][GameRoutes][PlayerApi] Got SetOutfitOperation");
        object? result = new { Code = 999 };
        ctx.Response.ContentType = "application/json;charset=utf-8";
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string accesstoken = "";
        if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            string[] words = authHeader.ToString().Split(",");
            var decodedBytes = Convert.FromBase64String(words[0].Replace("Basic ", ""));
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            accesstoken = decodedString.Replace(":", "");
        }

        var session = await DatabaseManager.GetSessionAccessToken(accesstoken);

        await using var conn = await DatabaseManager.OpenConnectionAsync();
        ctx.Request.EnableBuffering();

        using StreamReader reader = new StreamReader(
            ctx.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        string body = await reader.ReadToEndAsync();

        ctx.Request.Body.Position = 0;

        PlayerOutfit? wearRequest = JsonSerializer.Deserialize<PlayerOutfit>(body);

        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            await using (var unwearCmd = new MySqlCommand(@"
        UPDATE inventorys 
        SET iswearing = 0 
        WHERE swid = @swid;", conn, transaction))
            {
                unwearCmd.Parameters.AddWithValue("@swid", (string)session["swid"]);
                await unwearCmd.ExecuteNonQueryAsync();
            }

            foreach (var equipmentId in wearRequest.parts)
            {
                await using var wearCmd = new MySqlCommand(@"
            UPDATE inventorys 
            SET iswearing = 1 
            WHERE swid = @swid AND id = @id;", conn, transaction);

                wearCmd.Parameters.AddWithValue("@swid", (string)session["swid"]);
                wearCmd.Parameters.AddWithValue("@id", equipmentId);

                await wearCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        result = new SignedResponse<object>
        {
            data = new
            {
               parts = await DatabaseManager.getCurrentOutfit((string)session["swid"])
            }
        };

        var json = JsonSerializer.Serialize(result, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

