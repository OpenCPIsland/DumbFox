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

namespace WebAPI.Api.Game.routes.world;

public static class GetRoomsOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI] Got Rooms request");
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

        List<RoomIdentifier> RoomsFound = new List<RoomIdentifier>();
        RoomsFound.Add(new RoomIdentifier
        {
            zoneId = new ZoneId { name = ctx.Request.Query["rooms"] },
            contentIdentifier = "1.13.0" + ";online;" + GameRoutes.formatted + ";NONE", //ctx.Request.Headers["X-CP-Client-Version"] + ";offline;" + GameRoutes.formatted + ";NONE",
            room = ctx.Request.Query["rooms"]
        });


        var json = JsonSerializer.Serialize(RoomsFound, options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);

    }
}

