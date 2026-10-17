using Common.Utils;
using Common.Utils.Game;
using Microsoft.AspNetCore.Http;
using MySqlConnector;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebAPI.Api.Game.routes.inventory;

public class DeleteCustomEquipmentOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][GameRoutes][InventoryApi] Got DeleteCustomEquipmentOperation");
        object? result = new { Code = 999 };
        ctx.Response.ContentType = "application/json;charset=utf-8";
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string accesstoken = "";
        if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            string[] words = authHeader.ToString().Split(",");
            var decodedBytes = Convert.FromBase64String(words[0].Replace("Basic ", ""));
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            accesstoken = decodedString.Replace(":", "");
        }

        var session = await DatabaseManager.GetSessionAccessToken(accesstoken);

        await using var conn = await DatabaseManager.OpenConnectionAsync();

        var equipmentIdStr = ctx.Request.RouteValues["equipmentId"]?.ToString();

        await using var cmd = new MySqlCommand(@"DELETE FROM inventorys WHERE swid = @swid AND id = @id;", conn);

        cmd.Parameters.AddWithValue("@swid", (string)session["swid"]);
        cmd.Parameters.AddWithValue("@id", Convert.ToInt64(equipmentIdStr));

        var rowsAffected = await cmd.ExecuteNonQueryAsync();

        result = new CustomEquipmentId {
            equipmentId = Convert.ToInt64(equipmentIdStr)
        };

        var json = JsonSerializer.Serialize(result, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);
    }

}

