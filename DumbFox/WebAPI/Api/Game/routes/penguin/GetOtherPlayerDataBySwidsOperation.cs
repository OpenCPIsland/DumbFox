using Common.Utils;
using Common.Utils.Game;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Api.Game.routes.penguin;

public class GetOtherPlayerDataBySwidsOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI] Got GetOtherPlayerDataBySwids request");
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

        ctx.Request.EnableBuffering();
        using StreamReader read = new StreamReader(
            ctx.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        string body = await read.ReadToEndAsync();
        ctx.Request.Body.Position = 0;

        List<string>? swids = JsonSerializer.Deserialize<List<string>>(body);


        if (swids == null || swids.Count == 0)
        {
            throw new Exception("Invalid SWID list");
        }

        var resotherplayer = new List<OtherPlayerData>();

        foreach (var swid in swids)
        {
            var account = await DatabaseManager.GetAccountSwid(swid);
            var loadedProfile = await DatabaseManager.GetProfilesSwid(swid);

            if (account == null || loadedProfile == null)
                continue;

            DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(loadedProfile.RegisterTimestamp);
            int days = (int)(DateTimeOffset.UtcNow - date).TotalDays;

            var item = new OtherPlayerData
            {
                id = new ProfileId
                {
                    id = loadedProfile.Swid,
                    type = 1
                },
                name = (string)account["username"],
                member = loadedProfile.Member,
                profile = new Profile
                {
                    colour = loadedProfile.Colour,
                    daysOld = days
                },
                mascotXP = new
                {
                    AuntArctic = 1192307,
                    Rockhopper = 166666
                },
                outfit = await DatabaseManager.getCurrentOutfit(loadedProfile.Swid)
            };

            resotherplayer.Add(item);
        }


        result = resotherplayer;

        var json = JsonSerializer.Serialize(result, options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

