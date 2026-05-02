using Common.Utils;
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

public class GetProfile
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

        string accesstoken = "";
        if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            accesstoken = authHeader.ToString().Replace("BEARER ", "");
        }

        if (await DatabaseManager.GetSessionAccessToken(accesstoken) == null)
        {
            await ResponseHelper.WriteJgcError(ctx, CategoryTypes.FAILURE_BY_DESIGN, GenericCode.AUTHORIZATION_INVALID_OR_EXPIRED_TOKEN, 400);
        }
        else
        {

            var session = await DatabaseManager.GetSessionAccessToken(accesstoken);

            var eTagData = new
            {
                profileETag = Md5Hex(session["swid"].ToString()),
                displayNameETag = Md5Hex(session["username"].ToString())
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

            ProfileResponse response = new ProfileResponse
            {
                data = new ProfileData
                {
                    profile = new Profile
                    {
                        swid = session["swid"].ToString(),
                        username = session["username"].ToString()
                    },

                    displayName = new DisplayNameData
                    {
                        displayName = session["username"].ToString(),
                        proposedDisplayName = session["username"].ToString(),//$"CP{session["swid"].ToString()}"
                        moderatedStatusDate = new DateTime()
                    }
                 
                }
            };

            string json = JsonSerializer.Serialize(response, options);
            ctx.Response.ContentType = "application/json;charset=utf-8";


            await ctx.Response.WriteAsync(json);
            return;
        }
    }
}

