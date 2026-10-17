using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Utils;
using Common.Utils.Mix;

namespace WebAPI.Api.Mix.routes
{
    public class RegistrationTextPost
    {
        public static string GenerateResponseJson(JsonElement obj, HttpContext ctx)
        {
            RegistrationText DISNEY_EU_SHAREINFO_COOKIE_ACCEPT = new RegistrationText()
            {
                TextCode = "DISNEY-EU-SHAREINFO-COOKIE-ACCEPT",
                Text = "I agree that Disney Interactive may:<ul><li>Share personal information which it collects within The Walt Disney Family of Companies.</li><li>Use cookies to help provide relevant content to improve my online experience, to remember my choices (such as language choices) and to personalize content and services including targeting advertising and promotions to match my interests.</li></ul>"
            };
            RegistrationText GTOU = new RegistrationText()
            {
                TextCode = "GTOU",
                Text = "I have read and agree to the <a target=\"_blank\" href=\"https://disneytermsofuse.com/english/\">Disney Terms of Use</a>."
            };
            RegistrationText ppV2 = new RegistrationText()
            {
                TextCode = "ppV2",
                Text = "By creating an account, I confirm that I have read <a target=\"_blank\" href=\"https://privacy.thewaltdisneycompany.com/en/current-privacy-policy/\">Disney’s Privacy Policy</a>."
            };
            RegistrationText gtou_ppv2_proxy_create = new RegistrationText()
            {
                TextCode = "gtou_ppv2_proxy_create",
                Text = "By creating an account, I agree to the <a target=\"_blank\" href=\"https://disneytermsofuse.com/english/\">Terms of Use</a> and acknowledge the <a target=\"_blank\" href=\"https://privacy.thewaltdisneycompany.com/en/current-privacy-policy/\">Privacy Policy</a>."
            };
            RegistrationText cookie_ppv2_proxy_create = new RegistrationText()
            {
                TextCode = "cookie_ppv2_proxy_create",
                Text = "By creating an account, I confirm that I have read <a target=\"_blank\" href=\"https://privacy.thewaltdisneycompany.com/en/current-privacy-policy/\">Disney’s Privacy Policy</a> and <a target=\"_blank\" href=\"https://privacy.thewaltdisneycompany.com/en/current-privacy-policy/cookies-policy\">Cookies Policy</a>."
            };
            RegistrationText GTOU_create = new RegistrationText()
            {
                TextCode = "GTOU_create",
                Text = "I have read and agree to the <a target=\"_blank\" href=\"https://disneytermsofuse.com/english/\">Disney Terms of Use</a>."
            };
            //disneyMix
            RegistrationText WDIGFamilySites = new RegistrationText()
            {
                TextCode = "WDIGFamilySites",
                Text = "Yes! Send me Disney updates and offers."
            };

            JsonElement textCodesElement = obj.GetProperty("TextCodes");

            var requestedCodes = textCodesElement
                .EnumerateArray()
                .Select(x => x.GetString())
                .ToHashSet();

            var blocks = new List<RegistrationText>();

            if (requestedCodes.Contains("DISNEY-EU-SHAREINFO-COOKIE-ACCEPT"))
                blocks.Add(DISNEY_EU_SHAREINFO_COOKIE_ACCEPT);

            if (requestedCodes.Contains("GTOU"))
                blocks.Add(GTOU);

            if (requestedCodes.Contains("ppV2"))
                blocks.Add(ppV2);

            if (requestedCodes.Contains("GTOU_create"))
                blocks.Add(GTOU_create);

            if (requestedCodes.Contains("gtou_ppv2_proxy_create"))
                blocks.Add(gtou_ppv2_proxy_create);

            if (requestedCodes.Contains("cookie_ppv2_proxy_create"))
                blocks.Add(cookie_ppv2_proxy_create);

            if (requestedCodes.Contains("WDIGFamilySites"))
                blocks.Add(WDIGFamilySites);

            GetRegistrationTextResponse response = new GetRegistrationTextResponse
            {
                Status = "OK",
                RegistrationText = blocks
            };

            var options = new JsonSerializerOptions
            {
               // PropertyNamingPolicy = JsonNamingPolicy.Never,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(response, options);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            ctx.Response.ContentLength = bytes.Length;

            return json;
        }

        public static async Task Handle(HttpContext ctx)
        {
            Console.WriteLine("[WebAPI][MixRoutes] Got RegistrationText request");


            ctx.Request.EnableBuffering();

            using StreamReader reader = new StreamReader(
                ctx.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            string body = await reader.ReadToEndAsync();

            ctx.Request.Body.Position = 0;

            ctx.Response.Headers["X-Cloud-Trace-Context"] = MixRoutes.Generate();
            ctx.Response.Headers["Server"] = "Google Frontend";
            ctx.Response.Headers["Alt-Svc"] = "quic=\":443\"; ma=2592000; v=\"44,43,39,35\"";

            Console.WriteLine($"[WebAPI][MixRoutes] body: {body}");

            JsonElement obj = JsonSerializer.Deserialize<JsonElement>(body);

            string response = GenerateResponseJson(obj, ctx);

            ctx.Response.ContentType = "application/json;charset=utf-8";

            await ctx.Response.WriteAsync(response);
        }
    }
}
