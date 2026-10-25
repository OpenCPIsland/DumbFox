using Common.Utils;
using Common.Utils.Game;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAPI.Api.Game.routes.consumable;

namespace WebAPI.Api.Game.Api
{
    public static class ConsumableApi
    {
        public static void Register(this WebApplication app)
        {
            app.MapGet("/consumable/v1", GetConsumableInventoryOperation.Handler).AddEndpointFilter(async (context, next) => await GameRoutes.GameAPIMiddleWare(context, next));
            app.MapPost("/consumable/v1/{type}/{count:int}", PurchaseConsumableOperation.Handler).AddEndpointFilter(async (context, next) => await GameRoutes.GameAPIMiddleWare(context, next));
            app.MapMethods("/consumable/v1/{type}", ["POST", "DELETE"], UseConsumableOperation.Handler).AddEndpointFilter(async (context, next) => await GameRoutes.GameAPIMiddleWare(context, next));
            app.MapPost("/consumable/v1/partial", StorePartialConsumableOperation.Handler).AddEndpointFilter(async (context, next) => await GameRoutes.GameAPIMiddleWare(context, next));
        }
    }
}
