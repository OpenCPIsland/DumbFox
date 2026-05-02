using Common.Utils;
using Common.Utils.Jgc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace WebAPI.Api.Jgc.routes;

public static class GetSiteConfiguration
{

	private static string? _config;
	private static string Config => _config ??= GetSiteConfig();

	private static string GetSiteConfig()
	{
		Console.WriteLine("[WebAPI][JGC] Loaded site config, caching");
		string configPath = Path.Combine(AppContext.BaseDirectory, "Config/ApiConfiguration.json");
		string config = File.ReadAllText(configPath);
		_config = config;
		return config;
	}
	
	public static async Task Handle(HttpContext ctx)
	{
		Console.WriteLine("[WebAPI][JGC] Got site config request");

		object? configJson = JsonSerializer.Deserialize<object>(Config);
		
		ctx.Response.Headers["Cache-Control"] = "public, max-age=43200";
		ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";

        ctx.Request.EnableBuffering();

        using StreamReader reader = new StreamReader(
            ctx.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        string body = await reader.ReadToEndAsync();

        ctx.Request.Body.Position = 0;

        Console.WriteLine($"[WebAPI][JGC] Body: {body}");

        await ctx.Response.WriteAsJsonAsync(new
		{
			data = configJson,
			error = (object?)null
		});
	}
}
