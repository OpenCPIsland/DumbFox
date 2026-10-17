using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Api.Game.routes.encryption;

namespace WebAPI.Api.Game.Api
{
    public static class EncryptionApi
    {
        public static void Register(this WebApplication app)
        {
            app.MapPost("/encryption-trusted/v1/encryptionKey", GetEncryptionKeyOperation.Handle);
        }
    }
}
