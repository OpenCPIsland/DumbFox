using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Api.Game.routes.player;

namespace WebAPI.Api.Game.Api
{
    public static class PlayerApi
    {
        public static void Register(this WebApplication app)
        {
            app.MapPost("/player/v1/outfit", SetOutfitOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapPost("/player/v1/profile", SetProfileOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapGet("/player/v1/", GetLocalPlayerDataOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapGet("/player/v1/id/{swid}", GetOtherPlayerDataBySwidOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapPost("/player/v1/id", GetOtherPlayerDataBySwidsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapGet("/player/v1/name/{displayName}", GetOtherPlayerDataByDisplayNameOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapPost("/player/v1/names", GetOtherPlayerDataByDisplayNamesOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });

            app.MapPost("/player/v1/id/online", GetOnlinePlayersBySwidsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });

        }
    }
}
