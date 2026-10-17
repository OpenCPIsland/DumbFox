using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Api.Game.routes.chat;

namespace WebAPI.Api.Game.Api
{
    public static class ChatApi
    {
        public static void Register(this WebApplication app)
        {
            app.MapPost("/chat/v1/message/verify", VerifyChatMessageOperation.Handle);
        }
    }
}
