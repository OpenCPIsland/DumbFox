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

public class FriendshipPut
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][Mix] Got FriendShip request");
        await using var conn = await DatabaseManager.OpenConnectionAsync();

        AddFriendshipResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        AddFriendshipRequest obj = JsonSerializer.Deserialize<AddFriendshipRequest>((string)ctx.Items["DecryptedModel"]);

        var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());

        if (session == null)
        {
            throw new Exception("Current Session not found");
        }

        FriendDB friend = await DatabaseManager.GetFriendsId(obj.InvitationId);

        if (friend == null)
        {
            throw new Exception("Given InvitationId or NotificationId not found.");
        }

        var loadedProfile = await DatabaseManager.GetProfilesSwid(friend.hisID);

        if (loadedProfile == null)
        {
            throw new Exception("From Given Swid from Profile not found");
        }


        await using var cmd0 = new MySqlCommand(@"UPDATE friends SET isfriend = 1 WHERE meID = @swid AND id= @id;", conn);
        cmd0.Parameters.AddWithValue("@swid", friend.meID);
        cmd0.Parameters.AddWithValue("@id", friend.Id);
        await cmd0.ExecuteNonQueryAsync();

        await using var cmd1 = new MySqlCommand(@"UPDATE friends SET isfriend = 1, isrequested = 0 WHERE meID = @swid AND hisID= @id;", conn);
        cmd1.Parameters.AddWithValue("@swid", friend.hisID);
        cmd1.Parameters.AddWithValue("@id", friend.meID);
        await cmd1.ExecuteNonQueryAsync();

        result = new AddFriendshipResponse
        {
            Notification = new AddFriendshipNotification
            {
                Friend = new User
                {
                    UserId = friend.hisID,
                    HashedUserId = DumbfoxEncryption.EncryptB64(friend.hisID, Convert.FromBase64String((string)session["aesSecret"])),
                    DisplayName = friend.hisusername,
                    FirstName = "Test",
                    Avatar = new Avatar
                    {
                        Accessory = new AvatarProperty { SelectionKey = loadedProfile.Accessory, TintIndex = loadedProfile.AccessoryTintIndex, XOffset = loadedProfile.AccessoryXOffeset, YOffset = loadedProfile.AccessoryYOffeset },
                        Costume = new AvatarProperty { SelectionKey = loadedProfile.Costume, TintIndex = loadedProfile.CostumeTintIndex, XOffset = loadedProfile.CostumeXOffeset, YOffset = loadedProfile.CostumeYOffeset },
                        Brow = new AvatarProperty { SelectionKey = loadedProfile.Brow, TintIndex = loadedProfile.BrowTintIndex, XOffset = loadedProfile.BrowXOffeset, YOffset = loadedProfile.BrowYOffeset },
                        Eyes = new AvatarProperty { SelectionKey = loadedProfile.Eyes, TintIndex = loadedProfile.EyesTintIndex, XOffset = loadedProfile.EyesXOffeset, YOffset = loadedProfile.EyesYOffeset },
                        Hair = new AvatarProperty { SelectionKey = loadedProfile.Hair, TintIndex = loadedProfile.HairTintIndex, XOffset = loadedProfile.HairXOffeset, YOffset = loadedProfile.HairYOffeset },
                        Nose = new AvatarProperty { SelectionKey = loadedProfile.Nose, TintIndex = loadedProfile.NoseTintIndex, XOffset = loadedProfile.NoseXOffeset, YOffset = loadedProfile.NoseYOffeset },
                        Mouth = new AvatarProperty { SelectionKey = loadedProfile.Mouth, TintIndex = loadedProfile.MouthTintIndex, XOffset = loadedProfile.MouthXOffeset, YOffset = loadedProfile.MouthYOffeset },
                        Skin = new AvatarProperty { SelectionKey = loadedProfile.Skin, TintIndex = loadedProfile.SkinTintIndex, XOffset = loadedProfile.SkinXOffeset, YOffset = loadedProfile.SkinYOffeset },
                        Hat = new AvatarProperty { SelectionKey = loadedProfile.Hat, TintIndex = loadedProfile.HatTintIndex, XOffset = loadedProfile.HatXOffeset, YOffset = loadedProfile.HatYOffeset }
                    }
                },

                FriendshipInvitationId = friend.Id,
                IsTrusted = friend.istrusted,
                Created = friend.timestamp,
                NotificationId = friend.Id,
                SequenceNumber = 3

            }
        };

        var json = JsonSerializer.Serialize(result, options);
        byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

        ctx.Response.ContentLength = encrypted.Length;

        ctx.Response.ContentType = "application/mix";
        await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
    }
}

