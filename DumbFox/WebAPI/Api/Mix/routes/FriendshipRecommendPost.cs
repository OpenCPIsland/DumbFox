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

public class FriendshipRecommendPost
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][Mix] Got FriendShipRecommend request");

        GetFriendshipRecommendationResponse? result;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        BaseUserRequest obj = JsonSerializer.Deserialize<BaseUserRequest>((string)ctx.Items["DecryptedModel"]);

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


        result = new GetFriendshipRecommendationResponse
        {
            FriendRecommendations = [new FriendRecommendation { DisplayName = "Mixbot", FirstName = "Mixbot", Avatar = new Avatar {
                        Accessory = new AvatarProperty { SelectionKey = "3", TintIndex = 13, XOffset = 0.0d, YOffset = 0.0d },
                        Costume = new AvatarProperty { SelectionKey = "15", TintIndex = 0, XOffset = 0.0d, YOffset = 0.0d },
                        Brow = new AvatarProperty { SelectionKey = "185", TintIndex = 0, XOffset = 0.0d, YOffset = 0.0124999998137355d },
                        Eyes = new AvatarProperty { SelectionKey = "48", TintIndex = 8, XOffset = 0.0d, YOffset = 0.0124999998137355d },
                        Hair = new AvatarProperty { SelectionKey = "186", TintIndex = 3, XOffset = 0.0d, YOffset = 0.0d },
                        Nose = new AvatarProperty { SelectionKey = "101", TintIndex = 0, XOffset = 0.0d, YOffset = 0.00203833859413862d },
                        Mouth = new AvatarProperty { SelectionKey = "84", TintIndex = 0, XOffset = 0.0d, YOffset = 0.0110180018469691d },
                        Skin = new AvatarProperty { SelectionKey = "123", TintIndex = 3, XOffset = 0.0d, YOffset = 0.0d },
                        Hat = new AvatarProperty { SelectionKey = "0", TintIndex = 0, XOffset = 0.0d, YOffset = 0.0d }
            }}]
        };

        var json = JsonSerializer.Serialize(result, options);
        byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String((string)session["aesSecret"]));

        ctx.Response.ContentLength = encrypted.Length;

        ctx.Response.ContentType = "application/mix";
        await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
    }
}

