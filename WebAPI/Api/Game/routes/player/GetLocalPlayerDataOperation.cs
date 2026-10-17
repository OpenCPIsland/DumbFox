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

namespace WebAPI.Api.Game.routes.player;

public static class GetLocalPlayerDataOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][GameRoutes][PlayerApi] Got GetLocalPlayerDataOperation");
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

        result = new LocalPlayerData
        {
            id = new ProfileId
            {
                id = (string)session["swid"],
                type = 1
            },
            name = (string)session["username"],
            member = loadedProfile.Member,
            profile = new Profile { colour = loadedProfile.Colour, daysOld = days },
            mascotXP = new 
            {
                AuntArctic = 1192307,
                Rockhopper = 166666
            },
            outfit = await DatabaseManager.getCurrentOutfit((string)session["swid"]),
            zoneId = new ZoneId { name = "Boardwalk" }
        };

        var json = JsonSerializer.Serialize(result, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

