using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Api.Game.routes.inventory;
namespace WebAPI.Api.Game.Api
{
    public static class InventoryApi
    {
        public static void Register(this WebApplication app)
        {
            app.MapPost("/inventory/v1/equipment", CreateCustomEquipmentOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapGet("/inventory/v1/equipment", GetInventoryOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapPost("/inventory/v1/equipment/{equipmentId}", DeleteCustomEquipmentOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
        }
    }
}
