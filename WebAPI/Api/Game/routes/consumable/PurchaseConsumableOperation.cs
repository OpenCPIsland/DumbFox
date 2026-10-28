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
public class PurchaseConsumableOperation
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
        if (!ShareableConsumables.TryGet(type, out ShareableConsumableDefinition definition)
            || !int.TryParse(ctx.Request.RouteValues["count"]?.ToString(), out int count)
            || count is < 1 or > 100)
        {
            Console.WriteLine($"Purchase Failed");
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { Code = 3 }, 400);
            return;
        }

        try
        {
            (ConsumableInventory inventory, long coins) =
                await DatabaseManager.PurchaseConsumable(swid, type, count, definition.Cost, definition.MemberOnly);
            result = new ConsumablePurchaseResponse
            {
                assets = new PlayerAssets { currency = new { coins }, coins = checked((int)coins) },
                inventory = new SignedResponse<ConsumableInventory> { swid = swid, data = inventory }
            };
        }
        catch (UnauthorizedAccessException)
        {
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { Code = 3 }, 403);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Not enough coins.")
        {
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { Code = 707 }, 400);
        }

        var json = JsonSerializer.Serialize(result, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

