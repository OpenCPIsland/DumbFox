using Common.Utils;
using Common.Utils.Game;
using Common.Utils.Jgc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Api.Game.routes.dailys;

public class GetCatalogCategoryOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][GameRoutes][CatalogApi] Got GetCatalogCategoryOperation");
        object? result = new { Code = 999 };
        ctx.Response.ContentType = "application/json;charset=utf-8";
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        ctx.Request.EnableBuffering();

        using StreamReader reader = new StreamReader(
            ctx.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        string body = await reader.ReadToEndAsync();

        ctx.Request.Body.Position = 0;

        CatalogCategoryRequest? CatalogCategoryRequest = JsonSerializer.Deserialize<CatalogCategoryRequest>(body);

        result = new CatalogSectionResponse
        {
            items = await DatabaseManager.getCurrentCataThemeItemsWithCategoryfromId(CatalogCategoryRequest.scheduledThemeChallengeId, CatalogCategoryRequest.equipmentCategory),
            cursor = CatalogCategoryRequest.cursor,
        };

        var json = JsonSerializer.Serialize(result, options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }
}

