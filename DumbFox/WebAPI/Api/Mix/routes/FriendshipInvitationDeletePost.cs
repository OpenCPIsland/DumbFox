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

public class FriendshipInvitationDeletePost
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][Mix] Got FriendShipInvitationDelete request");
        await using var conn = await DatabaseManager.OpenConnectionAsync();

        RemoveFriendshipInvitationResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        RemoveFriendshipInvitationRequest obj = JsonSerializer.Deserialize<RemoveFriendshipInvitationRequest>((string)ctx.Items["DecryptedModel"]);

        FriendDB friend = await DatabaseManager.GetFriendsId(obj.InvitationId);

        if (friend == null)
        {
            throw new Exception("Given InvitationId or NotificationId not found.");
        }

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

        await using var cmd0 = new MySqlCommand(@"UPDATE friends SET isdeleted = 1 WHERE meID = @swid AND id= @id;", conn);
        cmd0.Parameters.AddWithValue("@swid", friend.meID);
        cmd0.Parameters.AddWithValue("@id", friend.Id);
        await cmd0.ExecuteNonQueryAsync();

        await using var cmd1 = new MySqlCommand(@"UPDATE friends SET isdeleted = 1, isrequested = 0 WHERE meID = @swid AND hisID= @id;", conn);
        cmd1.Parameters.AddWithValue("@swid", friend.hisID);
        cmd1.Parameters.AddWithValue("@id", friend.meID);
        await cmd1.ExecuteNonQueryAsync();

        result = new RemoveFriendshipInvitationResponse
        {
            Notification = new RemoveFriendshipInvitationNotification 
            { 
                InvitationId = obj.InvitationId,
                Created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
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

