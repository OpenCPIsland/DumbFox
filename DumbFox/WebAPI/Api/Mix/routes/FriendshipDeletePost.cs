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

public class FriendshipDeletePost
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][Mix] Got FriendShipDelete request");
        await using var conn = await DatabaseManager.OpenConnectionAsync();

        RemoveFriendshipResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        RemoveFriendshipRequest obj = JsonSerializer.Deserialize<RemoveFriendshipRequest>((string)ctx.Items["DecryptedModel"]);

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


        FriendDB friend = await DatabaseManager.GetFriendsSwid((string)session["swid"], obj.FriendUserId);

        if (friend == null)
        {
            throw new Exception("Given InvitationId or NotificationId not found.");
        }

        await using var cmd0 = new MySqlCommand(@"UPDATE friends SET isdeleted = 1, isfriend = 0 WHERE meID = @swid AND hisID= @id;", conn);
        cmd0.Parameters.AddWithValue("@swid", friend.meID);
        cmd0.Parameters.AddWithValue("@id", friend.hisID);
        await cmd0.ExecuteNonQueryAsync();

        await using var cmd1 = new MySqlCommand(@"UPDATE friends SET isdeleted = 1, isrequested = 0, isfriend = 0 WHERE meID = @swid AND hisID= @id;", conn);
        cmd1.Parameters.AddWithValue("@swid", friend.hisID);
        cmd1.Parameters.AddWithValue("@id", friend.meID);
        await cmd1.ExecuteNonQueryAsync();

        result = new RemoveFriendshipResponse
        {
            Notification = new RemoveFriendshipNotification
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

