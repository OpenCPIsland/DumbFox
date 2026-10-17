using Microsoft.AspNetCore.Builder;
using WebAPI.Api.Jgc.routes;
using Common.Utils;
using Common.Utils.Jgc;
namespace WebAPI.Api.Jgc;

public static class JgcRoutes
{
	private const string BasePath = "/jgc/v5/";

    public static string FormatTimestamp(DateTimeOffset? date = null)
    {
        var d = date ?? DateTimeOffset.Now;

        string basePart = d.ToString("yyyy-MM-dd'T'HH:mm:ss.fff");
        string offset = d.ToString("zzz").Replace(":", "");

        return basePart + offset;
    }

    public static void Register(WebApplication app)
	{


        app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/jgc/v5"), branch =>
        {
            branch.Use(async (ctx, fwd) =>
            {
                ctx.Response.Headers["correlation-id"] = Guid.NewGuid().ToString();
                ctx.Response.Headers["Server"] = "Apache-Coyote/1.1";
                ctx.Response.Headers["Vary"] = "Origin";
                await fwd();
            });
        });

        app.MapGet("/DatabaseTest.cfg", DatabaseTest.Handle);
        app.MapPost(BasePath+"client/{clientId}/api-key", GetApiKeyImmediately.Handle);
		app.MapGet(BasePath + "client/{clientId}/configuration/site", GetSiteConfiguration.Handle);
		app.MapPost(BasePath + "client/{clientId}/guest/login", LogIn.Handle);
        app.MapPost(BasePath + "client/{clientId}/guest/refresh-auth/{refreshToken}", CreateRefreshQueueItem.Handle);
        app.MapGet(BasePath + "client/{clientId}/guest/{swid}", GetProfile.Handle);
        app.MapPost(BasePath + "client/{clientId}/guest/register", Register0.Handle);
        app.MapPost(BasePath + "client/{clientId}/validate", Validate.Handle);
    }
}
