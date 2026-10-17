using Common.Utils;
using Common.Utils.Jgc;
using Common.Utils.Mix;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using MySqlConnector;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using WebAPI;
using WebAPI.Api.Jgc;
namespace WebAPI.Api.Jgc.routes;

public class Register0
{

    private static string GenerateHex(int bytesLength)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(bytesLength);
        return Convert.ToHexString(bytes).ToLower();
    }


    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][JgcRoutes] Got Register0");

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            // PropertyNamingPolicy = JsonNamingPolicy.Never,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        await using var conn = await DatabaseManager.OpenConnectionAsync();

        ctx.Request.EnableBuffering();

        using StreamReader reader = new StreamReader(
            ctx.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        string body = await reader.ReadToEndAsync();

        ctx.Request.Body.Position = 0;

        Console.WriteLine($"[WebAPI][JgcRoutes][From Client by Register] Body: {body}");

        RegisterRequest? obj = JsonSerializer.Deserialize<RegisterRequest>(body);

        string ageAssumed = "CHILD";

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
            "aunt_artic",
            "rockhopper",
            "rock_hopper",
            "captainrockhopper",
            "captain_rockhopper",
            "captain_rock_hopper",
            "captainrock_hopper",
            "gary",
            "dot",
            "rookie",
            "djcadance",
            "dj_cadance",
            "cadance",
            "rory",
            "klutzy",
            "herbert",
            "pufflehandler",
            "puffle_handler",
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
            if (ContainsBlacklistedSmart(username, blacklist))
                return false;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(obj.profile.username))
        {
            if (obj.profile.username.Length < 3 || obj.profile.username.Length > 16)
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE_USERNAME_SIZE, 400, InputNameTypes.ProfileUsername);
                return;
            }

            if (!IsBlacklistedUsername(obj.profile.username, blacklistUser))
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.DISALLOWED_VALUE, 400, InputNameTypes.ProfileUsername);
                return;
            }

            if (IsValidUsername(obj.profile.username))
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE_FILTHY, 400, InputNameTypes.ProfileUsername);
                return;
            }

            await using var cmd1 = new MySqlCommand(@"SELECT * FROM accounts WHERE username = @username AND status IN ('PERMA', 'BANNED', 'LOCKED', 'ACTIVE', 'NO_EMAIL_VERFIED');", conn);
            cmd1.Parameters.AddWithValue("@username", obj.profile.username);

            await using var reader0 = await cmd1.ExecuteReaderAsync();

            if (await reader0.ReadAsync())
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INUSE_VALUE, 400, InputNameTypes.ProfileUsername);
                return;
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

        var blockedTlds = new[] { "ru" };

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

        if (!string.IsNullOrWhiteSpace(obj.profile.parentEmail))
        {
            ageAssumed = "CHILD";
            if (!IsValidEmail(obj.profile.parentEmail))
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE, 400, InputNameTypes.ProfileParentEmail);
                return;
            }

            if (obj.profile.parentEmail.Length < 10 || obj.profile.parentEmail.Length > 50)
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE_USERNAME_SIZE, 400, InputNameTypes.ProfileParentEmail);
                return;
            }

            if (IsBlockedEmail(obj.profile.parentEmail, blacklist, blockedTlds))
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.PARENT_EMAIL_BLACKLISTED, 400, InputNameTypes.ProfileParentEmail);
                return;
            }
        }


        if (!string.IsNullOrWhiteSpace(obj.profile.email))
        {
            ageAssumed = "ADULT";
            if (!IsValidEmail(obj.profile.email))
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE, 400, InputNameTypes.ProfileParentEmail);
                return;
            }

            if (obj.profile.email.Length < 10 || obj.profile.email.Length > 50)
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.INVALID_VALUE_USERNAME_SIZE, 400, InputNameTypes.ProfileParentEmail);
                return;
            }

            if (IsBlockedEmail(obj.profile.email, blacklist, blockedTlds))
            {
                await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.PARENT_EMAIL_BLACKLISTED, 400, InputNameTypes.ProfileParentEmail);
                return;
            }


            if (string.IsNullOrWhiteSpace(obj.profile.username))
            {
                await using var cmd1 = new MySqlCommand(@"SELECT * FROM accounts WHERE email = @email AND status IN ('PERMA', 'BANNED', 'LOCKED', 'ACTIVE', 'NO_EMAIL_VERFIED');", conn);
                cmd1.Parameters.AddWithValue("@email", obj.profile.email);

                await using var reader0 = await cmd1.ExecuteReaderAsync();

                if (await reader0.ReadAsync())
                {

                    bool valid1 = BCrypt.Net.BCrypt.Verify(obj.password, (string)reader0["penguinword"]);
                    if (valid1)
                        await ResponseHelper.WriteJgcError(ctx, CategoryTypes.ACTIONABLE_INPUT, RegisterValidationCode.ACCOUNT_FOUND, 400, InputNameTypes.ProfilePassword);
                    return;
                }
            }
        }

        string swid0 = Guid.NewGuid().ToString("B").ToUpper();

        await using var cmd3 = new MySqlCommand(
    @"INSERT INTO accounts (swid, referenceid, email, penguinword, username, displayname, registeredday, ageassume, birthday, region ) VALUES (@swid, @refid, @email, @penguinword, @username, @username, @registeredday, @ageassume, @birthday, @region);",
    conn);
        cmd3.Parameters.AddWithValue("@swid", swid0);
        cmd3.Parameters.AddWithValue("@refid", Guid.NewGuid().ToString("N").ToLower());
        if (!string.IsNullOrWhiteSpace(obj.profile.parentEmail))
        {
            cmd3.Parameters.AddWithValue("@email", obj.profile.parentEmail);
            cmd3.Parameters.AddWithValue("@username", obj.profile.username);
        }
        else
        {
            cmd3.Parameters.AddWithValue("@email", obj.profile.email);
            cmd3.Parameters.AddWithValue("@username", "CP" + new Random().Next(1, 999999));
        }
        cmd3.Parameters.AddWithValue("@penguinword", BCrypt.Net.BCrypt.HashPassword(obj.password, workFactor: 10));
        cmd3.Parameters.AddWithValue("@registeredday", JgcRoutes.FormatTimestamp());
        cmd3.Parameters.AddWithValue("@ageassume", ageAssumed);
        cmd3.Parameters.AddWithValue("@birthday", obj.profile.dateOfBirth ?? "2026-01-01");
        cmd3.Parameters.AddWithValue("@region", obj.profile.region ?? "US");
        await cmd3.ExecuteNonQueryAsync();



        await using var cmd5 = new MySqlCommand(@"SELECT * FROM accounts WHERE email = @email AND swid = @swid AND status IN ('PERMA', 'BANNED', 'LOCKED', 'ACTIVE', 'NO_EMAIL_VERFIED');", conn);
        cmd5.Parameters.AddWithValue("@email", obj.profile.email ?? obj.profile.parentEmail);
        cmd5.Parameters.AddWithValue("@swid", swid0);

        long id;
        string swid;
        string hash;
        string username;
        string birthday;
        string email;
        string assumeage;
        string region;

        await using (var reader4 = await cmd5.ExecuteReaderAsync())
        {
            if (!await reader4.ReadAsync())
                throw new Exception("User not found after insert");

            id =  reader4.GetInt64("id");
            swid = reader4["swid"].ToString();
            hash = reader4["penguinword"].ToString();
            username = reader4["username"].ToString();
            birthday = reader4["birthday"].ToString();
            email = reader4["email"].ToString();
            assumeage = reader4["ageassume"].ToString();
            region = reader4["region"].ToString();
        } 


            var eTagData = new
            {
                profileETag = Md5Hex(swid),
                displayNameETag = Md5Hex(username)
            };

            static string Md5Hex(string input)
            {
                using var md5 = MD5.Create();
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = md5.ComputeHash(bytes);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }

            // Build ETag (Node equivalent)
            string eTagJson = JsonSerializer.Serialize(eTagData);
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(eTagJson));
            string etag = $"W/\"{base64}\"";

            long eightDigitTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();



            LogInResponse response;

            if (ageAssumed == "CHILD")
            {
                response = new LogInResponse
                {
                    data = new LogInData
                    {
                        etag = etag,

                        profile = new Profile
                        {

                            swid = swid,
                            username = username,
                            dateOfBirth = birthday,
                            ageBand = assumeage,
                            region = region,
                            parentEmail = email,
                            languagePreference = obj.profile.languagePreference,
                            parentEmailVerified = true

                        },
                        displayName = new DisplayName
                        {
                            displayName = username,
                            proposedDisplayName = $"CP{id}"
                        },
                        marketing = obj.marketing,
                        token = new Token
                        {
                            access_token = GenerateHex(16),
                            refresh_token = GenerateHex(16),
                            swid = swid,
                            ttl = 86400,
                            refresh_ttl = eightDigitTimestamp
                        }
                    }
                };
            }
            else
            {
                response = new LogInResponse
                {
                    data = new LogInData
                    {
                        etag = etag,

                        profile = new Profile
                        {

                            swid = swid,
                            username = username,
                            dateOfBirth = birthday,
                            ageBand = assumeage,
                            region = region,
                            email = email,
                            languagePreference = obj.profile.languagePreference,
                            emailVerified = true

                        },
                        displayName = new DisplayName
                        {
                            displayName = username,
                            proposedDisplayName = $"CP{id}"
                        },
                        marketing = obj.marketing,
                        token = new Token
                        {
                            access_token = GenerateHex(16),
                            refresh_token = GenerateHex(16),
                            swid = swid,
                            ttl = 86400,
                            refresh_ttl = eightDigitTimestamp
                        }
                    }
                };
            }

            string json = JsonSerializer.Serialize(response, options);

            Console.WriteLine(json);    
            string apiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(86));

            ctx.Response.Headers["api-key"] = apiKey;

            await using var cmd = new MySqlCommand(
                "INSERT INTO sessions (session_id, expires_at) VALUES (@apikey, DATE_ADD(NOW(), INTERVAL 1440 MINUTE));",
                conn);
            cmd.Parameters.AddWithValue("@apikey", apiKey);
            await cmd.ExecuteNonQueryAsync();

            DatabaseManager.GetSessionApiKeyAndAddtoSessionUsers(apiKey, response.data.token.access_token, response.data.profile.swid, response.data.token.refresh_token, response.data.profile.username, ctx.Request.RouteValues["clientId"]?.ToString());



            await using var cmd4 = new MySqlCommand(@"INSERT INTO profiles (swid, registertimestamp) VALUES (@swid, @currentday);", conn);
            cmd4.Parameters.AddWithValue("@swid", response.data.profile.swid);
            cmd4.Parameters.AddWithValue("@currentday", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            await cmd4.ExecuteNonQueryAsync();

            await using var cmd6 = new MySqlCommand(@"INSERT INTO quests (swid, questId) VALUES (@swid, @questId);", conn);
            cmd6.Parameters.AddWithValue("@swid", response.data.profile.swid);
            cmd6.Parameters.AddWithValue("@questId", "AAC001Q001LeakyShip");
            await cmd6.ExecuteNonQueryAsync();

            await using var cmd7 = new MySqlCommand(@"INSERT INTO quests (swid, questId) VALUES (@swid, @questId);", conn);
            cmd7.Parameters.AddWithValue("@swid", response.data.profile.swid);
            cmd7.Parameters.AddWithValue("@questId", "RHC001Q001TreasureQuest");
            await cmd7.ExecuteNonQueryAsync();

            await using var cmd8 = new MySqlCommand(@"INSERT INTO quests (swid, questId) VALUES (@swid, @questId);", conn);
            cmd8.Parameters.AddWithValue("@swid", response.data.profile.swid);
            cmd8.Parameters.AddWithValue("@questId", "RKC001Q001Drop");
            await cmd8.ExecuteNonQueryAsync();

            await using var cmd9 = new MySqlCommand(@"INSERT INTO quests (swid, questId) VALUES (@swid, @questId);", conn);
            cmd9.Parameters.AddWithValue("@swid", response.data.profile.swid);
            cmd9.Parameters.AddWithValue("@questId", "DJC001Q001Plan");
            await cmd9.ExecuteNonQueryAsync();

            ctx.Response.ContentType = "application/json;charset=utf-8";
            await ctx.Response.WriteAsync(json);


    }
}

