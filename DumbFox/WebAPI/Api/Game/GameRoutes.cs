using Common.Utils;
using Common.Utils.Game;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using WebAPI.Api.Game.routes.chat;
using WebAPI.Api.Game.routes.dailys;
using WebAPI.Api.Game.routes.iapp;
using WebAPI.Api.Game.routes.penguin;
using WebAPI.Api.Game.routes.reward;
using WebAPI.Api.Game.routes.world;

namespace WebAPI.Api.Game;

public static class GameRoutes
{

    public static string Generate()
    {
        string salt = "K10B93N601N1514nD438597b23v23b54235uv23523459u234c23";
        string milliseconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        string generatedWithTimer = Md5Hex(milliseconds);
        string generated = Md5Hex(salt + milliseconds + generatedWithTimer);
        return generated;
    }

    public static string Md5Hex(string input)
    {
        using var md5 = MD5.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = md5.ComputeHash(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static async Task<object?> GameAPIMiddleWare(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var ctx = context.HttpContext;
        var sw = Stopwatch.StartNew();

        ctx.Response.Headers["X-Cloud-Trace-Context"] = Generate();
        ctx.Response.Headers["Server"] = "Google Frontend";

        string accesstoken = "";
        if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            string[] words = authHeader.ToString().Split(",");
            var decodedBytes = Convert.FromBase64String(words[0].Replace("Basic ", ""));
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            accesstoken = decodedString.Replace(":", "");
        }
        else
        {
            sw.Stop();
            ctx.Response.Headers["X-Execution-Time"] = $"{sw.Elapsed.TotalMilliseconds:F0}";
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { }, 401);
            return null;
        }
        var session = await DatabaseManager.GetSessionAccessToken(accesstoken);
        if (session is null)
        {
            sw.Stop();
            ctx.Response.Headers["X-Execution-Time"] = $"{sw.Elapsed.TotalMilliseconds:F0}";
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { }, 401);
            return null;
        }
        sw.Stop();
        ctx.Response.Headers["X-Execution-Time"] = $"{sw.Elapsed.TotalMilliseconds:F0}";
        var result = await next(context);
        return result;
    }

    public static string formatted = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public static void Register(WebApplication app)
    {
        app.MapGet("/player/v1/", GetLocalPlayerDataOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapGet("/player/v1/name/{displayName}", GetOtherPlayerDataByDisplayNameOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapGet("/player/v1/id/{swid}", GetOtherPlayerDataBySwidOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/player/v1/outfit", SetOutfitOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/player/v1/id/online", GetOnlinePlayersBySwidsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/player/v1/id", GetOtherPlayerDataBySwidsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/player/v1/names", GetOtherPlayerDataByDisplayNamesOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapGet("/inventory/v1/equipment", GetInventoryOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/inventory/v1/equipment", CreateCustomEquipmentOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/inventory/v1/equipment/{equipmentId}", DeleteCustomEquipmentOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });

        app.MapGet("/catalog/v1/clothing/themes/stats", GetCurrentThemeOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/catalog/v1/clothing/items/recent", GetCatalogRecentOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/catalog/v1/clothing/items/popular", GetCatalogPopularOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/catalog/v1/clothing/items/friends", GetCatalogFriendsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/catalog/v1/clothing/items/category", GetCatalogCategoryOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });

        app.MapGet("/reward/v1/preregistration", ClaimPreregistrationRewardOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/reward/v1/claimServerAdded", ClaimServerAddedRewardsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });

        app.MapPost("/iap-pc/v1/session", PCSessionStartOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });

        app.MapGet("/game/v1/rooms", GetRoomsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });
        app.MapPost("/game/v1/worlds/{world}/language/{langID}/rooms/{Room}/players", PostRoomPlayersOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameAPIMiddleWare(context, next); });

        app.MapPost("/encryption-trusted/v1/encryptionKey", GetEncryptionKeyOperation.Handle);
        app.MapPost("/chat/v1/message/verify", VerifyChatMessageOperation.Handle);
    }
}

