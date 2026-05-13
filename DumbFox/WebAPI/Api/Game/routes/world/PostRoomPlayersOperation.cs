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

namespace WebAPI.Api.Game.routes.world;

public static class PostRoomPlayersOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI] Got Worlds request");
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

        var loadedProfile = await DatabaseManager.GetProfilesSwid((string)session["swid"]);

        if (loadedProfile == null)
        {
            throw new Exception("From Given Swid from Profile not found");
        }

        DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(loadedProfile.RegisterTimestamp);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        int days = (int)(now - date).TotalDays;

        var world = ctx.Request.RouteValues["world"]?.ToString();
        var langID = ctx.Request.RouteValues["langID"]?.ToString();
        var room = ctx.Request.RouteValues["Room"]?.ToString();

        result = new SignedResponse<JoinRoomData> 
        {
            data = new JoinRoomData
            {
                swid = loadedProfile.Swid,
                room = world + ":" + langID + ":" + room + "::" + "1.13.0" + ";offline;" + GameRoutes.formatted + ";NONE",//world + ":" + langID + ":" + room + "::" + ctx.Request.Headers["X-CP-Client-Version"] + ";offline;" + GameRoutes.formatted + ";NONE",
                sessionId = (long)session["sessionId"],
                playerRoomData = new PlayerRoomData
                {
                    member = loadedProfile.Member,
                    outfit = new PlayerOutfitDetails
                    {
                        sessionId = (long)session["sessionId"],
                        parts = await DatabaseManager.getCurrentOutfit((string)session["swid"])
                    },
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
                        coins = 250,
                        partySupplies = []
                    },
                    profile = new Profile
                    {
                        colour = loadedProfile.Colour,
                        daysOld = days
                    }
                },
                userName = (string)session["username"],
                membershipRights = new MembershipRights
                {
                    swid = loadedProfile.Swid,
                    member = loadedProfile.Member,
                    allAccessAllowed = true
                },
                selectedTubeId = loadedProfile.SelectedTubeId
            }
        };

        var json = JsonSerializer.Serialize(result, options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

