using Common.Utils;
using Common.Utils.Mix;
using Common.Utils.Game;
using Common.Utils.Jgc;
using Microsoft.AspNetCore.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAPI.Api.Jgc;

namespace WebAPI.Api;

public static class ResponseHelper
{
    public static async Task WriteJsonAsync(HttpContext ctx, object data, int statusCode = 500)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json;charset=utf-8";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(data, options);

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }

    public static async Task WriteJgcError(HttpContext ctx, string category, string code, int statusCode = 500, string? input = null, TemporaryToken? data2 = null, string? developermessage=null)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.Headers["X-Cnection"] = "close";
        ctx.Response.ContentType = "application/json;charset=utf-8";
        GuestControllerWebCallResponse data = new GuestControllerWebCallResponse
        {
            error = new GuestApiErrorCollection
            {
                keyCategory = category,
                correlationId = ctx.Response.Headers["correlation-id"],
                errors = [new GuestApiError {
                        code = code,
                        category = category,
                        inputName = input,
                        errorId = Guid.NewGuid().ToString(),
                        timestamp = JgcRoutes.FormatTimestamp(),
                        data = data2,
                        developerMessage = developermessage
                    }]
            }
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(data, options);
        await ctx.Response.WriteAsync(json);
    }
}

