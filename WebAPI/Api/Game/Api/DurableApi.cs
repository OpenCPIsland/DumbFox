using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Api.Game.routes.durable;

namespace WebAPI.Api.Game.Api
{
    public static class DurableApi
    {
        public static void Register(this WebApplication app)
        {
            app.MapGet("/player/v1/durable/equip/{type}", EquipDurableOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
        }
    }
}
