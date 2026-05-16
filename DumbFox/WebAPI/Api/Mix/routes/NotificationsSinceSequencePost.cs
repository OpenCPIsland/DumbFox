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

namespace WebAPI.Api.Mix.routes
{
    public class NotificationsSinceSequencePost
    {
        public static async Task Handle(HttpContext ctx)
        {
            //Console.WriteLine("[WebAPI][Mix] Got NotificationsSinceSequence request");

            GetNotificationsResponse? result;
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());
            GetNotificationsSinceSequenceRequest obj = JsonSerializer.Deserialize<GetNotificationsSinceSequenceRequest>((string)ctx.Items["DecryptedModel"]);

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


            result = new GetNotificationsResponse
            {
                NotificationSequenceCounter = getSrvSett.Sequence,
                AddFriendship = await DatabaseManager.getNotificationFriendships((string)session["swid"], (string)session["aesSecret"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                RemoveFriendship = await DatabaseManager.getNotificationFriendshipRemoves((string)session["swid"], (string)session["aesSecret"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                AddFriendshipInvitation = await DatabaseManager.getNotificationFriendshipInvations((string)session["swid"], (string)session["aesSecret"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                RemoveFriendshipInvitation =  await DatabaseManager.getNotificationFriendshipInvationRemoves((string)session["swid"], (string)session["aesSecret"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                AddAlert = await DatabaseManager.getNotificationAlerts((string)session["swid"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                ClearAlert = await DatabaseManager.getNotificationAlertClears((string)session["swid"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                AddChatThread = await DatabaseManager.getChatThreadNotifications((string)session["swid"], (string)session["aesSecret"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                AddChatThreadTextMessage = await DatabaseManager.getChatThreadTextNotifications((string)session["swid"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                AddChatThreadStickerMessage = await DatabaseManager.getChatThreadStickerNotifications((string)session["swid"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                AddChatThreadGagMessage = await DatabaseManager.getChatThreadGagNotifications((string)session["swid"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                AddChatThreadVideoMessage = await DatabaseManager.getChatThreadVideoNotifications((string)session["swid"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                AddChatThreadPhotoMessage = await DatabaseManager.getChatThreadPhotoNotifications((string)session["swid"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                AddChatThreadGameStateMessage = await DatabaseManager.getChatThreadGameStateNotifications((string)session["swid"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
                AddChatThreadGameEventMessage = await DatabaseManager.getChatThreadGameEventNotifications((string)session["swid"], ctx.Request.Headers["X-Mix-OneIdToken"].ToString()),
            };

            await using var conn = await DatabaseManager.OpenConnectionAsync();
            await using var cmd0 = new MySqlCommand("UPDATE serversettings SET sequence = @seq;", conn);
            cmd0.Parameters.AddWithValue("@seq", obj.SequenceNumber);
            await cmd0.ExecuteNonQueryAsync();

            var json = JsonSerializer.Serialize(result, options);
            byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

            ctx.Response.ContentLength = encrypted.Length;

            ctx.Response.ContentType = "application/mix";
            await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
        }
    }
}
