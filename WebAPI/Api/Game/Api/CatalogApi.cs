using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Api.Game.routes.dailys;

namespace WebAPI.Api.Game.Api
{
    public static class CatalogApi
    {
        public static void Register(this WebApplication app)
        {
            app.MapPost("/catalog/v1/clothing/items/category", GetCatalogCategoryOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapPost("/catalog/v1/clothing/items/friends", GetCatalogFriendsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapPost("/catalog/v1/clothing/items/popular", GetCatalogPopularOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapPost("/catalog/v1/clothing/items/recent", GetCatalogRecentOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapGet("/catalog/v1/clothing/themes/stats", GetCurrentThemeOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
        }
    }
}
