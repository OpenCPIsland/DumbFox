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

namespace WebAPI.Api.Game.routes.penguin;

public class SetProfileOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI] Got SetProfile request");
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

        Profile? currentColor = JsonSerializer.Deserialize<Profile>(body);

        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            await using (var updateCmd = new MySqlCommand(@"UPDATE profiles SET colour = @colour WHERE swid = @swid;", conn, transaction))
            {
                updateCmd.Parameters.AddWithValue("@swid", (string)session["swid"]);
                updateCmd.Parameters.AddWithValue("@colour", currentColor.colour);
                await updateCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var loadedProfile = await DatabaseManager.GetProfilesSwid((string)session["swid"]);

        DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(loadedProfile.RegisterTimestamp);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        int days = (int)(now - date).TotalDays;

        result = new SignedResponse<object>
        {
            data = new Profile
            {
                daysOld = days,
                colour = loadedProfile.Colour,
            }
        };

        var json = JsonSerializer.Serialize(result, options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

