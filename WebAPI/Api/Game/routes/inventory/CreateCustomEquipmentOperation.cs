using Common.Utils;
using Common.Utils.Game;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebAPI.Api.Game.routes.inventory;

public class CreateCustomEquipmentOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][GameRoutes][InventoryApi] Got CreateCustomEquipmentOperation");
        object? result = new { Code = 999 };
        ctx.Response.ContentType = "application/json;charset=utf-8";
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string accesstoken = "";
        if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            string[] words = authHeader.ToString().Split(",");
            var decodedBytes = Convert.FromBase64String(words[0].Replace("Basic ", ""));
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            accesstoken = decodedString.Replace(":", "");
        }

        var session = await DatabaseManager.GetSessionAccessToken(accesstoken);

        await using var conn = await DatabaseManager.OpenConnectionAsync();
        ctx.Request.EnableBuffering();

        using StreamReader reader = new StreamReader(
            ctx.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        string body = await reader.ReadToEndAsync();

        ctx.Request.Body.Position = 0;


        var loadedProfile = await DatabaseManager.GetProfilesSwid((string)session["swid"]);

        if (loadedProfile == null)
        {
            throw new Exception("From Given Swid from Profile not found");
        }

        CustomEquipment? CustomEquipmentRequest = JsonSerializer.Deserialize<CustomEquipment>(body);

        if (CustomEquipmentRequest != null)
        {
            result = new CreateEquipmentResponse
            {
                equipmentId = await DatabaseManager.InsertCustomEquipment(
                (string)session["swid"],
                CustomEquipmentRequest,
                (string)session["username"]
            ),
                assets = new PlayerAssets
                {
                    currency = new
                    {
                        coins = loadedProfile.Coins-40
                    },
                    mascotXP = new
                    {
                        AuntArctic = 1192307,
                        Rockhopper = 166666
                    },
                    collectibleCurrency = new { },
                    colourPacks = [],
                    decals = [169, 170, 171],
                    fabrics = [258, 259, 260],
                    emotePacks = [],
                    sizzleClips = [],
                    equipmentTemplates = [],
                    lots = [],
                    decorations = [238, 239, 240, 241, 242, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 265, 266, 268, 269, 270, 272, 273, 274, 275, 276],
                    structures = [],
                    musicTracks = [],
                    lighting = [],
                    durables = [],
                    tubes = [],
                    coins = loadedProfile.Coins-40,
                    partySupplies = []
                }
            };
        } else
        {
            result = new CreateEquipmentResponse
            {
                equipmentId = 0,
                assets = new PlayerAssets
                {
                    currency = new
                    {
                        coins = loadedProfile.Coins
                    },
                    mascotXP = new
                    {
                        AuntArctic = 1192307,
                        Rockhopper = 166666
                    },
                    collectibleCurrency = new { },
                    colourPacks = [],
                    decals = [169, 170, 171],
                    fabrics = [258, 259, 260],
                    emotePacks = [],
                    sizzleClips = [],
                    equipmentTemplates = [],
                    lots = [],
                    decorations = [238, 239, 240, 241, 242, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 265, 266, 268, 269, 270, 272, 273, 274, 275, 276],
                    structures = [],
                    musicTracks = [],
                    lighting = [],
                    durables = [],
                    tubes = [],
                    coins = loadedProfile.Coins,
                    partySupplies = []
                }
            };
        }


        //await DatabaseManager.getSwidInventory((string)session["swid"]);

        var json = JsonSerializer.Serialize(result, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

