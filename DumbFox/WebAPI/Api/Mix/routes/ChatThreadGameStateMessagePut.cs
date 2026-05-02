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

public class ChatThreadGameStateMessagePut
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][Mix] Got ChatThreadTextMessage request");
        await using var conn = await DatabaseManager.OpenConnectionAsync();

        AddChatThreadGameStateMessageResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        AddChatThreadGameStateMessageRequest obj = JsonSerializer.Deserialize<AddChatThreadGameStateMessageRequest>((string)ctx.Items["DecryptedModel"]);

        var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());

        if (session == null)
        {
            throw new Exception("Current Session not found");
        }

        result = new AddChatThreadGameStateMessageResponse
        {
            Notification = new AddChatThreadGameStateMessageNotification
            {
                Message = await DatabaseManager.InsertChatThreadTypeMessage<GameStateChatMessage>(obj.ChatThreadId, obj.ClientChatMessageId, (string)session["swid"], (string)session["username"], obj.GameName, "gamename", "isgame", ChatMessageTypes.GAME_STATE),
                Created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SequenceNumber = 3,
                NotificationId = obj.ChatThreadId
            }
        };

        var json = JsonSerializer.Serialize(result, options);
        byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

        ctx.Response.ContentLength = encrypted.Length;

        ctx.Response.ContentType = "application/mix";
        await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
    }
}

