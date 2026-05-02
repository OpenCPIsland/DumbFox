using Common.Utils;
using Common.Utils.Mix;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Api.Mix.routes
{
    public class SessionUserPut
    {
        private static int currentId = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % int.MaxValue);

        public static int GenerateId()
        {
            return ++currentId;
        }

        public static async Task Handle(HttpContext ctx)
        {
            Console.WriteLine("[WebAPI][Mix] Got SessionUserPut request");

            StartUserSessionResponse? result;
            ctx.Response.ContentType = "application/json;charset=utf-8";
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            
            var obj = (JsonElement)ctx.Items["DecryptedJson"];


            string publicKeyModulus = obj.GetProperty("PublicKeyModulus").GetString() ?? "";
            string publicKeyExponent = obj.GetProperty("PublicKeyExponent").GetString() ?? "";
            long sessionGroupId = obj.GetProperty("SessionGroupId").GetInt64();
            int protocolVersion = obj.GetProperty("ProtocolVersion").GetInt16();
            string userId = obj.GetProperty("UserId").GetString() ?? "";
            // long? timestamp = obj.GetProperty("Timestamp");


            byte[] modulus = Convert.FromBase64String(publicKeyModulus);
            byte[] exponent = Convert.FromBase64String(publicKeyExponent);

            byte[] aesKey = DumbfoxEncryption.RandomAes256Key();

            byte[] encrypted;
            using (RSA rsa = DumbfoxEncryption.BuildRsaFromModExp(modulus, exponent))
            {
                encrypted = DumbfoxEncryption.RsaEncryptPkcs1(rsa, aesKey);
            }

            string ekB64 = Convert.ToBase64String(encrypted);

            int sessionId = GenerateId();

            result = new StartUserSessionResponse
            {
                Status = BaseResponseStatus.OK.ToString(),
                EncryptedSymmetricKey = ekB64,
                SessionId = sessionId,
                HashedUserId = DumbfoxEncryption.EncryptB64(userId, aesKey)
            };

            await using var conn = await DatabaseManager.OpenConnectionAsync();
            await using var cmd0 = new MySqlCommand("UPDATE sessionusers SET sessionId = @SessionId, aesSecret = @aesSecret WHERE access_token = @AccessToken;", conn);
            cmd0.Parameters.AddWithValue("@SessionId", sessionId);
            cmd0.Parameters.AddWithValue("@aesSecret", Convert.ToBase64String(aesKey));
            cmd0.Parameters.AddWithValue("@AccessToken", ctx.Request.Headers["X-Mix-OneIdToken"].ToString());
            await cmd0.ExecuteNonQueryAsync();

            var json = JsonSerializer.Serialize(result, options);

            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            ctx.Response.ContentLength = bytes.Length;

            await ctx.Response.WriteAsync(json);
        }
    }
}
