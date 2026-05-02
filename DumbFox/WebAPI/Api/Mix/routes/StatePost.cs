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

    public class StatePost
    {
        public static async Task Handle(HttpContext ctx)
        {
            Console.WriteLine("[WebAPI][Mix] Got StatePost request");

            GetStateResponse? result;
            ctx.Response.ContentType = "application/json;charset=utf-8";
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());

            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedProfile = await DatabaseManager.GetProfilesSwid((string)session["swid"]);

            if (loadedProfile == null)
            {
                throw new Exception("From Given Swid from Profile not found");
            }


            result = new GetStateResponse
            {
                Users = await DatabaseManager.getFriendshipUsers((string)session["swid"], (string)session["aesSecret"]),
                Friendships = await DatabaseManager.getFriendships((string)session["swid"]),
                FriendshipInvitations = await DatabaseManager.getFriendshipInvations((string)session["swid"]),
                Alerts = await DatabaseManager.getAlerts((string)session["swid"]),
                ChatThreads = await DatabaseManager.getChatThreads((string)session["swid"], (string)session["aesSecret"])
            };

            var json = JsonSerializer.Serialize(result, options);

            byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

            ctx.Response.ContentLength = encrypted.Length;

            ctx.Response.ContentType = "application/mix";
            await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
        }
    }
}
