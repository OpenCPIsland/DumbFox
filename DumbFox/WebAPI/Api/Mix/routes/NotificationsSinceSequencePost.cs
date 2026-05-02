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
            var obj = (JsonElement)ctx.Items["DecryptedJson"];


            result = new GetNotificationsResponse
            {
                NotificationSequenceCounter = obj.GetProperty("SequenceNumber").GetInt16(),
                AddFriendship = await DatabaseManager.getNotificationFriendships((string)session["swid"], (string)session["aesSecret"]),
                RemoveFriendship = await DatabaseManager.getNotificationFriendshipRemoves((string)session["swid"], (string)session["aesSecret"]),
                AddFriendshipInvitation = await DatabaseManager.getNotificationFriendshipInvations((string)session["swid"], (string)session["aesSecret"]),
                RemoveFriendshipInvitation =  await DatabaseManager.getNotificationFriendshipInvationRemoves((string)session["swid"], (string)session["aesSecret"]),
                AddAlert = await DatabaseManager.getNotificationAlerts((string)session["swid"]),
                ClearAlert = await DatabaseManager.getNotificationAlertClears((string)session["swid"]),
                AddChatThread = await DatabaseManager.getChatThreadNotifications((string)session["swid"], (string)session["aesSecret"]),
                AddChatThreadTextMessage = await DatabaseManager.getChatThreadTextNotifications((string)session["swid"]),
                AddChatThreadStickerMessage = await DatabaseManager.getChatThreadStickerNotifications((string)session["swid"]),
                AddChatThreadGagMessage = await DatabaseManager.getChatThreadGagNotifications((string)session["swid"]),
                AddChatThreadVideoMessage = await DatabaseManager.getChatThreadVideoNotifications((string)session["swid"]),
                AddChatThreadPhotoMessage = await DatabaseManager.getChatThreadPhotoNotifications((string)session["swid"]),
                AddChatThreadGameStateMessage = await DatabaseManager.getChatThreadGameStateNotifications((string)session["swid"]),
                AddChatThreadGameEventMessage = await DatabaseManager.getChatThreadGameEventNotifications((string)session["swid"]),
            };

            var json = JsonSerializer.Serialize(result, options);
            byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

            ctx.Response.ContentLength = encrypted.Length;

            ctx.Response.ContentType = "application/mix";
            await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
        }
    }
}
