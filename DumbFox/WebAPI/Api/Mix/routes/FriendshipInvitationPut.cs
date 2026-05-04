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

public class FriendshipInvitationPut
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][Mix] Got FriendShipInvitation request");

        AddFriendshipInvitationResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        AddFriendshipInvitationRequest obj = JsonSerializer.Deserialize<AddFriendshipInvitationRequest>((string)ctx.Items["DecryptedModel"]);

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

        var getUserName = await DatabaseManager.GetAccountUserName(obj.InviteeDisplayName);
       
        if (getUserName == null)
        {
            throw new Exception("Current Username not found");
        }

        long notfiid = await DatabaseManager.InsertFriend((string)getUserName["swid"], obj.InviteeDisplayName, (string)session["swid"], (string)session["username"]);

        var loadedOtherProfile = await DatabaseManager.GetProfilesSwid((string)getUserName["swid"]);

        if (loadedOtherProfile == null)
        {
            throw new Exception("From Given Swid from Profile not found");
        }

        result = new AddFriendshipInvitationResponse
        {
            Notification = new AddFriendshipInvitationNotification
            {
                Created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SequenceNumber = (long)session["seqcencenumber"],
                NotificationId = (long)session["seqcencenumber"],
                Invitation = new FriendshipInvitation { FriendDisplayName = (string)getUserName["username"], FriendshipInvitationId = notfiid, IsInviter = true, IsTrusted = false },
                Friend = new User {
                UserId = loadedOtherProfile.Swid,
                HashedUserId = DumbfoxEncryption.EncryptB64(loadedOtherProfile.Swid, Convert.FromBase64String((string)session["aesSecret"])),
                DisplayName = (string)getUserName["username"],
                FirstName = "Test",
                Avatar = new Avatar
                {
                    Accessory = new AvatarProperty { SelectionKey = loadedOtherProfile.Accessory, TintIndex = loadedOtherProfile.AccessoryTintIndex, XOffset = loadedOtherProfile.AccessoryXOffeset, YOffset = loadedOtherProfile.AccessoryYOffeset },
                    Costume = new AvatarProperty { SelectionKey = loadedOtherProfile.Costume, TintIndex = loadedOtherProfile.CostumeTintIndex, XOffset = loadedOtherProfile.CostumeXOffeset, YOffset = loadedOtherProfile.CostumeYOffeset },
                    Brow = new AvatarProperty { SelectionKey = loadedOtherProfile.Brow, TintIndex = loadedOtherProfile.BrowTintIndex, XOffset = loadedOtherProfile.BrowXOffeset, YOffset = loadedOtherProfile.BrowYOffeset },
                    Eyes = new AvatarProperty { SelectionKey = loadedOtherProfile.Eyes, TintIndex = loadedOtherProfile.EyesTintIndex, XOffset = loadedOtherProfile.EyesXOffeset, YOffset = loadedOtherProfile.EyesYOffeset },
                    Hair = new AvatarProperty { SelectionKey = loadedOtherProfile.Hair, TintIndex = loadedOtherProfile.HairTintIndex, XOffset = loadedOtherProfile.HairXOffeset, YOffset = loadedOtherProfile.HairYOffeset },
                    Nose = new AvatarProperty { SelectionKey = loadedOtherProfile.Nose, TintIndex = loadedOtherProfile.NoseTintIndex, XOffset = loadedOtherProfile.NoseXOffeset, YOffset = loadedOtherProfile.NoseYOffeset },
                    Mouth = new AvatarProperty { SelectionKey = loadedOtherProfile.Mouth, TintIndex = loadedOtherProfile.MouthTintIndex, XOffset = loadedOtherProfile.MouthXOffeset, YOffset = loadedOtherProfile.MouthYOffeset },
                    Skin = new AvatarProperty { SelectionKey = loadedOtherProfile.Skin, TintIndex = loadedOtherProfile.SkinTintIndex, XOffset = loadedOtherProfile.SkinXOffeset, YOffset = loadedOtherProfile.SkinYOffeset },
                    Hat = new AvatarProperty { SelectionKey = loadedOtherProfile.Hat, TintIndex = loadedOtherProfile.HatTintIndex, XOffset = loadedOtherProfile.HatXOffeset, YOffset = loadedOtherProfile.HatYOffeset }
                } }
            }
        };

        var json = JsonSerializer.Serialize(result, options);
        byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

        ctx.Response.ContentLength = encrypted.Length;

        ctx.Response.ContentType = "application/mix";
        await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
    }
}

