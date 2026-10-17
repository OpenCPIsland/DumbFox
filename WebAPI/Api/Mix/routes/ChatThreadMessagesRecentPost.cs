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

public class ChatThreadMessagesRecentPost
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][MixRoutes] Got ChatThreadMessagesRecent request");
        await using var conn = await DatabaseManager.OpenConnectionAsync();

        GetChatThreadMessagesResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        GetChatThreadMessagesRequest obj = JsonSerializer.Deserialize<GetChatThreadMessagesRequest>((string)ctx.Items["DecryptedModel"]);

        var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());

        if (session == null)
        {
            throw new Exception("Current Session not found");
        }

        result = new GetChatThreadMessagesResponse
        {
            Cursor = obj.Cursor,
            Text = await DatabaseManager.getChatThreadTexts(obj.ChatThreadId, (string)session["swid"]),
            Sticker = await DatabaseManager.getChatThreadStickers(obj.ChatThreadId, (string)session["swid"]),
            Gag = await DatabaseManager.getChatThreadGags(obj.ChatThreadId, (string)session["swid"]),
            Video = await DatabaseManager.getChatThreadVideos(obj.ChatThreadId, (string)session["swid"]),
            Photo = await DatabaseManager.getChatThreadPhotos(obj.ChatThreadId, (string)session["swid"]),
            GameState = await DatabaseManager.getChatThreadGameStates(obj.ChatThreadId, (string)session["swid"]),
            GameEvent = await DatabaseManager.getChatThreadGameEvents(obj.ChatThreadId, (string)session["swid"]),

        };

        var json = JsonSerializer.Serialize(result, options);
        byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

        ctx.Response.ContentLength = encrypted.Length;

        ctx.Response.ContentType = "application/mix";
        await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
    }
}

