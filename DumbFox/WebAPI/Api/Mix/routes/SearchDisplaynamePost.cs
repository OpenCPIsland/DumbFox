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

public class SearchDisplaynamePost
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][Mix] Got SearchDisplayname request");
        await using var conn = await DatabaseManager.OpenConnectionAsync();
        DisplayNameSearchResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());

        DisplayNameSearchRequest obj = JsonSerializer.Deserialize<DisplayNameSearchRequest>((string)ctx.Items["DecryptedModel"]);

        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        await using var cmd = new MySqlCommand(@"SELECT * FROM accounts WHERE username = @displayName;",conn);
        cmd.Parameters.AddWithValue("@displayName", obj.DisplayName);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            ctx.Response.StatusCode = 200;
            var loadedProfile = await DatabaseManager.GetProfilesSwid((string)reader["swid"]);
            if (loadedProfile == null)
            {
                throw new Exception("From Given Swid from Profile not found");
            }

            result = new DisplayNameSearchResponse
            {
                DisplayName = (string)reader["username"],
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

            };

        } else
        {
            Console.WriteLine("Displayname Not found in accounts : "+obj.DisplayName);
            ctx.Response.StatusCode = 404;
            result = new DisplayNameSearchResponse
            {
                DisplayName = obj.DisplayName,
                FirstName = "Test",
            };
        }

        var json = JsonSerializer.Serialize(result, options);
        byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

        ctx.Response.ContentLength = encrypted.Length;

        ctx.Response.ContentType = "application/mix";
        await reader.DisposeAsync();
        await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
    }
}

