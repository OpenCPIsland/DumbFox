using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Utils;
using Common.Utils.Mix;

namespace WebAPI.Api.Mix.routes
{
    public class LanguagePreferencePost
    {
        public static async Task Handle(HttpContext ctx)
        {
            Console.WriteLine("[WebAPI][Mix] Got LanguagePreference request");

            BaseResponse? result;
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());
            var obj = (JsonElement)ctx.Items["DecryptedJson"];


            result = new BaseResponse
            {
                Status = "OK"
            };

            var json = JsonSerializer.Serialize(result, options);
            byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

            ctx.Response.ContentLength = encrypted.Length;

            ctx.Response.ContentType = "application/mix";
            await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
        }
    }
}
