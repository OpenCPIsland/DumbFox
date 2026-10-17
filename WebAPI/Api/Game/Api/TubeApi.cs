using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Api.Game.routes.tube;

namespace WebAPI.Api.Game.Api
{
    public static class TubeApi
    {
        public static void Register(this WebApplication app)
        {
            app.MapPost("/tube/v1/equip/{type}", EquipTubeOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
        }
    }
}
