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
    public class OfficialAccountAllPost
    {
        public static async Task Handle(HttpContext ctx)
        {
            Console.WriteLine("[WebAPI][Mix] Got OfficialAccountAll request");

            OfficialAccountsResponse? result;
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());
            var obj = (JsonElement)ctx.Items["DecryptedJson"];


            result = new OfficialAccountsResponse
            {
                OfficialAccountIds = [
                    //new GuestOfficialAccount { OaId = "OA-72844169130089556", OaName = "Mix Bot", IsAvailable = true, CanUnfollow = false },
                    new GuestOfficialAccount { OaId = "OA-284650157239423", OaName = "Disney Music", IsAvailable = true, CanUnfollow = true },
                    new GuestOfficialAccount { OaId = "OA-605752426948016", OaName = "Disney Movies", IsAvailable = true, CanUnfollow = true },
                    new GuestOfficialAccount { OaId = "OA-72844169130089556", OaName = "Disney LOL", IsAvailable = true, CanUnfollow = true },
                    new GuestOfficialAccount { OaId = "OA-5211780345359782", OaName = "Radio Disney", IsAvailable = true, CanUnfollow = true },
                    new GuestOfficialAccount { OaId = "OA-401824450246838", OaName = "Oh My Disney", IsAvailable = true, CanUnfollow = false },
                    new GuestOfficialAccount { OaId = "OA-183407994057410", OaName = "Disney XD", IsAvailable = true, CanUnfollow = true },
                    new GuestOfficialAccount { OaId = "OA-90469282549296078", OaName = "Disney Channel", IsAvailable = true, CanUnfollow = true }
                ]
            };

            var json = JsonSerializer.Serialize(result, options);
            byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

            ctx.Response.ContentLength = encrypted.Length;

            ctx.Response.ContentType = "application/mix";
            await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
        }
    }
}
