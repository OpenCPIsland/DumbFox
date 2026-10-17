using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Api.Game.routes.iap;

namespace WebAPI.Api.Game.Api
{
    public static class IAPApi
    {
        public static void Register(this WebApplication app)
        {
            app.MapGet("/iap/v1/products", GetProductsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });

            app.MapPost("/iap-pc/v1/session", PCSessionStartOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
        }
    }
}
