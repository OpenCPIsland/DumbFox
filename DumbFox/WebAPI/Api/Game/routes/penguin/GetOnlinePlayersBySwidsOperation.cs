using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebAPI.Api.Game.routes.penguin;

public class GetOnlinePlayersBySwidsOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI] Got GetOnlinePlayersBySwids request");
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
        result = swids;

        var json = JsonSerializer.Serialize(result, options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

