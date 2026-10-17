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

namespace WebAPI.Api.Mix.routes;

public class FriendshipTrustDeletePost
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][MixRoutes] Got FriendShipTrustDelete request");

        RemoveFriendshipTrustResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        RemoveFriendshipTrustRequest obj = JsonSerializer.Deserialize<RemoveFriendshipTrustRequest>((string)ctx.Items["DecryptedModel"]);

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

        var getSrvSett = await DatabaseManager.GetServerSettings();
        if (getSrvSett == null)
        {
            throw new Exception("No Server Settings is found?");
        }

        result = new RemoveFriendshipTrustResponse
        {
            Notification = new RemoveFriendshipTrustNotification
            {
                FriendUserId = obj.FriendUserId,
                Created = (long)session["timestamp"],
                SequenceNumber = getSrvSett.Sequence,
                NotificationId = getSrvSett.Sequence,
            }
        };

        var json = JsonSerializer.Serialize(result, options);
        byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

        ctx.Response.ContentLength = encrypted.Length;

        ctx.Response.ContentType = "application/mix";
        await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
    }
}

