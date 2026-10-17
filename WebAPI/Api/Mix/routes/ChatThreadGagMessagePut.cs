using Common.Utils;
using Common.Utils.Mix;
using Microsoft.AspNetCore.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Api.Mix.routes;

public class ChatThreadGagMessagePut
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][MixRoutes] Got ChatThreadTextMessage request");
        await using var conn = await DatabaseManager.OpenConnectionAsync();

        AddChatThreadGagMessageResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        AddChatThreadGagMessageRequest obj = JsonSerializer.Deserialize<AddChatThreadGagMessageRequest>((string)ctx.Items["DecryptedModel"]);

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

        result = new AddChatThreadGagMessageResponse
        {
            Notification = new AddChatThreadGagMessageNotification
            {
                Message = await DatabaseManager.InsertChatThreadTypeMessage<GagChatMessage>(obj.ChatThreadId, obj.ClientChatMessageId, (string)session["swid"], (string)session["username"], obj.ContentId, "contentid", "isgag", ChatMessageTypes.GAG),
                Created = (long)session["timestamp"],
                SequenceNumber = getSrvSett.Sequence,
                NotificationId = getSrvSett.Sequence
            }
        };

        var json = JsonSerializer.Serialize(result, options);
        byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

        ctx.Response.ContentLength = encrypted.Length;

        ctx.Response.ContentType = "application/mix";
        await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
    }
}

