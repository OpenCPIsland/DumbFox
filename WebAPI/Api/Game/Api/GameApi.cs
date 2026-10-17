using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Api.Game.routes.game;

namespace WebAPI.Api.Game.Api
{
    public static class GameApi
    {
        public static void Register(this WebApplication app)
        {
            app.MapGet("/game/v1/rooms", GetRoomsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapPost("/game/v1/worlds/{world}/language/{langID}/rooms/{Room}/players", PostRoomPlayersOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
        }
    }
}
