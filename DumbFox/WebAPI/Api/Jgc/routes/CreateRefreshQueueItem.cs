using Common.Utils;
using Common.Utils.Game;
using Common.Utils.Jgc;
using Common.Utils.Mix;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAPI;
using WebAPI.Api.Jgc;

namespace WebAPI.Api.Jgc.routes;

public class CreateRefreshQueueItem
{
    private static string GenerateHex(int bytesLength)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(bytesLength);
        return Convert.ToHexString(bytes).ToLower();
    }

    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][JGC] Got Refreshtoken request");
        await using var conn = await DatabaseManager.OpenConnectionAsync();
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            // PropertyNamingPolicy = JsonNamingPolicy.Never,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        await using var cmd1 = new MySqlCommand(@"SELECT * FROM sessionusers WHERE refresh_token = @refreshToken;", conn);
        cmd1.Parameters.AddWithValue("@refreshToken", ctx.Request.RouteValues["refreshToken"]?.ToString());

        await using var reader0 = await cmd1.ExecuteReaderAsync();
        if (await reader0.ReadAsync())
        {

            var eTagData = new
            {
                profileETag = Md5Hex(reader0["swid"].ToString()),
                displayNameETag = Md5Hex(reader0["username"].ToString())
            };

            static string Md5Hex(string input)
            {
                using var md5 = MD5.Create();
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = md5.ComputeHash(bytes);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }

            // Build ETag (Node equivalent)
            string eTagJson = JsonSerializer.Serialize(eTagData);
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(eTagJson));
            string etag = $"W/\"{base64}\"";

            long eightDigitTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            RefreshResponse response = new RefreshResponse
            {
                data = new RefreshData
                {
                    etag = etag,
                    token = new Token
                    {
                        access_token = GenerateHex(16),
                        refresh_token = GenerateHex(16),
                        swid = reader0["swid"].ToString(),
                        ttl = 86400,
                        refresh_ttl = eightDigitTimestamp
                    }
                }
            };

            string json = JsonSerializer.Serialize(response, options);
            ctx.Response.ContentType = "application/json;charset=utf-8";

            await reader0.CloseAsync();

            await using var cmd0 = new MySqlCommand("UPDATE sessionusers SET refresh_token = @rrfr, access_token = @accesstoken WHERE refresh_token = @RefreshToken;", conn);
            cmd0.Parameters.AddWithValue("@rrfr", response.data.token.refresh_token);
            cmd0.Parameters.AddWithValue("@accesstoken", response.data.token.access_token);
            cmd0.Parameters.AddWithValue("@RefreshToken", ctx.Request.RouteValues["refreshToken"]?.ToString());
            await cmd0.ExecuteNonQueryAsync();

            await ctx.Response.WriteAsync(json);
            return;
        }
        else
        {
            await ResponseHelper.WriteJgcError(ctx, CategoryTypes.FAILURE_BY_DESIGN, GenericCode.AUTHORIZATION_INVALID_OR_EXPIRED_TOKEN, 400);
        }
    }
}
