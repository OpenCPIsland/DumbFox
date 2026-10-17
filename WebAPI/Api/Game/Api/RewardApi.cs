using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Api.Game.routes.reward;

namespace WebAPI.Api.Game.Api
{
    public static class RewardApi
    {
        public static void Register(this WebApplication app)
        {

            app.MapGet("/reward/v1/preregistration", ClaimPreregistrationRewardOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
            app.MapPost("/reward/v1/claimServerAdded", ClaimServerAddedRewardsOperation.Handle).AddEndpointFilter(async (context, next) => { return await GameRoutes.GameAPIMiddleWare(context, next); });
        }
    }
}
