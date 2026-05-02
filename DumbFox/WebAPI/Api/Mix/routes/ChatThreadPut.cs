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

public class ChatThreadPut
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][Mix] Got ChatThread request");
        await using var conn = await DatabaseManager.OpenConnectionAsync();

        AddChatThreadResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        AddChatThreadRequest obj = JsonSerializer.Deserialize<AddChatThreadRequest>((string)ctx.Items["DecryptedModel"]);

        var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());

        if (session == null)
        {
            throw new Exception("Current Session not found");
        }

        result = new AddChatThreadResponse
        {
            Notification = new AddChatThreadNotification
            {
                ChatThread = await DatabaseManager.InsertChatThread(obj.ChatThreadType, obj.MemberUserIds, (string)session["swid"], (string)session["aesSecret"]),
                WelcomingUserId = obj.UserId,
                Created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SequenceNumber = 3,
                NotificationId = 1
            }
        };

        var json = JsonSerializer.Serialize(result, options);
        byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

        ctx.Response.ContentLength = encrypted.Length;

        ctx.Response.ContentType = "application/mix";
        await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
    }
}

