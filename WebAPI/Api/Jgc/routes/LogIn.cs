using Common.Utils;
using Common.Utils.Jgc;
using Common.Utils.Mix;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
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

public class LogIn
{
	private static string GenerateHex(int bytesLength)
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(bytesLength);
		return Convert.ToHexString(bytes).ToLower();
	}

	
	public static async Task Handle(HttpContext ctx)
	{
		Console.WriteLine("[WebAPI][JgcRoutes] Got LogIn");
        await using var conn = await DatabaseManager.OpenConnectionAsync();
        // Allow multiple reads
        ctx.Request.EnableBuffering();

		using StreamReader reader = new StreamReader(
			ctx.Request.Body,
			Encoding.UTF8,
			detectEncodingFromByteOrderMarks: false,
			leaveOpen: true);

		string body = await reader.ReadToEndAsync();
		
		ctx.Request.Body.Position = 0;

        Console.WriteLine($"[WebAPI][JgcRoutes][From Client by LogIn] Body: {body}");

        string apiKey = "";
        if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            apiKey = authHeader.ToString().Replace("APIKEY ", "");
        }

        if (await DatabaseManager.GetSessionApiKey(apiKey) == null)
		{
            await ResponseHelper.WriteJgcError(ctx, CategoryTypes.FAILURE_BY_DESIGN, GenericCode.API_KEY_INVALID, 400);
        }
		else
		{

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                // PropertyNamingPolicy = JsonNamingPolicy.Never,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            ctx.Response.Headers["api-key"] = apiKey;

            LogInRequest? obj = JsonSerializer.Deserialize<LogInRequest>(body);

            bool IsValidEmail(string email)
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                try
                {
                    var addr = new MailAddress(email);
                    return addr.Address == email;
                }
                catch
                {
                    return false;
                }
            }

            string dbobject = "username";
            if (IsValidEmail(obj.loginValue))
            {
                dbobject = "email";
            }

            await using var cmd1 = new MySqlCommand(@"SELECT * FROM accounts WHERE "+dbobject+ " = @username AND status IN ('PERMA', 'BANNED', 'LOCKED', 'ACTIVE', 'NO_EMAIL_VERFIED');", conn);
            cmd1.Parameters.AddWithValue("@username", obj.loginValue);

            await using var reader0 = await cmd1.ExecuteReaderAsync();

            if (await reader0.ReadAsync())
            {
                bool valid = BCrypt.Net.BCrypt.Verify(obj.password, reader0["penguinword"].ToString());
                if (!valid)
                {
                    Console.WriteLine("Invaild Login");
                    await ResponseHelper.WriteJgcError(ctx, CategoryTypes.FAILURE_BY_DESIGN, LoginCode.AUTHORIZATION_CREDENTIALS, 400);
                    return;
                }
                
                if (reader0["status"].ToString() == "BANNED")
                {
                    await ResponseHelper.WriteJgcError(ctx, CategoryTypes.FAILURE_BY_DESIGN, LoginCode.PROFILE_USER_BANNED, 400);
                    return;
                }

                if (reader0["status"].ToString() == "PERMA")
                {
                    await ResponseHelper.WriteJgcError(ctx, CategoryTypes.FAILURE_BY_DESIGN, LoginCode.PROFILE_DISABLED, 400);
                    return;
                }

                var eTagData = new
                {
                    profileETag = Md5Hex(reader0["swid"].ToString()),
                    displayNameETag = Md5Hex(reader0["displayname"].ToString())
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

                LogInResponse response = new LogInResponse
                {
                    data = new LogInData
                    {
                        etag = etag,

                        profile = new Profile
                        {
                            swid = reader0["swid"].ToString(),
                            username = reader0["username"].ToString()
                        },
                        displayName = new DisplayName
                        {
                            displayName = reader0["displayname"].ToString(),
                            proposedDisplayName = $"CP{reader0["id"].ToString()}"
                        },
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

                await reader0.DisposeAsync();

                DatabaseManager.GetSessionApiKeyAndAddtoSessionUsers(apiKey, response.data.token.access_token, response.data.profile.swid, response.data.token.refresh_token, response.data.profile.username, ctx.Request.RouteValues["clientId"]?.ToString());


                ctx.Response.ContentType = "application/json;charset=utf-8";
                await ctx.Response.WriteAsync(json);

            }
        }

    }
}
