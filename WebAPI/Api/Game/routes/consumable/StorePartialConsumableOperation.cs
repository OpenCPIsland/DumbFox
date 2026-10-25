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

public class StorePartialConsumableOperation
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

        SignedResponse<UsedConsumable>? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<SignedResponse<UsedConsumable>>(ctx.Request.Body);
        }
        catch (JsonException)
        {
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { Code = 3 }, 400);
            return;
        }

        if (request?.data is not UsedConsumable data
            || !ShareableConsumables.TryGet(data.type, out ShareableConsumableDefinition definition)
            || data.partialCount < 0
            || data.partialCount > definition.ActionCount)
        {
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { Code = 3 }, 400);
            return;
        }

        ConsumableInventory inventory = await DatabaseManager.GetConsumableInventory(swid);
        if (!inventory.inventoryMap.ContainsKey(data.type))
        {
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { Code = 3 }, 400);
            return;
        }

        await DatabaseManager.StoreConsumablePartial(swid, data.type, data.partialCount);
        inventory = await DatabaseManager.GetConsumableInventory(swid);
        result = new SignedResponse<ConsumableInventory>
        {
            data = inventory
        };


        var json = JsonSerializer.Serialize(result, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

