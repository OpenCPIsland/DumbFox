using Common.Utils;
using Common.Utils.Game;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Api.Game.routes.consumable;

public class UseConsumableOperation
{
    public static async Task Handler(HttpContext ctx)
    {

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
        string swid = (string)session["swid"];

        string type = ctx.Request.RouteValues["type"]?.ToString() ?? "";
        if (!ShareableConsumables.TryGet(type, out ShareableConsumableDefinition definition))
        {
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { Code = 3 }, 403);
            return;
        }

        try
        {
            (int partialCount, string authorizationToken, long goodUntil) = await DatabaseManager.UseConsumable(swid, type);
            result = new SignedResponse<UsedConsumable>
            {
                data = new UsedConsumable { type = type, partialCount = partialCount }
            };
        }
        catch (InvalidOperationException)
        {
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { Code = 3 }, 403);
        }


        var json = JsonSerializer.Serialize(result, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

