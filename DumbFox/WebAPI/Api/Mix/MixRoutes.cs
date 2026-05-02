using Common.Utils;
using Common.Utils.Mix;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebAPI.Api.Mix.routes;

namespace WebAPI.Api.Mix
{
    public static class MixRoutes
    {
        private const string BasePath = "/";
        private const string BaseMixPath = "/mix";

        public static string Generate()
        {
            string salt = "K10B93N601N1514nD438597b23v23b54235uv23523459u234c23";
            string milliseconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string generatedWithTimer = Md5Hex(milliseconds);
            string generated = Md5Hex(salt + milliseconds + generatedWithTimer);
            return generated;
        }

        public static string Md5Hex(string input)
        {
            using var md5 = MD5.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = md5.ComputeHash(bytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static async Task<object?> MixAPIMiddleWare(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var ctx = context.HttpContext;
            var contentType = ctx.Request.Headers["Content-Type"].ToString();

            ctx.Response.Headers["X-Cloud-Trace-Context"] = Generate();
            ctx.Response.Headers["Server"] = "Google Frontend";
            ctx.Response.Headers["Alt-Svc"] = "quic=\":443\"; ma=2592000; v=\"44,43,39,35\"";

            string accesstoken = "";
            if (ctx.Request.Headers.TryGetValue("X-Mix-OneIdToken", out var authHeader))
            {
                accesstoken = authHeader;
            }
            else
            {
                Console.WriteLine("[WebAPI][MIX] Missing OneIdToken");
                await ResponseHelper.WriteJsonAsync(ctx, new BaseResponse { Status = "UNAUTHORIZED_MIX_SESSION" }, 401);
                return null;
            }


            var session = await DatabaseManager.GetSessionAccessToken(ctx.Request.Headers["X-Mix-OneIdToken"].ToString());
            if (session is null)
            {
                await ResponseHelper.WriteJsonAsync(ctx, new BaseResponse { Status = "UNAUTHORIZED_ONEID_TOKEN" }, 401);
                return null;
            }

            if (!string.IsNullOrEmpty(contentType) && contentType.Contains("application/mix"))
            {
                ctx.Request.EnableBuffering();
                using StreamReader reader = new StreamReader(
                    ctx.Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);
                using var ms = new MemoryStream();
                await reader.BaseStream.CopyToAsync(ms);
                byte[] blob = ms.ToArray();

                ctx.Request.Body.Position = 0;
                string decrypted = DumbfoxEncryption.DecryptIvPrependedBytes(blob, Convert.FromBase64String((string)session["aesSecret"]));

                PathString path = ctx.Request.Path;

                if (path.StartsWithSegments("/mix/notifications/sinceSequence") || path.StartsWithSegments("/notifications/sinceSequence"))
                {
                    JsonElement obj2 = JsonSerializer.Deserialize<JsonElement>(decrypted);
                    ctx.Items["DecryptedJson"] = obj2;
                    ctx.Items["DecryptedModel"] = decrypted;
                    return await next(context);
                }
                Console.WriteLine($"[WebAPI][MIX] Decrypted body: {decrypted}");
                JsonElement obj = JsonSerializer.Deserialize<JsonElement>(decrypted);

                ctx.Items["DecryptedJson"] = obj;

                ctx.Items["DecryptedModel"] = decrypted;
            }
            else
            {
                ctx.Request.EnableBuffering();
                using StreamReader reader = new StreamReader(
                    ctx.Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);
                string body = await reader.ReadToEndAsync();
                ctx.Request.Body.Position = 0;

                Console.WriteLine($"[WebAPI][MIX] body: {body}");
                JsonElement obj = JsonSerializer.Deserialize<JsonElement>(body);

                ctx.Items["DecryptedJson"] = obj;

                ctx.Items["DecryptedModel"] = body;
            }



            var result = await next(context);
            return result;
        }

        public static void Register(WebApplication app)
        {

            app.MapPost(BasePath + "registration/text", RegistrationTextPost.Handle);
            app.MapPost(BasePath + "session/user/delete", SessionUserDeletePost.Handle);
            app.MapPut(BasePath + "session/user", SessionUserPut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "state", StatePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "languagePreference", LanguagePreferencePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "notifications/sinceSequence", NotificationsSinceSequencePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "officialAccount/all", OfficialAccountAllPost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BasePath + "displayname", DisplaynamePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "search/displayname", SearchDisplaynamePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BasePath + "avatar", AvatarPut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });

            app.MapPut(BasePath + "friendship", FriendshipPut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "friendship/delete", FriendshipDeletePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BasePath + "friendship/invitation", FriendshipInvitationPut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "friendship/invitation/delete", FriendshipInvitationDeletePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "friendship/recommend", FriendshipRecommendPost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "friendship/trust/delete", FriendshipTrustDeletePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });

            app.MapPut(BasePath + "chatThread", ChatThreadPut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "chatThread/messages/recent", ChatThreadMessagesRecentPost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BasePath + "chatThread/messages/bySequenceRange", ChatThreadMessagesBySequenceRangePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BasePath + "chatThread/textMessage", ChatThreadTextMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BasePath + "chatThread/stickerMessage", ChatThreadStickerMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BasePath + "chatThread/gagMessage", ChatThreadGagMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BasePath + "chatThread/videoMessage", ChatThreadVideoMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BasePath + "chatThread/photoMessage", ChatThreadPhotoMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BasePath + "chatThread/gameStateMessage", ChatThreadGameStateMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });

            app.MapPost(BaseMixPath + "/registration/text", RegistrationTextPost.Handle);
            app.MapPost(BaseMixPath + "/session/user/delete", SessionUserDeletePost.Handle);
            app.MapPut(BaseMixPath + "/session/user", SessionUserPut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/state", StatePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/languagePreference", LanguagePreferencePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/notifications/sinceSequence", NotificationsSinceSequencePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/officialAccount/all", OfficialAccountAllPost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BaseMixPath + "/displayname", DisplaynamePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/search/displayname", SearchDisplaynamePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BaseMixPath + "/avatar", AvatarPut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });

            app.MapPut(BaseMixPath + "/friendship", FriendshipPut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/friendship/delete", FriendshipDeletePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BaseMixPath + "/friendship/invitation", FriendshipInvitationPut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/friendship/invitation/delete", FriendshipInvitationDeletePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/friendship/recommend", FriendshipRecommendPost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/friendship/trust/delete", FriendshipTrustDeletePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });

            app.MapPut(BaseMixPath + "/chatThread", ChatThreadPut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/chatThread/messages/recent", ChatThreadMessagesRecentPost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPost(BaseMixPath + "/chatThread/messages/bySequenceRange", ChatThreadMessagesBySequenceRangePost.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BaseMixPath + "/chatThread/textMessage", ChatThreadTextMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BaseMixPath + "/chatThread/stickerMessage", ChatThreadStickerMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BaseMixPath + "/chatThread/gagMessage", ChatThreadGagMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BaseMixPath + "/chatThread/videoMessage", ChatThreadVideoMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BaseMixPath + "/chatThread/photoMessage", ChatThreadPhotoMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
            app.MapPut(BaseMixPath + "/chatThread/gameStateMessage", ChatThreadGameStateMessagePut.Handle).AddEndpointFilter(async (context, next) => { return await MixAPIMiddleWare(context, next); });
        }
    }
}
