using Common.Utils;
using Common.Utils.Mix;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using WebAPI.Api.Jgc.routes;

namespace WebAPI.Api.Mix.routes
{
    public class DisplaynamePut
    {
        public static async Task Handle(HttpContext ctx)
        {
            Console.WriteLine("[WebAPI][MixRoutes] Got DisplaynamePut request");

            SetDisplayNameResponse? result;
            await using var conn = await DatabaseManager.OpenConnectionAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());

            var body = ctx.Items["DecryptedModel"] as string;
            var obj = JsonSerializer.Deserialize<SetDisplayNameRequest>(body);


            bool IsValidUsername(string username)
            {
                if (string.IsNullOrWhiteSpace(username))
                    return false;

                // Check allowed characters
                if (!Regex.IsMatch(username, @"^[A-Za-z0-9_]+$"))
                    return false;

                // Count digits
                int digitCount = username.Count(char.IsDigit);

                return digitCount >= 6 && digitCount <= 7;
            }

            string Normalize(string input)
            {
                if (string.IsNullOrWhiteSpace(input))
                    return string.Empty;

                var map = new Dictionary<char, char>
                {
                    ['4'] = 'a',
                    ['@'] = 'a',
                    ['3'] = 'e',
                    ['1'] = 'i',
                    ['!'] = 'i',
                    ['0'] = 'o',
                    ['5'] = 's',
                    ['$'] = 's',
                    ['7'] = 't',
                    [' '] = '_',
                    ['_'] = '_'
                };

                var sb = new StringBuilder();

                foreach (var c in input.ToLowerInvariant())
                {
                    if (map.TryGetValue(c, out var replacement))
                        sb.Append(replacement);
                    else if (char.IsLetterOrDigit(c))
                        sb.Append(c);
                    // ignore underscores and other separators
                }

                return sb.ToString();
            }

            bool ContainsBlacklistedWord(string username, string[] blacklist)
            {
                if (string.IsNullOrWhiteSpace(username))
                    return false;

                var lower = username.ToLowerInvariant();

                foreach (var word in blacklist)
                {
                    if (lower.Contains(word.ToLowerInvariant()))
                        return true;
                }

                return false;
            }

            var blacklistUser = new[]
            {
            "auntartic",
            "rockhopper",
            "gary",
            "dot",
            "rookie",
            "djcadance",
            "cadance",
            "rory",
            "klutzy",
            "herbert",
            "pufflehandler",
            "shellbeard",
            "testplayer"
        };


            bool ContainsBlacklistedSmart(string username, string[] blacklist)
            {
                var normalized = Normalize(username);

                foreach (var word in blacklist)
                {
                    if (normalized.Contains(word.ToLowerInvariant()))
                        return true;
                }

                return false;
            }


            bool IsBlacklistedUsername(string username, string[] blacklist)
            {
                if (ContainsBlacklistedWord(username, blacklist))
                    return false;
                if (ContainsBlacklistedSmart(username, blacklist))
                    return false;
                return true;
            }

                if (!string.IsNullOrWhiteSpace(obj.DisplayName))
                {
                 if (obj.DisplayName.Length < 3 || obj.DisplayName.Length > 16)
                    {
                        await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE_USERNAME_SIZE, 400, InputNameTypes.ProfileUsername);
                        return;
                    }

                 if (!IsBlacklistedUsername(obj.DisplayName, blacklistUser))
                 {
                        await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.DISALLOWED_VALUE, 400, InputNameTypes.ProfileUsername);
                        return;
                 }

                 if (IsValidUsername(obj.DisplayName))
                 {
                        await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE_FILTHY, 400, InputNameTypes.ProfileUsername);
                        return;
                 }

                await using var cmd = new MySqlCommand(@"SELECT * FROM accounts WHERE username = @username AND status IN ('PERMA', 'BANNED', 'LOCKED', 'ACTIVE', 'NO_EMAIL_VERFIED');", conn);
                cmd.Parameters.AddWithValue("@username", obj.DisplayName);

                await using var reader0 = await cmd.ExecuteReaderAsync();

                    if (await reader0.ReadAsync())
                    {
                        await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INUSE_VALUE, 400, InputNameTypes.ProfileUsername);
                        return;
                    }
                }


            await using var cmd1 = new MySqlCommand(@"UPDATE accounts SET username = @username, displayname = @username WHERE swid = @swid;", conn);
            cmd1.Parameters.AddWithValue("@swid", (string)session["swid"]);
            cmd1.Parameters.AddWithValue("@username", obj.DisplayName);
            await cmd1.ExecuteNonQueryAsync();

            result = new SetDisplayNameResponse
            {
                DisplayName = obj.DisplayName
            };

            var json = JsonSerializer.Serialize(result, options);
            byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

            ctx.Response.ContentLength = encrypted.Length;

            ctx.Response.ContentType = "application/mix";
            await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
        }
    }
}
