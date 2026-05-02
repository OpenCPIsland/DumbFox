using Common.Utils;
using Common.Utils.Game;
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

namespace WebAPI.Api.Mix.routes
{
    public class SessionUserDeletePost
    {
        public static string GenerateResponseJson(JsonElement obj, HttpContext ctx, Dictionary<string, object> session)
        {
            object response = new 
            {
                Status = "OK"
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(response, options);
        }

        public static async Task Handle(HttpContext ctx)
        {
            Console.WriteLine("[WebAPI][Mix] Got SessionUserDelete request");

            string accesstoken = "";
            if (ctx.Request.Headers.TryGetValue("X-Mix-OneIdToken", out var authHeader))
            {
                accesstoken = authHeader;
            }
            var session = await DatabaseManager.GetSessionAccessToken(accesstoken);
            if (session != null)
            {
                // Allow multiple reads
                ctx.Request.EnableBuffering();

                using StreamReader reader = new StreamReader(
                    ctx.Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                using var ms = new MemoryStream();
                await reader.BaseStream.CopyToAsync(ms);
                byte[] blob = ms.ToArray();

                ctx.Request.Body.Position = 0;

                ctx.Response.Headers.Remove("Transfer-Encoding");

                ctx.Response.Headers["X-Cloud-Trace-Context"] = MixRoutes.Generate();
                ctx.Response.Headers["Server"] = "Google Frontend";
                ctx.Response.Headers["Alt-Svc"] = "quic=\":443\"; ma=2592000; v=\"44,43,39,35\"";


                
                string decrypted = DumbfoxEncryption.DecryptIvPrependedBytes(blob, Convert.FromBase64String((string)session["aesSecret"]));
                Console.WriteLine(decrypted);

                Console.WriteLine($"[WebAPI][MIX] Decrypted body: {decrypted}");

                JsonElement obj = JsonSerializer.Deserialize<JsonElement>(decrypted);

                string response = GenerateResponseJson(obj, ctx, session);
                byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(response, Convert.FromBase64String((string)session["aesSecret"]));
                ctx.Response.ContentLength = encrypted.Length;
                ctx.Response.ContentType = "application/mix";

                await using var conn = await DatabaseManager.OpenConnectionAsync();
                await using var cmd = new MySqlCommand("DELETE FROM sessionusers WHERE access_token = @accessToken;", conn);
                cmd.Parameters.AddWithValue("@accessToken", ctx.Request.Headers["X-Mix-OneIdToken"].ToString());
                await cmd.ExecuteNonQueryAsync();
                await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
            }
            else
            {
                ctx.Request.EnableBuffering();
                using StreamReader reader = new StreamReader(
                    ctx.Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);
                string body = await reader.ReadToEndAsync();
                ctx.Request.Body.Position = 0;

                Console.WriteLine($"[WebAPI][MIX] body: {body}");

                ctx.Response.Headers["X-Cloud-Trace-Context"] = MixRoutes.Generate();
                ctx.Response.Headers["Server"] = "Google Frontend";
                ctx.Response.Headers["Alt-Svc"] = "quic=\":443\"; ma=2592000; v=\"44,43,39,35\"";

                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json;charset=utf-8";

                BaseResponse response = new BaseResponse
                {
                    Status = BaseResponseStatus.UNAUTHORIZED_ONEID_TOKEN.ToString(),
                };

                var options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(response, options);

                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                ctx.Response.ContentLength = bytes.Length;

                await ctx.Response.WriteAsync(json);
            }
        }
    }
}
