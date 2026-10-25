using Common.Utils;
using Common.Utils.Game;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using WebAPI.Api.Game.Api;
using WebAPI.Api.Game.routes.dailys;
using WebAPI.Api.Game.routes.durable;
using WebAPI.Api.Game.routes.iap;
using WebAPI.Api.Game.routes.tube;

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
        ctx.Items["authenticated-session"] = session;
        sw.Stop();
        ctx.Response.Headers["X-Execution-Time"] = $"{sw.Elapsed.TotalMilliseconds:F0}";
        var result = await next(context);
        return result;
    }

    public static string formatted = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public static void Register(WebApplication app)
    {
        BreadcrumbApi.Register(app);//0%
        ChatApi.Register(app);//100% done TODO Chatfilter needs done and Logging due EU politic thing
        ConsumableApi.Register(app);//0%
        DurableApi.Register(app);//100%
        EncryptionApi.Register(app);//100% done
        GameApi.Register(app);//30% TODO other lists needs be still done
        IglooApi.Register(app);//0%
        InventoryApi.Register(app);//100% done
        MinigameApi.Register(app);//0%
        PlayerApi.Register(app);//75% done TODO Make Friend is online or offline properly and Find Friend
        QuestApi.Register(app);//0%
        RewardApi.Register(app);//15%
        SavedOutfitApi.Register(app);//0%
        TaskApi.Register(app);//0%
        IAPApi.Register(app);//50% not sure if needed be done further
        TutorialApi.Register(app);//0%
        ModerationApi.Register(app);//0%
        TubeApi.Register(app);//100% done
        DisneyStoreApi.Register(app);//0%
        NewsfeedApi.Register(app);//0%
        CatalogApi.Register(app);//75% TODO SubmitSubmission and your current stats
        ScheduledEventApi.Register(app);//0%
        DiagnosticsApi.Register(app);//0%
        CaptchaApi.Register(app);//0%
    }
}

