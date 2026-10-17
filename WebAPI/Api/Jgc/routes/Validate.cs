using Common.Utils;
using Common.Utils.Jgc;
using Microsoft.AspNetCore.Http;
using MySqlConnector;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
namespace WebAPI.Api.Jgc.routes;

public class Validate
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][JgcRoutes] Got Validate");
        ctx.Request.EnableBuffering();

        using StreamReader reader = new StreamReader(
            ctx.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        string body = await reader.ReadToEndAsync();

        ctx.Request.Body.Position = 0;

        Console.WriteLine($"[WebAPI][JgcRoutes][From Client by Validate] Body: {body}");
        await using var conn = await DatabaseManager.OpenConnectionAsync();
        ValidateRequest? obj = JsonSerializer.Deserialize<ValidateRequest>(body);

        if (!string.IsNullOrWhiteSpace(obj.username) && !string.IsNullOrWhiteSpace(obj.email))
        {
            await ResponseHelper.WriteJgcError(ctx, CategoryTypes.FAILURE_BY_DESIGN, GenericCode.MALFORMED_INPUT, 400);
            return;
        }

        bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            // Check allowed characters
            if (!Regex.IsMatch(username, @"^[A-Za-z0-9_]+$"))
                return false;

            // Count digits
            int digitCount = username.Count(char.IsDigit);

            return digitCount >= 6 && digitCount <= 7;
        }

        string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var map = new Dictionary<char, char>
            {
                ['4'] = 'a',
                ['@'] = 'a',
                ['3'] = 'e',
                ['1'] = 'i',
                ['!'] = 'i',
                ['0'] = 'o',
                ['5'] = 's',
                ['$'] = 's',
                ['7'] = 't',
                [' '] = '_',
                ['_'] = '_'
            };

            var sb = new StringBuilder();

            foreach (var c in input.ToLowerInvariant())
            {
                if (map.TryGetValue(c, out var replacement))
                    sb.Append(replacement);
                else if (char.IsLetterOrDigit(c))
                    sb.Append(c);
                // ignore underscores and other separators
            }

            return sb.ToString();
        }

        bool ContainsBlacklistedWord(string username, string[] blacklist)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            var lower = username.ToLowerInvariant();

            foreach (var word in blacklist)
            {
                if (lower.Contains(word.ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        var blacklistUser = new[]
        {
            "auntartic",
            "rockhopper",
            "gary",
            "dot",
            "rookie",
            "djcadance",
            "cadance",
            "rory",
            "klutzy",
            "herbert",
            "pufflehandler",
            "shellbeard",
            "testplayer"
        };


        bool ContainsBlacklistedSmart(string username, string[] blacklist)
        {
            var normalized = Normalize(username);

            foreach (var word in blacklist)
            {
                if (normalized.Contains(word.ToLowerInvariant()))
                    return true;
            }

            return false;
        }


        bool IsBlacklistedUsername(string username, string[] blacklist)
        {
            if (ContainsBlacklistedWord(username, blacklist))
                return false;
            if(ContainsBlacklistedSmart(username, blacklist))
                return false;
            return true;
        }

        if (string.IsNullOrWhiteSpace(obj.password) || obj.password == "testing123")
        {
            if (!string.IsNullOrWhiteSpace(obj.username))
            {
                if (obj.username.Length < 3 || obj.username.Length > 16)
                {
                    await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE_USERNAME_SIZE, 400, InputNameTypes.ProfileUsername);
                    return;
                }

                if (!IsBlacklistedUsername(obj.username, blacklistUser))
                {
                    await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.DISALLOWED_VALUE, 400, InputNameTypes.ProfileUsername);
                    return;
                }

                if (IsValidUsername(obj.username))
                {
                    await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE_FILTHY, 400, InputNameTypes.ProfileUsername);
                    return;
                }

                await using var cmd = new MySqlCommand(@"SELECT * FROM accounts WHERE username = @username AND status IN ('PERMA', 'BANNED', 'LOCKED', 'ACTIVE', 'NO_EMAIL_VERFIED');", conn);
                cmd.Parameters.AddWithValue("@username", obj.username);

                await using var reader0 = await cmd.ExecuteReaderAsync();

                if (await reader0.ReadAsync())
                {
                    await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INUSE_VALUE, 400, InputNameTypes.ProfileUsername);
                    return;
                }
            }
        }

        bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        string GetDomain(string email)
        {
            var atIndex = email.LastIndexOf('@');
            return atIndex >= 0 ? email[(atIndex + 1)..].ToLowerInvariant() : null;
        }

        bool IsBlacklisted(string email, string[] blacklist)
        {
            var domain = GetDomain(email);
            if (string.IsNullOrEmpty(domain))
                return false;

            foreach (var blocked in blacklist)
            {
                var b = blocked.ToLowerInvariant();
                if (domain == b || domain.EndsWith("." + b))
                    return true;
            }

            return false;
        }

        bool HasBlockedTld(string email, string[] blockedTlds)
        {
            var domain = GetDomain(email);
            if (string.IsNullOrEmpty(domain))
                return false;

            foreach (var tld in blockedTlds)
            {
                if (domain.EndsWith("." + tld, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        var blockedTlds = new[] {"ru"};

        var blacklist = new[]
        {
            "disney.com",
            "wallet.disney.com",
            "panfu.com",
            "panfu.us",
            "panfu.me",
            "pannun.de",
            "pannun.net",
            "pannun.eu"
        };


        bool IsBlockedEmail(string email, string[] blockedDomains, string[] blockedTlds)
        {
            if (!IsValidEmail(email))
                return true;

            if (email.Contains(".exemple") || email.Contains(".exemple.") || email.Contains(".10minutemail.") || email.Contains("10minutemail.") || email.Contains(".tpwlb.") || email.Contains("tpwlb.") || email.Contains("tcwlm.") || email.Contains("bbitq.") || email.Contains("bbitf.") || email.Contains("tmmcv.") || email.Contains("tmmwj.") || email.Contains("tmmbt.") || email.Contains("kvhrw.") || email.Contains("nvhrw.") || email.Contains("bvhrs.") || email.Contains("ttirv.") || email.Contains("ttirv.") || email.Contains(".tcwlm.") || email.Contains(".bbitq.") || email.Contains(".bbitf.") || email.Contains(".tmmcv.") || email.Contains(".tmmwj.") || email.Contains(".tmmbt.") || email.Contains(".kvhrw.") || email.Contains(".nvhrw.") || email.Contains(".bvhrs.") || email.Contains(".ttirv.") || email.Contains(".ttirv.") || email.Contains("emlpro.") || email.Contains("emltmp.") || email.Contains("inboxbear.") || email.Contains("incognitomail.") || email.Contains("instant-mail.") || email.Contains(".emlpro.") || email.Contains(".emltmp.") || email.Contains(".inboxbear.") || email.Contains(".incognitomail.") || email.Contains(".instant-mail."))
                return true;

            if (IsBlacklisted(email, blockedDomains))
                return true;

            if (HasBlockedTld(email, blockedTlds))
                return true;

            return false;
        }

        if (!string.IsNullOrWhiteSpace(obj.email))
        {
            if (!IsValidEmail(obj.email))
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE, 400, InputNameTypes.ProfileParentEmail);
                return;
            }

            if (obj.email.Length < 10 || obj.email.Length > 50)
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE_USERNAME_SIZE, 400, InputNameTypes.ProfileParentEmail);
                return;
            }

            if (IsBlockedEmail(obj.email, blacklist, blockedTlds)) {

                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.PARENT_EMAIL_BLACKLISTED, 400, InputNameTypes.ProfileParentEmail);
                return;
            }
        }

        await ctx.Response.WriteAsJsonAsync(new ValidateResponse { });
    }
}

