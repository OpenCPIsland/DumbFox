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
    public class AvatarPut
    {
        public static async Task Handle(HttpContext ctx)
        {
            Console.WriteLine("[WebAPI][MixRoutes] Got AvatarPut request");

            SetAvatarResponse? result;
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());

            var body = ctx.Items["DecryptedModel"] as string;
            var obj = JsonSerializer.Deserialize<SetAvatarRequest>(body);

            string NormalizeKey(string value, string fallback)
            {
                return value == "0" ? fallback : value;
            }
            
            if(obj.UserId == "Fake Friend")
            {
                Console.WriteLine("[WebAPI][Mix] Mixbot cant update:");
            }
            else
            {

                await using var conn = await DatabaseManager.OpenConnectionAsync();
                await using var cmd = new MySqlCommand(@"UPDATE profiles SET Accessory = @Accessory, AccessoryTintIndex = @AccessoryTintIndex, AccessoryXOffeset = @AccessoryXOffeset, AccessoryYOffeset = @AccessoryYOffeset, Costume = @Costume, CostumeTintIndex = @CostumeTintIndex, CostumeXOffeset = @CostumeXOffeset, CostumeYOffeset = @CostumeYOffeset, Brow = @Brow, BrowTintIndex = @BrowTintIndex, BrowXOffeset = @BrowXOffeset, BrowYOffeset = @BrowYOffeset, Eyes = @Eyes, EyesTintIndex = @EyesTintIndex, EyesXOffeset = @EyesXOffeset, EyesYOffeset = @EyesYOffeset, Hair = @Hair, HairTintIndex = @HairTintIndex, HairXOffeset = @HairXOffeset, HairYOffeset = @HairYOffeset, Nose = @Nose, NoseTintIndex = @NoseTintIndex, NoseXOffeset = @NoseXOffeset, NoseYOffeset = @NoseYOffeset, Mouth = @Mouth, MouthTintIndex = @MouthTintIndex, MouthXOffeset = @MouthXOffeset, MouthYOffeset = @MouthYOffeset, Skin = @Skin, SkinTintIndex = @SkinTintIndex, SkinXOffeset = @SkinXOffeset, SkinYOffeset = @SkinYOffeset, Hat = @Hat, HatTintIndex = @HatTintIndex, HatXOffeset = @HatXOffeset, HatYOffeset = @HatYOffeset  WHERE swid = @swid;", conn);
                
                cmd.Parameters.AddWithValue("@swid", (string)session["swid"]);
                cmd.Parameters.AddWithValue("@Accessory", NormalizeKey(obj.Avatar.Accessory.SelectionKey, "3"));
                cmd.Parameters.AddWithValue("@AccessoryTintIndex", (int)obj.Avatar.Accessory.TintIndex);
                cmd.Parameters.AddWithValue("@AccessoryXOffeset", obj.Avatar.Accessory.XOffset.ToString());
                cmd.Parameters.AddWithValue("@AccessoryYOffeset", obj.Avatar.Accessory.YOffset.ToString());
                cmd.Parameters.AddWithValue("@Costume", NormalizeKey(obj.Avatar.Costume.SelectionKey, "15"));
                cmd.Parameters.AddWithValue("@CostumeTintIndex", (int)obj.Avatar.Costume.TintIndex);
                cmd.Parameters.AddWithValue("@CostumeXOffeset", obj.Avatar.Costume.XOffset.ToString());
                cmd.Parameters.AddWithValue("@CostumeYOffeset", obj.Avatar.Costume.YOffset.ToString());
                cmd.Parameters.AddWithValue("@Brow", NormalizeKey(obj.Avatar.Brow.SelectionKey, "9"));
                cmd.Parameters.AddWithValue("@BrowTintIndex", (int)obj.Avatar.Brow.TintIndex);
                cmd.Parameters.AddWithValue("@BrowXOffeset", obj.Avatar.Brow.XOffset.ToString());
                cmd.Parameters.AddWithValue("@BrowYOffeset", obj.Avatar.Brow.YOffset.ToString());
                cmd.Parameters.AddWithValue("@Eyes", NormalizeKey(obj.Avatar.Eyes.SelectionKey, "54"));
                cmd.Parameters.AddWithValue("@EyesTintIndex", (int)obj.Avatar.Eyes.TintIndex);
                cmd.Parameters.AddWithValue("@EyesXOffeset", obj.Avatar.Eyes.XOffset.ToString());
                cmd.Parameters.AddWithValue("@EyesYOffeset", obj.Avatar.Eyes.YOffset.ToString());
                cmd.Parameters.AddWithValue("@Hair", NormalizeKey(obj.Avatar.Hair.SelectionKey, "69"));
                cmd.Parameters.AddWithValue("@HairTintIndex", (int)obj.Avatar.Hair.TintIndex);
                cmd.Parameters.AddWithValue("@HairXOffeset", obj.Avatar.Hair.XOffset.ToString());
                cmd.Parameters.AddWithValue("@HairYOffeset", obj.Avatar.Hair.YOffset.ToString());
                cmd.Parameters.AddWithValue("@Nose", NormalizeKey(obj.Avatar.Nose.SelectionKey, "106"));
                cmd.Parameters.AddWithValue("@NoseTintIndex", (int)obj.Avatar.Nose.TintIndex);
                cmd.Parameters.AddWithValue("@NoseXOffeset", obj.Avatar.Nose.XOffset.ToString());
                cmd.Parameters.AddWithValue("@NoseYOffeset", obj.Avatar.Nose.YOffset.ToString());
                cmd.Parameters.AddWithValue("@Mouth", NormalizeKey(obj.Avatar.Nose.SelectionKey, "194"));
                cmd.Parameters.AddWithValue("@MouthTintIndex", (int)obj.Avatar.Mouth.TintIndex);
                cmd.Parameters.AddWithValue("@MouthXOffeset", obj.Avatar.Mouth.XOffset.ToString());
                cmd.Parameters.AddWithValue("@MouthYOffeset", obj.Avatar.Mouth.YOffset.ToString());
                cmd.Parameters.AddWithValue("@Skin", NormalizeKey(obj.Avatar.Nose.SelectionKey, "123"));
                cmd.Parameters.AddWithValue("@SkinTintIndex", (int)obj.Avatar.Skin.TintIndex);
                cmd.Parameters.AddWithValue("@SkinXOffeset", obj.Avatar.Skin.XOffset.ToString());
                cmd.Parameters.AddWithValue("@SkinYOffeset", obj.Avatar.Skin.YOffset.ToString());
                cmd.Parameters.AddWithValue("@Hat", "0");
                cmd.Parameters.AddWithValue("@HatTintIndex", 0);
                cmd.Parameters.AddWithValue("@HatXOffeset", "0.0");
                cmd.Parameters.AddWithValue("@HatYOffeset", "0.0");
                await cmd.ExecuteNonQueryAsync();
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

            result = new SetAvatarResponse
            {
                Notification  = new SetAvatarNotification {
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
                    }, Created = (long)session["timestamp"], NotificationId = getSrvSett.Sequence, SequenceNumber = getSrvSett.Sequence
                }
            };

            var json = JsonSerializer.Serialize(result, options);
            byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

            ctx.Response.ContentLength = encrypted.Length;

            ctx.Response.ContentType = "application/mix";
            await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
        }
    }
}
