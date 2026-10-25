using Common.Utils.Game;
using Common.Utils.Mix;
using MySqlConnector;
using Sfs2X.Entities;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using static Common.Utils.Game.DecorationLayout;
using static System.Net.Mime.MediaTypeNames;

namespace Common.Utils;

public static class DatabaseManager
{
    private static string? _connectionString;
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);
    public static string? ConnectionString => _connectionString;

    public static void Configure(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public static bool TryConfigureFromEnvironment()
    {
        string? cs = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(cs))
            return false;
        _connectionString = cs;
        return true;
    }

    public static MySqlConnection CreateConnection()
    {
        EnsureConfigured();
        return new MySqlConnection(_connectionString);
    }

    public static async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        MySqlConnection conn = CreateConnection();
        try
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            return conn;
        }
        catch
        {
            await conn.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public static async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return false;
        try
        {
            await using MySqlConnection conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            return conn.State == ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }

    public static async Task GetSessionApiKeyAndAddtoSessionUsers(string apikey, string accessToken, string swid, string refreshToken, string username, string clientId)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(
            "SELECT seeid FROM sessions WHERE session_id = @apikey;",
            conn);

        cmd.Parameters.AddWithValue("@apikey", apikey);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new Exception("Session not found");
        }

        var seeid = reader.GetInt64("seeid");

        await reader.CloseAsync();

        await using var cmd4 = new MySqlCommand(
            @"INSERT INTO sessionusers (seeid, access_token, swid, refresh_token, username, isclient) VALUES (@seeid, @accessToken, @swid, @refreshToken, @username, @clientId);",
            conn);

        cmd4.Parameters.AddWithValue("@seeid", seeid);
        cmd4.Parameters.AddWithValue("@accessToken", accessToken);
        cmd4.Parameters.AddWithValue("@swid", swid);
        cmd4.Parameters.AddWithValue("@refreshToken", refreshToken);
        cmd4.Parameters.AddWithValue("@username", username);
        cmd4.Parameters.AddWithValue("@clientId", clientId);

        await cmd4.ExecuteNonQueryAsync();
    }

    public static async Task<Dictionary<string, object>?> GetSessionApiKey(string apikey)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(
            "SELECT * FROM sessions WHERE session_id = @apikey;",
            conn);

        cmd.Parameters.AddWithValue("@apikey", apikey);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var result = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                result[reader.GetName(i)] = reader.GetValue(i);
            }

            return result;
        }

        return null;
    }

    public static async Task<Dictionary<string, object>?> GetSessionId(long sess)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(
            "SELECT * FROM sessionusers WHERE sessionId = @sess;",
            conn);

        cmd.Parameters.AddWithValue("@sess", sess);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var result = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                result[reader.GetName(i)] = reader.GetValue(i);
            }

            return result;
        }

        return null;
    }

    public static async Task<Dictionary<string, object>?> GetSessionAccessToken(string accesstoken)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(
            "SELECT * FROM sessionusers WHERE access_token = @token;",
            conn);

        cmd.Parameters.AddWithValue("@token", accesstoken);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var result = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                result[reader.GetName(i)] = reader.GetValue(i);
            }

            return result;
        }

        return null;
    }

    public static async Task<Dictionary<string, object>?> GetAccountSwid(string swid)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(
            @"SELECT swid, username, displayname FROM accounts WHERE swid = @swid;",
            conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var result = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                result[reader.GetName(i)] = reader.GetValue(i);
            }

            return result;
        }

        return null;
    }

    public static async Task<Dictionary<string, object>?> GetAccountUserName(string username)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(
            @"SELECT swid, username, displayname FROM accounts WHERE username = @displayname;",
            conn);

        cmd.Parameters.AddWithValue("@displayname", username);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var result = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                result[reader.GetName(i)] = reader.GetValue(i);
            }

            return result;
        }

        return null;
    }

    public static async Task<WorldsDB?> GetWorldByWorld(string world)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"SELECT * FROM worlds WHERE name = @world;", conn);

        cmd.Parameters.AddWithValue("@world", world);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new WorldsDB
            {
                Id = reader.GetInt64("id"),
                Name = reader.GetString("name"),
                LangId = reader.GetInt32("langid"),
                UserCount = reader.GetInt32("usercount"),
                Host = reader.GetString("host"),
                TcpPort = reader.GetInt32("tcpport"),
                HttpsPort = reader.GetInt32("httpsport"),
                WsPort = reader.GetInt32("wsport"),
                WssPort = reader.GetInt32("wssport"),
                WSHost = reader.GetString("wshost"),
            };
        }

        return null;
    }

    public static async Task<ProfilesDB?> GetProfilesSwid(string userId)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(
            @"SELECT swid, registertimestamp, langid, lang, lastscene, lastworld, instance, colour, auntarctic, djcadence, rockhopper, rookie, coins, chocolite, coconut, coin, confettiflower, crystalcluster, crystalice, crystalsmall, crystalsnowflake, flourflower, pearl, rainbowdrop, seasponge, seaweed, shell, windchime, member, selectedTubeId, Accessory, AccessoryTintIndex, CAST(AccessoryXOffeset AS DOUBLE) AS AccessoryXOffeset, CAST(AccessoryYOffeset AS DOUBLE) AS AccessoryYOffeset, Costume, CostumeTintIndex, CAST(CostumeXOffeset AS DOUBLE) AS CostumeXOffeset, CAST(CostumeYOffeset AS DOUBLE) AS CostumeYOffeset, Brow, BrowTintIndex, CAST(BrowXOffeset AS DOUBLE) AS BrowXOffeset, CAST(BrowYOffeset AS DOUBLE) AS BrowYOffeset, Eyes, EyesTintIndex, CAST(EyesXOffeset AS DOUBLE) AS EyesXOffeset, CAST(EyesYOffeset AS DOUBLE) AS EyesYOffeset, Hair, HairTintIndex, CAST(HairXOffeset AS DOUBLE) AS HairXOffeset, CAST(HairYOffeset AS DOUBLE) AS HairYOffeset, Nose, NoseTintIndex, CAST(NoseXOffeset AS DOUBLE) AS NoseXOffeset, CAST(NoseYOffeset AS DOUBLE) AS NoseYOffeset, Mouth, MouthTintIndex, CAST(MouthXOffeset AS DOUBLE) AS MouthXOffeset, CAST(MouthYOffeset AS DOUBLE) AS MouthYOffeset, Skin, SkinTintIndex, CAST(SkinXOffeset AS DOUBLE) AS SkinXOffeset, CAST(SkinYOffeset AS DOUBLE) AS SkinYOffeset, Hat, HatTintIndex, CAST(HatXOffeset AS DOUBLE) AS HatXOffeset, CAST(HatYOffeset AS DOUBLE) AS HatYOffeset FROM profiles WHERE swid = @swid;",
            conn);

        cmd.Parameters.AddWithValue("@swid", userId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new ProfilesDB
            {
                Swid = reader.GetString("swid"),
                RegisterTimestamp = reader.GetInt64("registertimestamp"),
                LangId = reader.GetInt32("langid"),
                Lang = reader.GetString("lang"),
                LastScene = reader.GetString("lastscene"),
                LastWorld = reader.GetString("lastworld"),
                Instance = reader.GetString("instance"),
                Colour = reader.GetInt32("colour"),
                Auntarctic = reader.GetInt32("auntarctic"),
                Djcadence = reader.GetInt32("djcadence"),
                Rockhopper = reader.GetInt32("rockhopper"),
                Rookie = reader.GetInt32("rookie"),
                Coins = reader.GetInt32("coins"),
                Chocolite = reader.GetInt32("chocolite"),
                Coconut = reader.GetInt32("coconut"),
                Coin = reader.GetInt32("coin"),
                Confettiflower = reader.GetInt32("confettiflower"),
                Crystalcluster = reader.GetInt32("crystalcluster"),
                Crystalice = reader.GetInt32("crystalice"),
                Crystalsmall = reader.GetInt32("crystalsmall"),
                Crystalsnowflake = reader.GetInt32("crystalsnowflake"),
                Flourflower = reader.GetInt32("flourflower"),
                Pearl = reader.GetInt32("pearl"),
                Rainbowdrop = reader.GetInt32("rainbowdrop"),
                Seasponge = reader.GetInt32("seasponge"),
                Seaweed = reader.GetInt32("seaweed"),
                Shell = reader.GetInt32("shell"),
                Windchime = reader.GetInt32("windchime"),
                Member = reader.GetBoolean("member"),
                SelectedTubeId = reader.GetInt32("selectedTubeId"),
                Accessory = reader.GetString("Accessory"),
                AccessoryTintIndex = reader.GetInt32("AccessoryTintIndex"),
                AccessoryXOffeset = reader.GetDouble("AccessoryXOffeset"),
                AccessoryYOffeset = reader.GetDouble("AccessoryYOffeset"),
                Costume = reader.GetString("Costume"),
                CostumeTintIndex = reader.GetInt32("CostumeTintIndex"),
                CostumeXOffeset = reader.GetDouble("CostumeXOffeset"),
                CostumeYOffeset = reader.GetDouble("CostumeYOffeset"),
                Brow = reader.GetString("Brow"),
                BrowTintIndex = reader.GetInt32("BrowTintIndex"),
                BrowXOffeset = reader.GetDouble("BrowXOffeset"),
                BrowYOffeset = reader.GetDouble("BrowYOffeset"),
                Eyes = reader.GetString("Eyes"),
                EyesTintIndex = reader.GetInt32("EyesTintIndex"),
                EyesXOffeset = reader.GetDouble("EyesXOffeset"),
                EyesYOffeset = reader.GetDouble("EyesYOffeset"),
                Hair = reader.GetString("Hair"),
                HairTintIndex = reader.GetInt32("HairTintIndex"),
                HairXOffeset = reader.GetDouble("HairXOffeset"),
                HairYOffeset = reader.GetDouble("HairYOffeset"),
                Nose = reader.GetString("Nose"),
                NoseTintIndex = reader.GetInt32("NoseTintIndex"),
                NoseXOffeset = reader.GetDouble("NoseXOffeset"),
                NoseYOffeset = reader.GetDouble("NoseYOffeset"),
                Mouth = reader.GetString("Mouth"),
                MouthTintIndex = reader.GetInt32("MouthTintIndex"),
                MouthXOffeset = reader.GetDouble("MouthXOffeset"),
                MouthYOffeset = reader.GetDouble("MouthYOffeset"),
                Skin = reader.GetString("Skin"),
                SkinTintIndex = reader.GetInt32("SkinTintIndex"),
                SkinXOffeset = reader.GetDouble("SkinXOffeset"),
                SkinYOffeset = reader.GetDouble("SkinYOffeset"),
                Hat = reader.GetString("Hat"),
                HatTintIndex = reader.GetInt32("HatTintIndex"),
                HatXOffeset = reader.GetDouble("HatXOffeset"),
                HatYOffeset = reader.GetDouble("HatYOffeset")
            };
        }

        return null;
    }

    public static async Task<ConsumableInventory> GetConsumableInventory(string swid)
    {
        ConsumableInventory inventory = new ConsumableInventory();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(
            "SELECT type, item_count, partial_count, last_purchase_timestamp FROM consumables WHERE swid = @swid;",
            conn);
        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            inventory.inventoryMap[reader.GetString("type")] = new InventoryItemStock
            {
                itemCount = reader.GetInt32("item_count"),
                partialCount = reader.GetInt32("partial_count"),
                lastPurchaseTimestamp = reader.GetInt64("last_purchase_timestamp")
            };
        }

        return inventory;
    }

    public static async Task<(ConsumableInventory Inventory, long Coins)> PurchaseConsumable(
        string swid,
        string type,
        int count,
        int unitCost,
        bool memberOnly)
    {
        await using var conn = await OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();
        try
        {
            long coins;
            bool member;
            await using (var profileCommand = new MySqlCommand(
                "SELECT coins, member FROM profiles WHERE swid = @swid FOR UPDATE;",
                conn,
                transaction))
            {
                profileCommand.Parameters.AddWithValue("@swid", swid);
                await using var reader = await profileCommand.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new InvalidOperationException("Player profile was not found.");
                coins = reader.GetInt64("coins");
                member = reader.GetBoolean("member");
            }

            if (!member && memberOnly)
                throw new UnauthorizedAccessException("Membership is required to purchase this consumable.");

            long totalCost = checked((long)unitCost * count);
            if (coins < totalCost)
                throw new InvalidOperationException("Not enough coins.");

            await using (var updateCoins = new MySqlCommand(
                "UPDATE profiles SET coins = coins - @cost WHERE swid = @swid;",
                conn,
                transaction))
            {
                updateCoins.Parameters.AddWithValue("@cost", totalCost);
                updateCoins.Parameters.AddWithValue("@swid", swid);
                await updateCoins.ExecuteNonQueryAsync();
            }

            long purchaseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await using (var updateInventory = new MySqlCommand(
                @"INSERT INTO consumables (swid, type, item_count, partial_count, last_purchase_timestamp)
                  VALUES (@swid, @type, @count, 0, @timestamp)
                  ON DUPLICATE KEY UPDATE item_count = item_count + VALUES(item_count), partial_count = 0, last_purchase_timestamp = VALUES(last_purchase_timestamp);",
                conn,
                transaction))
            {
                updateInventory.Parameters.AddWithValue("@swid", swid);
                updateInventory.Parameters.AddWithValue("@type", type);
                updateInventory.Parameters.AddWithValue("@count", count);
                updateInventory.Parameters.AddWithValue("@timestamp", purchaseTimestamp);
                await updateInventory.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return (await GetConsumableInventory(swid), coins - totalCost);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static async Task<(int PartialCount, string AuthorizationToken, long GoodUntil)> UseConsumable(string swid, string type)
    {
        await using var conn = await OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();
        try
        {
            int itemCount;
            int partialCount;
            await using (var readInventory = new MySqlCommand(
                "SELECT item_count, partial_count FROM consumables WHERE swid = @swid AND type = @type FOR UPDATE;",
                conn,
                transaction))
            {
                readInventory.Parameters.AddWithValue("@swid", swid);
                readInventory.Parameters.AddWithValue("@type", type);
                await using var reader = await readInventory.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new InvalidOperationException("Consumable is not in the player's inventory.");
                itemCount = reader.GetInt32("item_count");
                partialCount = reader.GetInt32("partial_count");
            }

            if (itemCount <= 0 && partialCount <= 0)
                throw new InvalidOperationException("Consumable is not in the player's inventory.");

            if (partialCount > 0)
            {
                await using var clearPartial = new MySqlCommand(
                    "UPDATE consumables SET partial_count = 0 WHERE swid = @swid AND type = @type;",
                    conn,
                    transaction);
                clearPartial.Parameters.AddWithValue("@swid", swid);
                clearPartial.Parameters.AddWithValue("@type", type);
                await clearPartial.ExecuteNonQueryAsync();
            }
            else if (itemCount > 1)
            {
                await using var decrement = new MySqlCommand(
                    "UPDATE consumables SET item_count = item_count - 1 WHERE swid = @swid AND type = @type;",
                    conn,
                    transaction);
                decrement.Parameters.AddWithValue("@swid", swid);
                decrement.Parameters.AddWithValue("@type", type);
                await decrement.ExecuteNonQueryAsync();
            }
            else
            {
                await using var consumeLast = new MySqlCommand(
                    "UPDATE consumables SET item_count = 0 WHERE swid = @swid AND type = @type;",
                    conn,
                    transaction);
                consumeLast.Parameters.AddWithValue("@swid", swid);
                consumeLast.Parameters.AddWithValue("@type", type);
                await consumeLast.ExecuteNonQueryAsync();
            }

            string authorizationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            long goodUntil = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds();
            await using (var clearExpiredUses = new MySqlCommand(
                "DELETE FROM consumable_uses WHERE expires_at <= @now;",
                conn,
                transaction))
            {
                clearExpiredUses.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                await clearExpiredUses.ExecuteNonQueryAsync();
            }

            await using (var issueUse = new MySqlCommand(
                @"INSERT INTO consumable_uses (token, swid, type, expires_at)
                  VALUES (@token, @swid, @type, @expiresAt);",
                conn,
                transaction))
            {
                issueUse.Parameters.AddWithValue("@token", authorizationToken);
                issueUse.Parameters.AddWithValue("@swid", swid);
                issueUse.Parameters.AddWithValue("@type", type);
                issueUse.Parameters.AddWithValue("@expiresAt", goodUntil);
                await issueUse.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return (partialCount, authorizationToken, goodUntil);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static async Task<bool> ConsumeConsumableUseAuthorization(string swid, string type, string token)
    {
        await using var conn = await OpenConnectionAsync();
        await using var consume = new MySqlCommand(
            "DELETE FROM consumable_uses WHERE token = @token AND swid = @swid AND type = @type AND expires_at > @now;",
            conn);
        consume.Parameters.AddWithValue("@token", token);
        consume.Parameters.AddWithValue("@swid", swid);
        consume.Parameters.AddWithValue("@type", type);
        consume.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        return await consume.ExecuteNonQueryAsync() == 1;
    }

    public static async Task<Dictionary<long, long>> AwardPartyBlasterRewards(
        IEnumerable<(string Swid, long SessionId, long InitialCoins, long Coins)> rewards)
    {
        await using var conn = await OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();
        try
        {
            Dictionary<long, long> balances = new();
            foreach ((string swid, long sessionId, long initialCoins, long coins) in rewards)
            {
                await using (var credit = new MySqlCommand(
                    @"INSERT INTO profiles (swid, coins) VALUES (@swid, @initialCoins + @coins)
                      ON DUPLICATE KEY UPDATE coins = GREATEST(coins, @initialCoins) + @coins;",
                    conn,
                    transaction))
                {
                    credit.Parameters.AddWithValue("@swid", swid);
                    credit.Parameters.AddWithValue("@initialCoins", initialCoins);
                    credit.Parameters.AddWithValue("@coins", coins);
                    if (await credit.ExecuteNonQueryAsync() < 1)
                        throw new InvalidOperationException($"Player profile could not be credited for party blaster reward: {swid}.");
                }

                await using var readBalance = new MySqlCommand(
                    "SELECT coins FROM profiles WHERE swid = @swid;",
                    conn,
                    transaction);
                readBalance.Parameters.AddWithValue("@swid", swid);
                object? result = await readBalance.ExecuteScalarAsync();
                if (result is null || result is DBNull)
                    throw new InvalidOperationException($"Player profile was not found after party blaster reward: {swid}.");
                balances.Add(sessionId, Convert.ToInt64(result));
            }

            await transaction.CommitAsync();
            return balances;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static async Task StoreConsumablePartial(string swid, string type, int partialCount)
    {
        await using var conn = await OpenConnectionAsync();
        await using var save = new MySqlCommand(
            @"INSERT INTO consumables (swid, type, item_count, partial_count, last_purchase_timestamp)
              VALUES (@swid, @type, 0, @partialCount, 0)
              ON DUPLICATE KEY UPDATE partial_count = @partialCount;",
            conn);
        save.Parameters.AddWithValue("@swid", swid);
        save.Parameters.AddWithValue("@type", type);
        save.Parameters.AddWithValue("@partialCount", partialCount);
        await save.ExecuteNonQueryAsync();
    }

    public static async Task<long> InsertCustomEquipment(string swid, CustomEquipment equipment, string displayname)
    {
        await using var conn = await OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            await using var insertCmd = new MySqlCommand(@"
            INSERT INTO inventorys 
            (swid, definitionid, datetimecreated, source, sourceid) 
            VALUES (@swid, @definitionid, @datetimecreated, @source, @sourceid);
            SELECT LAST_INSERT_ID();", conn, transaction);

            insertCmd.Parameters.AddWithValue("@swid", swid);
            insertCmd.Parameters.AddWithValue("@definitionid", equipment.definitionId);
            insertCmd.Parameters.AddWithValue("@datetimecreated", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            insertCmd.Parameters.AddWithValue("@source", $"Used ClothingDesinger to create by {displayname}");
            if (equipment.sourceId == null || equipment.sourceId == 0)
            {
                insertCmd.Parameters.AddWithValue("@sourceid", 0);
            }
            else
            {
                insertCmd.Parameters.AddWithValue("@sourceid", equipment.sourceId);
            }

            var currentInsertId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync());
            foreach (var part in equipment.parts ?? [])
            {
                foreach (var custom in part.customizations ?? [])
                {
                    await using var partCmd = new MySqlCommand(@"
                    INSERT INTO inventorycostumes
                    (id, swid, slotindex, partindex, type, definitionid, scale, rotation, repeatt, uoffset, voffset)
                    VALUES
                    (@id, @swid, @slotindex, @partindex, @type, @definitionid, @scale, @rotation, @repeat, @uoffset, @voffset);",
                    conn, transaction);

                    partCmd.Parameters.AddWithValue("@id", currentInsertId);
                    partCmd.Parameters.AddWithValue("@swid", swid);
                    partCmd.Parameters.AddWithValue("@slotindex", part.slotIndex);
                    partCmd.Parameters.AddWithValue("@partindex", custom.index);
                    partCmd.Parameters.AddWithValue("@type", custom.type);
                    partCmd.Parameters.AddWithValue("@definitionid", custom.definitionId);
                    partCmd.Parameters.AddWithValue("@scale", custom.scale);
                    partCmd.Parameters.AddWithValue("@rotation", custom.rotation);
                    partCmd.Parameters.AddWithValue("@repeat", custom.repeat);
                    partCmd.Parameters.AddWithValue("@uoffset", custom.uoffset);
                    partCmd.Parameters.AddWithValue("@voffset", custom.voffset);

                    await partCmd.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
            return currentInsertId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static async Task<long> InsertFriend(string hisID, string hisdisplayname, string meID, string meDisplayName)
    {
        await using var conn = await OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            await using var insertCmd = new MySqlCommand(@"
            INSERT INTO friends 
            (meID, hisID, username, hisusername, istrusted, isrequested, isdeleted, timestamp) 
            VALUES (@meID, @hisID, @meusername, @hisusername, @istrusted, @isrequested, @isdeleted, @timestamp);
            SELECT LAST_INSERT_ID();", conn, transaction);

            insertCmd.Parameters.AddWithValue("@meID", meID);
            insertCmd.Parameters.AddWithValue("@hisID", hisID);
            insertCmd.Parameters.AddWithValue("@meusername", meDisplayName);
            insertCmd.Parameters.AddWithValue("@hisusername", hisdisplayname);
            insertCmd.Parameters.AddWithValue("@istrusted", false);
            insertCmd.Parameters.AddWithValue("@isrequested", true);
            insertCmd.Parameters.AddWithValue("@isdeleted", false);
            insertCmd.Parameters.AddWithValue("@timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            var currentInsertId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync());

            await using var insertCmd1 = new MySqlCommand(@"
            INSERT INTO friends 
            (meID, hisID, username, hisusername, istrusted, isrequested, isdeleted, timestamp) 
            VALUES (@meID, @hisID, @meusername, @hisusername, @istrusted, @isrequested, @isdeleted, @timestamp);
            SELECT LAST_INSERT_ID();", conn, transaction);

            insertCmd1.Parameters.AddWithValue("@meID", hisID);
            insertCmd1.Parameters.AddWithValue("@hisID", meID);
            insertCmd1.Parameters.AddWithValue("@meusername", hisdisplayname);
            insertCmd1.Parameters.AddWithValue("@hisusername", meDisplayName);
            insertCmd1.Parameters.AddWithValue("@istrusted", false);
            insertCmd1.Parameters.AddWithValue("@isrequested", false);
            insertCmd1.Parameters.AddWithValue("@isdeleted", false);
            insertCmd1.Parameters.AddWithValue("@timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            await insertCmd1.ExecuteScalarAsync();

            await transaction.CommitAsync();
            return currentInsertId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static async Task<List<CurrentThemeData>> getCurrentCataClothThemes()
    {
        var result = new List<CurrentThemeData>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT * FROM clothingcatalogthemes;", conn);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new CurrentThemeData
            {
                scheduledThemeChallengeId = reader.GetInt64("scheduledthemechallengeid"),
                userSubmissionClothingCatalogId = reader.GetInt64("usersubmissionclothingcatalogid"),
                mostPopularItem = await getMostSolditem(reader.GetInt64("scheduledthemechallengeid")),
                submissionReward = new Reward
                {
                    coins = reader.GetInt64("coins"),
                    currency = new
                    {
                        coins = reader.GetInt64("coins")
                    }
                }
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<CatalogItemData>> getCurrentCataThemeItemsId(long id)
    {
        var result = new List<CatalogItemData>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT * FROM clothingcatalogthemeinvs WHERE scheduledthemechallengeid = @id;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new CatalogItemData
            {
                clothingCatalogItemId = reader.GetInt64("clothingcatalogitemid"),
                creatorName = reader.GetString("creatorname"),
                cost = reader.GetInt64("price"),
                numberSold = reader.GetInt64("numbersold"),
                equipment = new CustomEquipment
                {
                    definitionId = reader.GetInt32("definitionid"),
                    equipmentId = reader.GetInt64("equipmentid"),
                    parts = await ItemListPartsForCataClothThemes(reader.GetInt64("clothingcatalogitemid"))
                }
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<CatalogItemData>> getCurrentCataThemeItemsWithCategoryfromId(long id, string equequipmentcategory)
    {
        var result = new List<CatalogItemData>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT * FROM clothingcatalogthemeinvs WHERE scheduledthemechallengeid = @id AND equipmentcategory = @equipmentcategory;", conn);

        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@equipmentcategory", equequipmentcategory);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new CatalogItemData
            {
                clothingCatalogItemId = reader.GetInt64("clothingcatalogitemid"),
                creatorName = reader.GetString("creatorname"),
                cost = reader.GetInt64("price"),
                numberSold = reader.GetInt64("numbersold"),
                equipment = new CustomEquipment
                {
                    definitionId = reader.GetInt32("definitionid"),
                    equipmentId = reader.GetInt64("equipmentid"),
                    parts = await ItemListPartsForCataClothThemes(reader.GetInt64("clothingcatalogitemid"))
                }
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<CatalogItemData>> getCurrentCataThemeItemsIdInPopluar(long id)
    {
        var result = new List<CatalogItemData>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT * FROM clothingcatalogthemeinvs WHERE scheduledthemechallengeid = @id AND numbersold >= 1 ORDER BY numbersold;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new CatalogItemData
            {
                clothingCatalogItemId = reader.GetInt64("clothingcatalogitemid"),
                creatorName = reader.GetString("creatorname"),
                cost = reader.GetInt64("price"),
                numberSold = reader.GetInt64("numbersold"),
                equipment = new CustomEquipment
                {
                    definitionId = reader.GetInt32("definitionid"),
                    equipmentId = reader.GetInt64("equipmentid"),
                    parts = await ItemListPartsForCataClothThemes(reader.GetInt64("clothingcatalogitemid"))
                }
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<CatalogItemData?> getMostSolditem(long id)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"SELECT * FROM clothingcatalogthemeinvs WHERE scheduledthemechallengeid = @id AND numbersold >= 1 ORDER BY numbersold DESC LIMIT 1;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new CatalogItemData
            {
                clothingCatalogItemId = reader.GetInt64("clothingcatalogitemid"),
                creatorName = reader.GetString("creatorname"),
                cost = reader.GetInt64("price"),
                numberSold = reader.GetInt64("numbersold"),
                equipment = new CustomEquipment
                {
                    definitionId = reader.GetInt32("definitionid"),
                    equipmentId = reader.GetInt64("equipmentid"),
                    parts = await ItemListPartsForCataClothThemes(reader.GetInt64("clothingcatalogitemid"))
                }
            };
        }

        return null;
    }


    public static async Task<List<CustomEquipmentPart>> ItemListPartsForCataClothThemes(long itemId)
    {
        var result = new List<CustomEquipmentPart>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"
        SELECT slotindex 
        FROM clothingcatalogthemeinvcostus 
        WHERE clothingcatalogitemid = @id
        GROUP BY slotindex;", conn);

        cmd.Parameters.AddWithValue("@id", itemId);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var slotIndex = reader.GetInt32("slotindex");

            var part = new CustomEquipmentPart
            {
                slotIndex = slotIndex,
                customizations = await ItemListCostumForCataClothThemes(itemId, slotIndex)
            };

            result.Add(part);
        }

        return result;
    }
    public static async Task<List<CustomEquipmentCustomization>> ItemListCostumForCataClothThemes(long itemId, int slotIndex)
    {
        var result = new List<CustomEquipmentCustomization>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT clothingcatalogitemid, type, definitionid, slotindex, partindex, scale, rotation, repeatt, uoffset, voffset FROM clothingcatalogthemeinvcostus WHERE clothingcatalogitemid = @id;", conn);

        cmd.Parameters.AddWithValue("@id", itemId);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new CustomEquipmentCustomization
            {
                type = reader.GetInt32("type"),
                definitionId = reader.GetInt32("definitionid"),
                index = reader.GetInt32("partindex"),
                scale = float.Parse(reader.GetString("scale")),
                rotation = float.Parse(reader.GetString("rotation")),
                repeat = reader.GetBoolean("repeatt"),
                uoffset = float.Parse(reader.GetString("uoffset")),
                voffset = float.Parse(reader.GetString("voffset"))
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<FriendDB?> GetFriendsId(long Id)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(
            @"SELECT id, meID, hisID, username, hisusername, istrusted, isfriend, isrequested, isdeleted, timestamp FROM friends WHERE id = @id AND isrequested = 0;",
            conn);

        cmd.Parameters.AddWithValue("@id", Id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new FriendDB
            {
                Id = reader.GetInt64("id"),
                meID = reader.GetString("meID"),
                hisID = reader.GetString("hisID"),
                username = reader.GetString("username"),
                hisusername = reader.GetString("hisusername"),
                istrusted = reader.GetBoolean("istrusted"),
                isfriend = reader.GetBoolean("isfriend"),
                isrequested = reader.GetBoolean("isrequested"),
                isdeleted = reader.GetBoolean("isdeleted"),
                timestamp = reader.GetInt64("timestamp")
            };
        }

        return null;
    }

    public static async Task<FriendDB?> GetFriendsSwid(string swid, string otherswid)
    {
        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(
            @"SELECT id, meID, hisID, username, hisusername, istrusted, isfriend, isrequested, isdeleted, timestamp FROM friends WHERE meID = @swid AND hisID = @otherswid;",
            conn);

        cmd.Parameters.AddWithValue("@swid", swid);
        cmd.Parameters.AddWithValue("@otherswid", otherswid);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new FriendDB
            {
                Id = reader.GetInt64("id"),
                meID = reader.GetString("meID"),
                hisID = reader.GetString("hisID"),
                username = reader.GetString("username"),
                hisusername = reader.GetString("hisusername"),
                istrusted = reader.GetBoolean("istrusted"),
                isfriend = reader.GetBoolean("isfriend"),
                isrequested = reader.GetBoolean("isrequested"),
                isdeleted = reader.GetBoolean("isdeleted"),
                timestamp = reader.GetInt64("timestamp")
            };
        }

        return null;
    }

    public static async Task<ServerSettingsDB?> GetServerSettings()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT * FROM serversettings;", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new ServerSettingsDB
            {
                Sequence = reader.GetInt64("sequence"),
            };
        }

        return null;
    }

    public static async Task<List<Friendship>> getFriendships(string swid)
    {
        var result = new List<Friendship>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, meID, hisID, isrequested, istrusted, isdeleted, timestamp FROM friends WHERE meID = @swid AND isfriend = 1 AND isdeleted = 0;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new Friendship
            {
                FriendUserId = reader.GetString("hisID"),
                IsTrusted = reader.GetBoolean("istrusted")
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<FriendshipInvitation>> getFriendshipInvations(string swid)
    {
        var result = new List<FriendshipInvitation>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, meID, hisID, username, hisusername, isrequested, istrusted, isdeleted, timestamp FROM friends WHERE meID = @swid AND isfriend = 0 AND isdeleted = 0;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {

            var item = new FriendshipInvitation
            {
                FriendshipInvitationId = reader.GetInt64("id"),
                FriendDisplayName = reader.GetString("hisusername"),
                IsInviter = reader.GetBoolean("isrequested"),
                IsTrusted = reader.GetBoolean("istrusted")
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<Alert>> getAlerts(string swid)
    {
        var result = new List<Alert>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, swid, level, message, timestamp, hasseen FROM alerts WHERE swid = @swid AND hasseen = 0;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {

            var item = new Alert
            {
                AlertId = reader.GetInt64("id"),
                Level = reader.GetString("level"),
                Text = reader.GetString("message"),
                Timestamp = reader.GetInt64("timestamp")
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<AddAlertNotification>> getNotificationAlerts(string swid, string accesstoken)
    {
        var result = new List<AddAlertNotification>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, swid, level, message, timestamp, hasseen FROM alerts WHERE swid = @swid AND hasseen = 0;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedProfile = await GetProfilesSwid(reader.GetString("swid"));

            var getSrvSett = await GetServerSettings();

            var item = new AddAlertNotification
            {
                Alert = new Alert
                {
                    AlertId = reader.GetInt64("id"),
                    Level = reader.GetString("level"),
                    Text = reader.GetString("message"),
                    Timestamp = reader.GetInt64("timestamp")
                },
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence,
                SequenceNumber = getSrvSett.Sequence
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<ClearAlertNotification>> getNotificationAlertClears(string swid, string accesstoken)
    {
        var result = new List<ClearAlertNotification>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, swid, level, message, timestamp, hasseen FROM alerts WHERE swid = @swid AND hasseen = 1;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {

            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedProfile = await GetProfilesSwid(reader.GetString("swid"));

            var getSrvSett = await GetServerSettings();

            var item = new ClearAlertNotification
            {
                AlertIds = [reader.GetInt64("id")],
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence,
                SequenceNumber = getSrvSett.Sequence
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<AddFriendshipNotification>> getNotificationFriendships(string swid, string aesSecret, string accesstoken)
    {
        var result = new List<AddFriendshipNotification>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, meID, hisID, username, hisusername, isrequested, istrusted, isdeleted, timestamp FROM friends WHERE meID = @swid AND isfriend = 1 AND isdeleted = 0;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {

            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedProfile = await GetProfilesSwid(reader.GetString("hisID"));
            var getSrvSett = await GetServerSettings();

            var item = new AddFriendshipNotification
            {
                Friend = new Mix.User
                {
                    UserId = loadedProfile.Swid,
                    HashedUserId = DumbfoxEncryption.EncryptB64(loadedProfile.Swid, Convert.FromBase64String(aesSecret)),
                    DisplayName = reader.GetString("hisusername"),
                    FirstName = reader.GetString("hisusername"),
                    Avatar = new Avatar
                    {
                        Accessory = new AvatarProperty { SelectionKey = loadedProfile.Accessory, TintIndex = loadedProfile.AccessoryTintIndex, XOffset = loadedProfile.AccessoryXOffeset, YOffset = loadedProfile.AccessoryYOffeset },
                        Costume = new AvatarProperty { SelectionKey = loadedProfile.Costume, TintIndex = loadedProfile.CostumeTintIndex, XOffset = loadedProfile.CostumeXOffeset, YOffset = loadedProfile.CostumeYOffeset },
                        Brow = new AvatarProperty { SelectionKey = loadedProfile.Brow, TintIndex = loadedProfile.BrowTintIndex, XOffset = loadedProfile.BrowXOffeset, YOffset = loadedProfile.BrowYOffeset },
                        Eyes = new AvatarProperty { SelectionKey = loadedProfile.Eyes, TintIndex = loadedProfile.EyesTintIndex, XOffset = loadedProfile.EyesXOffeset, YOffset = loadedProfile.EyesYOffeset },
                        Hair = new AvatarProperty { SelectionKey = loadedProfile.Hair, TintIndex = loadedProfile.HairTintIndex, XOffset = loadedProfile.HairXOffeset, YOffset = loadedProfile.HairYOffeset },
                        Nose = new AvatarProperty { SelectionKey = loadedProfile.Nose, TintIndex = loadedProfile.NoseTintIndex, XOffset = loadedProfile.NoseXOffeset, YOffset = loadedProfile.NoseYOffeset },
                        Mouth = new AvatarProperty { SelectionKey = loadedProfile.Mouth, TintIndex = loadedProfile.MouthTintIndex, XOffset = loadedProfile.MouthXOffeset, YOffset = loadedProfile.MouthYOffeset },
                        Skin = new AvatarProperty { SelectionKey = loadedProfile.Skin, TintIndex = loadedProfile.SkinTintIndex, XOffset = loadedProfile.SkinXOffeset, YOffset = loadedProfile.SkinYOffeset },
                        Hat = new AvatarProperty { SelectionKey = loadedProfile.Hat, TintIndex = loadedProfile.HatTintIndex, XOffset = loadedProfile.HatXOffeset, YOffset = loadedProfile.HatYOffeset }
                    }
                },

                FriendshipInvitationId = reader.GetInt64("id"),
                IsTrusted = reader.GetBoolean("istrusted"),
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence,
                SequenceNumber = getSrvSett.Sequence
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<RemoveFriendshipNotification>> getNotificationFriendshipRemoves(string swid, string aesSecret, string accesstoken)
    {
        var result = new List<RemoveFriendshipNotification>();

        var ids = new List<long>();

        await using var conn = await OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT id, meID, hisID, timestamp 
        FROM friends 
        WHERE meID = @swid 
        AND isfriend = 1 
        AND isdeleted = 1
        FOR UPDATE;", conn, transaction);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {

            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var getSrvSett = await GetServerSettings();

            var id = reader.GetInt64("id");
            ids.Add(id);

            result.Add(new RemoveFriendshipNotification
            {
                FriendUserId = reader.GetString("hisID"),
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence,
                SequenceNumber = getSrvSett.Sequence
            });
        }

        await reader.CloseAsync();

        if (ids.Count > 0)
        {
            var deleteCmd = new MySqlCommand(
                $"DELETE FROM friends WHERE id IN ({string.Join(",", ids)})",
                conn, transaction);

            await deleteCmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();

        return result;
    }

    public static async Task<List<Mix.User>> getFriendshipUsers(string swid, string aesSecret)
    {
        var result = new List<Mix.User>();

        await using var conn = await OpenConnectionAsync();

        var loadedAccountSwid = await GetAccountSwid(swid);
        var loadedProfile = await GetProfilesSwid(swid);

        var me = new Mix.User
        {
            UserId = loadedProfile.Swid,
            HashedUserId = DumbfoxEncryption.EncryptB64(loadedProfile.Swid, Convert.FromBase64String(aesSecret)),
            DisplayName = (string)loadedAccountSwid["username"],
            FirstName = (string)loadedAccountSwid["username"],
            Avatar = new Avatar
            {
                Accessory = new AvatarProperty { SelectionKey = loadedProfile.Accessory, TintIndex = loadedProfile.AccessoryTintIndex, XOffset = loadedProfile.AccessoryXOffeset, YOffset = loadedProfile.AccessoryYOffeset },
                Costume = new AvatarProperty { SelectionKey = loadedProfile.Costume, TintIndex = loadedProfile.CostumeTintIndex, XOffset = loadedProfile.CostumeXOffeset, YOffset = loadedProfile.CostumeYOffeset },
                Brow = new AvatarProperty { SelectionKey = loadedProfile.Brow, TintIndex = loadedProfile.BrowTintIndex, XOffset = loadedProfile.BrowXOffeset, YOffset = loadedProfile.BrowYOffeset },
                Eyes = new AvatarProperty { SelectionKey = loadedProfile.Eyes, TintIndex = loadedProfile.EyesTintIndex, XOffset = loadedProfile.EyesXOffeset, YOffset = loadedProfile.EyesYOffeset },
                Hair = new AvatarProperty { SelectionKey = loadedProfile.Hair, TintIndex = loadedProfile.HairTintIndex, XOffset = loadedProfile.HairXOffeset, YOffset = loadedProfile.HairYOffeset },
                Nose = new AvatarProperty { SelectionKey = loadedProfile.Nose, TintIndex = loadedProfile.NoseTintIndex, XOffset = loadedProfile.NoseXOffeset, YOffset = loadedProfile.NoseYOffeset },
                Mouth = new AvatarProperty { SelectionKey = loadedProfile.Mouth, TintIndex = loadedProfile.MouthTintIndex, XOffset = loadedProfile.MouthXOffeset, YOffset = loadedProfile.MouthYOffeset },
                Skin = new AvatarProperty { SelectionKey = loadedProfile.Skin, TintIndex = loadedProfile.SkinTintIndex, XOffset = loadedProfile.SkinXOffeset, YOffset = loadedProfile.SkinYOffeset },
                Hat = new AvatarProperty { SelectionKey = loadedProfile.Hat, TintIndex = loadedProfile.HatTintIndex, XOffset = loadedProfile.HatXOffeset, YOffset = loadedProfile.HatYOffeset }
            }
        };

        result.Add(me);


        await using var cmd = new MySqlCommand(@"SELECT id, meID, hisID, username, hisusername, isrequested, istrusted, isdeleted, timestamp FROM friends WHERE meID = @swid AND isfriend = 1 AND isdeleted = 0;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var loadedotherProfile = await GetProfilesSwid(reader.GetString("hisID"));

            var item = new Mix.User
            {
                UserId = loadedotherProfile.Swid,
                HashedUserId = DumbfoxEncryption.EncryptB64(loadedotherProfile.Swid, Convert.FromBase64String(aesSecret)),
                DisplayName = reader.GetString("hisusername"),
                FirstName = reader.GetString("hisusername"),
                Avatar = new Avatar
                {
                    Accessory = new AvatarProperty { SelectionKey = loadedotherProfile.Accessory, TintIndex = loadedotherProfile.AccessoryTintIndex, XOffset = loadedotherProfile.AccessoryXOffeset, YOffset = loadedotherProfile.AccessoryYOffeset },
                    Costume = new AvatarProperty { SelectionKey = loadedotherProfile.Costume, TintIndex = loadedotherProfile.CostumeTintIndex, XOffset = loadedotherProfile.CostumeXOffeset, YOffset = loadedotherProfile.CostumeYOffeset },
                    Brow = new AvatarProperty { SelectionKey = loadedotherProfile.Brow, TintIndex = loadedotherProfile.BrowTintIndex, XOffset = loadedotherProfile.BrowXOffeset, YOffset = loadedotherProfile.BrowYOffeset },
                    Eyes = new AvatarProperty { SelectionKey = loadedotherProfile.Eyes, TintIndex = loadedotherProfile.EyesTintIndex, XOffset = loadedotherProfile.EyesXOffeset, YOffset = loadedotherProfile.EyesYOffeset },
                    Hair = new AvatarProperty { SelectionKey = loadedotherProfile.Hair, TintIndex = loadedotherProfile.HairTintIndex, XOffset = loadedotherProfile.HairXOffeset, YOffset = loadedotherProfile.HairYOffeset },
                    Nose = new AvatarProperty { SelectionKey = loadedotherProfile.Nose, TintIndex = loadedotherProfile.NoseTintIndex, XOffset = loadedotherProfile.NoseXOffeset, YOffset = loadedotherProfile.NoseYOffeset },
                    Mouth = new AvatarProperty { SelectionKey = loadedotherProfile.Mouth, TintIndex = loadedotherProfile.MouthTintIndex, XOffset = loadedotherProfile.MouthXOffeset, YOffset = loadedotherProfile.MouthYOffeset },
                    Skin = new AvatarProperty { SelectionKey = loadedotherProfile.Skin, TintIndex = loadedotherProfile.SkinTintIndex, XOffset = loadedotherProfile.SkinXOffeset, YOffset = loadedotherProfile.SkinYOffeset },
                    Hat = new AvatarProperty { SelectionKey = loadedotherProfile.Hat, TintIndex = loadedotherProfile.HatTintIndex, XOffset = loadedotherProfile.HatXOffeset, YOffset = loadedotherProfile.HatYOffeset }
                }
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<AddFriendshipInvitationNotification>> getNotificationFriendshipInvations(string swid, string aesSecret, string accesstoken)
    {
        var result = new List<AddFriendshipInvitationNotification>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, meID, hisID, username, hisusername, isrequested, istrusted, isdeleted, timestamp FROM friends WHERE meID = @swid AND isfriend = 0 AND isdeleted = 0;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedProfile = await GetProfilesSwid(reader.GetString("hisID"));
            var getSrvSett = await GetServerSettings();

            var item = new AddFriendshipInvitationNotification
            {

                Invitation = new FriendshipInvitation
                {
                    FriendshipInvitationId = reader.GetInt64("id"),
                    FriendDisplayName = reader.GetString("hisusername"),
                    IsInviter = reader.GetBoolean("isrequested"),
                    IsTrusted = reader.GetBoolean("istrusted"),
                },
                Friend = new Mix.User
                {
                    UserId = loadedProfile.Swid,
                    HashedUserId = DumbfoxEncryption.EncryptB64(loadedProfile.Swid, Convert.FromBase64String(aesSecret)),
                    DisplayName = reader.GetString("hisusername"),
                    FirstName = reader.GetString("hisusername"),
                    Avatar = new Avatar
                    {
                        Accessory = new AvatarProperty { SelectionKey = loadedProfile.Accessory, TintIndex = loadedProfile.AccessoryTintIndex, XOffset = loadedProfile.AccessoryXOffeset, YOffset = loadedProfile.AccessoryYOffeset },
                        Costume = new AvatarProperty { SelectionKey = loadedProfile.Costume, TintIndex = loadedProfile.CostumeTintIndex, XOffset = loadedProfile.CostumeXOffeset, YOffset = loadedProfile.CostumeYOffeset },
                        Brow = new AvatarProperty { SelectionKey = loadedProfile.Brow, TintIndex = loadedProfile.BrowTintIndex, XOffset = loadedProfile.BrowXOffeset, YOffset = loadedProfile.BrowYOffeset },
                        Eyes = new AvatarProperty { SelectionKey = loadedProfile.Eyes, TintIndex = loadedProfile.EyesTintIndex, XOffset = loadedProfile.EyesXOffeset, YOffset = loadedProfile.EyesYOffeset },
                        Hair = new AvatarProperty { SelectionKey = loadedProfile.Hair, TintIndex = loadedProfile.HairTintIndex, XOffset = loadedProfile.HairXOffeset, YOffset = loadedProfile.HairYOffeset },
                        Nose = new AvatarProperty { SelectionKey = loadedProfile.Nose, TintIndex = loadedProfile.NoseTintIndex, XOffset = loadedProfile.NoseXOffeset, YOffset = loadedProfile.NoseYOffeset },
                        Mouth = new AvatarProperty { SelectionKey = loadedProfile.Mouth, TintIndex = loadedProfile.MouthTintIndex, XOffset = loadedProfile.MouthXOffeset, YOffset = loadedProfile.MouthYOffeset },
                        Skin = new AvatarProperty { SelectionKey = loadedProfile.Skin, TintIndex = loadedProfile.SkinTintIndex, XOffset = loadedProfile.SkinXOffeset, YOffset = loadedProfile.SkinYOffeset },
                        Hat = new AvatarProperty { SelectionKey = loadedProfile.Hat, TintIndex = loadedProfile.HatTintIndex, XOffset = loadedProfile.HatXOffeset, YOffset = loadedProfile.HatYOffeset }
                    }
                },
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence,
                SequenceNumber = getSrvSett.Sequence
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<RemoveFriendshipInvitationNotification>> getNotificationFriendshipInvationRemoves(string swid, string aesSecret, string accesstoken)
    {
        var result = new List<RemoveFriendshipInvitationNotification>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, meID, hisID, username, hisusername, isrequested, istrusted, isdeleted, timestamp FROM friends WHERE meID = @swid AND isfriend = 0 AND isdeleted = 1;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {

            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var getSrvSett = await GetServerSettings();

            var item = new RemoveFriendshipInvitationNotification
            {
                InvitationId = reader.GetInt64("id"),
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence,
                SequenceNumber = getSrvSett.Sequence
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<CustomEquipmentPart>> ItemListPartsList1(long itemId)
    {
        var result = new List<CustomEquipmentPart>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"
        SELECT slotindex 
        FROM inventorycostumes 
        WHERE id = @id
        GROUP BY slotindex;", conn);

        cmd.Parameters.AddWithValue("@id", itemId);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var slotIndex = reader.GetInt32("slotindex");

            var part = new CustomEquipmentPart
            {
                slotIndex = slotIndex,
                customizations = await ItemListPartsList(itemId, slotIndex)
            };

            result.Add(part);
        }

        return result;
    }

    public static async Task<List<CustomEquipmentCustomization>> ItemListPartsList(long itemId, int slotIndex)
    {
        var result = new List<CustomEquipmentCustomization>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, type, definitionid, slotindex, partindex, scale, rotation, repeatt, uoffset, voffset FROM inventorycostumes WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("@id", itemId);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new CustomEquipmentCustomization
            {
                type = reader.GetInt32("type"),
                definitionId = reader.GetInt32("definitionid"),
                index = reader.GetInt32("partindex"),
                scale = float.Parse(reader.GetString("scale")),
                rotation = float.Parse(reader.GetString("rotation")),
                repeat = reader.GetBoolean("repeatt"),
                uoffset = float.Parse(reader.GetString("uoffset")),
                voffset = float.Parse(reader.GetString("voffset"))
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<CustomEquipment>> getSwidInventory(string swid)
    {
        var result = new List<CustomEquipment>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, sourceid, definitionid, source, datetimecreated FROM inventorys WHERE swid = @swid;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new CustomEquipment
            {
                definitionId = reader.GetInt32("definitionid"),
                equipmentId = reader.GetInt64("id"),
                dateTimeCreated = reader.GetInt64("datetimecreated"),
                source = reader.GetString("source"),
                sourceId = reader.GetInt32("sourceid"),
                parts = await ItemListPartsList1(reader.GetInt64("id"))
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<CustomEquipment>> getCurrentOutfit(string swid)
    {
        var result = new List<CustomEquipment>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT id, sourceid, definitionid, source, datetimecreated FROM inventorys WHERE swid = @swid AND iswearing = 1;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item = new CustomEquipment
            {
                definitionId = reader.GetInt32("definitionid"),
                equipmentId = reader.GetInt64("id"),
                dateTimeCreated = reader.GetInt64("datetimecreated"),
                source = reader.GetString("source"),
                sourceId = reader.GetInt32("sourceid"),
                parts = await ItemListPartsList1(reader.GetInt64("id"))
            };

            result.Add(item);
        }

        return result;
    }


    //ChatThread

    public static async Task<ChatThread> InsertChatThread(string ChatThreadType, List<string> MemberUserIds, string swid, string aesSecret)
    {
        await using var conn = await OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            long threadId;

            await using (var cmd = new MySqlCommand(@"
            INSERT INTO chatthreads (type, timestamp)
            VALUES (@type, @timestamp);", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@type", ChatThreadType);
                cmd.Parameters.AddWithValue("@timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                await cmd.ExecuteNonQueryAsync();
                threadId = cmd.LastInsertedId;
            }

            var otherMembers = MemberUserIds
                .Where(x => x != swid)
                .Distinct()
                .ToList();

            var ownerInfo = await GetAccountSwid(swid);

            await using (var cmd = new MySqlCommand(@"
            INSERT INTO chatthreadsusers (id, swid, displayname, isowner)
            VALUES (@id, @swid, @displayname, @isowner);", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@id", threadId);
                cmd.Parameters.AddWithValue("@swid", swid);
                cmd.Parameters.AddWithValue("@displayname", ownerInfo["username"]?.ToString());
                cmd.Parameters.AddWithValue("@isowner", 1);

                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var memberSwid in otherMembers)
            {
                var accountInfo = await GetAccountSwid(memberSwid);

                await using var cmd = new MySqlCommand(@"
                INSERT INTO chatthreadsusers (id, swid, displayname, isowner)
                VALUES (@id, @swid, @displayname, @isowner);", conn, transaction);

                cmd.Parameters.AddWithValue("@id", threadId);
                cmd.Parameters.AddWithValue("@swid", memberSwid);
                cmd.Parameters.AddWithValue("@displayname", accountInfo["username"]?.ToString());
                cmd.Parameters.AddWithValue("@isowner", 0);

                await cmd.ExecuteNonQueryAsync();
            }



            ChatThread item;

            await using (var cmd2 = new MySqlCommand(@"
            SELECT *
            FROM chatthreads
            WHERE id = @id;", conn, transaction))
            {
                cmd2.Parameters.AddWithValue("@id", threadId);

                await using var reader = await cmd2.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    item = new ChatThread
                    {
                        ChatThreadId = reader.GetInt64("id"),
                        ChatThreadType = reader.GetString("type"),
                        IsTrusted = reader.GetBoolean("istrusted"),
                        Members = await getChatThreadsUsers(threadId, aesSecret)
                    };
                }
                else
                {
                    throw new Exception("Thread not found after insert.");
                }
            }

            await transaction.CommitAsync();
            return item;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static async Task<TextChatMessage> InsertChatThreadTextMessage(long id, long sessionid, string swid, string username, string message)
    {
        await using var conn = await OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            await using (var cmd = new MySqlCommand(@"
            INSERT INTO chatthreadchats (id, sessionid, timestamp, swid, nickname, text, istext)
            SELECT @id, @sessionid, @timestamp, @swid, @nickname, @text, 1
            FROM chatthreads ct
            INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
            WHERE ct.id = @id
              AND ctu.swid = @swid
            LIMIT 1;", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@sessionid", sessionid);
                cmd.Parameters.AddWithValue("@timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                cmd.Parameters.AddWithValue("@swid", swid);
                cmd.Parameters.AddWithValue("@nickname", username);
                cmd.Parameters.AddWithValue("@text", message);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    throw new InvalidOperationException("User not in thread or thread does not exist.");
            }

            TextChatMessage item;

            await using (var cmd2 = new MySqlCommand(@"
            SELECT ct.*,
            ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
            FROM chatthreadchats ct
            WHERE ct.id = @id
              AND ct.swid = @swid
              AND ct.istext = 1
            ORDER BY ct.timestamp DESC
            LIMIT 1;", conn, transaction))
            {
                cmd2.Parameters.AddWithValue("@id", id);
                cmd2.Parameters.AddWithValue("@swid", swid);

                await using var reader = await cmd2.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    throw new InvalidOperationException("Inserted message not found.");

                item = new TextChatMessage
                {
                    Text = reader.GetString("text"),
                    IsModerated = reader.GetBoolean("ismoderated"),
                    Created = reader.GetInt64("timestamp"),
                    ChatMessageId = reader.GetInt64("msgid"),
                    ClientChatMessageId = reader.GetInt64("sessionid"),
                    ChatThreadId = reader.GetInt64("id"),
                    SenderUserId = reader.GetString("swid"),
                    SequenceNumber = reader.GetInt64("msgid"),
                    MessageType = "TEXT"
                };
            }

            await transaction.CommitAsync();
            return item;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static T MapMessage<T>(DbDataReader reader, ChatMessageTypes type)
    where T : BaseChatMessage, new()
    {
        if (typeof(T) == typeof(TextChatMessage))
        {
            return (T)(BaseChatMessage)new TextChatMessage
            {
                Text = reader.GetString("text"),
                IsModerated = reader.GetBoolean("ismoderated"),
                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ChatThreadId = reader.GetInt64("id"),
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = type.ToString()
            };
        }
        else if (typeof(T) == typeof(StickerChatMessage))
        {
            return (T)(BaseChatMessage)new StickerChatMessage
            {
                ContentId = reader.GetString("contentid"),

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ChatThreadId = reader.GetInt64("id"),
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = type.ToString()
            };
        }
        else if (typeof(T) == typeof(GagChatMessage))
        {
            return (T)(BaseChatMessage)new GagChatMessage
            {
                ContentId = reader.GetString("contentid"),

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ChatThreadId = reader.GetInt64("id"),
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = type.ToString()
            };
        }
        else if (typeof(T) == typeof(VideoChatMessage))
        {
            return (T)(BaseChatMessage)new VideoChatMessage
            {
                UgcMediaId = reader.GetString("videoflavorid"),
                VideoId = reader.GetString("videoflavorid"),
                Caption = "Hello World Video",
                Duration = 0,
                Thumbnail = new PhotoFlavor
                {
                    PhotoFlavorId = reader.GetString("photoflavorid"),
                    Width = 140,
                    Height = 90,
                    Encoding = "PNG",
                    Url = "http://127.0.0.1/test.png"
                },
                VideoFlavors = new List<VideoFlavor>
                {
                    new VideoFlavor
                    {
                        VideoFlavorId = reader.GetString("videoflavorid"),
                        Width = 140,
                        Height = 90,
                        Format = "MP4",
                        Bitrate = 300
                    }
                },

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ChatThreadId = reader.GetInt64("id"),
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = type.ToString()
            };
        }
        else if (typeof(T) == typeof(PhotoChatMessage))
        {
            return (T)(BaseChatMessage)new PhotoChatMessage
            {
                UgcMediaId = reader.GetString("photoflavorid"),
                PhotoId = reader.GetString("photoflavorid"),
                Caption = "Hello World Picture",
                PhotoFlavors = new List<PhotoFlavor>
                {
                    new PhotoFlavor
                    {
                        PhotoFlavorId = reader.GetString("photoflavorid"),
                        Width = 140,
                        Height = 90,
                        Encoding = "PNG",
                        Url = "http://127.0.0.1/test.png"
                    }
                },

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ChatThreadId = reader.GetInt64("id"),
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = type.ToString()
            };
        }
        else if (typeof(T) == typeof(GameStateChatMessage))
        {
            return (T)(BaseChatMessage)new GameStateChatMessage
            {
                GameName = reader.GetString("gamename"),
                State = reader.GetString("gamestate"),

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ChatThreadId = reader.GetInt64("id"),
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = type.ToString()
            };
        }
        else if (typeof(T) == typeof(GameEventChatMessage))
        {
            return (T)(BaseChatMessage)new GameEventChatMessage
            {
                GameStateMessageId = reader.GetInt64("msgid"),
                GameName = reader.GetString("gamename"),

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ChatThreadId = reader.GetInt64("id"),
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = type.ToString()
            };
        }

        throw new NotSupportedException($"Unsupported type {typeof(T)}");
    }

    public static async Task<T> InsertChatThreadTypeMessage<T>(long id, long sessionid, string swid, string username, string message, string dbnamea, string dbname, ChatMessageTypes type) where T : BaseChatMessage, new()
    {
        await using var conn = await OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            await using (var cmd = new MySqlCommand($@"
            INSERT INTO chatthreadchats (id, sessionid, timestamp, swid, nickname, {dbnamea}, {dbname})
            SELECT @id, @sessionid, @timestamp, @swid, @nickname, @text, 1
            FROM chatthreads ct
            INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
            WHERE ct.id = @id
              AND ctu.swid = @swid
            LIMIT 1;", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@sessionid", sessionid);
                cmd.Parameters.AddWithValue("@timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                cmd.Parameters.AddWithValue("@swid", swid);
                cmd.Parameters.AddWithValue("@nickname", username);
                cmd.Parameters.AddWithValue("@text", message);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    throw new InvalidOperationException("User not in thread or thread does not exist.");
            }

            T item;

            await using (var cmd2 = new MySqlCommand($@"
            SELECT ct.*,
            ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
            FROM chatthreadchats ct
            WHERE ct.id = @id
              AND ct.swid = @swid
              AND ct.{dbname} = 1
            ORDER BY ct.timestamp DESC
            LIMIT 1;", conn, transaction))
            {
                cmd2.Parameters.AddWithValue("@id", id);
                cmd2.Parameters.AddWithValue("@swid", swid);

                await using var reader = await cmd2.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    throw new InvalidOperationException("Inserted message not found.");

                item = MapMessage<T>(reader, type);
            }

            await transaction.CommitAsync();
            return item;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static async Task<List<Mix.User>> getChatThreadsUsers(long id, string aesSecret)
    {
        var result = new List<Mix.User>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"SELECT * FROM chatthreadsusers WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var loadedProfile = await GetProfilesSwid(reader.GetString("swid"));

            var item = new Mix.User
            {
                UserId = loadedProfile.Swid,
                HashedUserId = DumbfoxEncryption.EncryptB64(loadedProfile.Swid, Convert.FromBase64String(aesSecret)),
                DisplayName = reader.GetString("displayname"),
                FirstName = reader.GetString("displayname"),
                Avatar = new Avatar
                {
                    Accessory = new AvatarProperty { SelectionKey = loadedProfile.Accessory, TintIndex = loadedProfile.AccessoryTintIndex, XOffset = loadedProfile.AccessoryXOffeset, YOffset = loadedProfile.AccessoryYOffeset },
                    Costume = new AvatarProperty { SelectionKey = loadedProfile.Costume, TintIndex = loadedProfile.CostumeTintIndex, XOffset = loadedProfile.CostumeXOffeset, YOffset = loadedProfile.CostumeYOffeset },
                    Brow = new AvatarProperty { SelectionKey = loadedProfile.Brow, TintIndex = loadedProfile.BrowTintIndex, XOffset = loadedProfile.BrowXOffeset, YOffset = loadedProfile.BrowYOffeset },
                    Eyes = new AvatarProperty { SelectionKey = loadedProfile.Eyes, TintIndex = loadedProfile.EyesTintIndex, XOffset = loadedProfile.EyesXOffeset, YOffset = loadedProfile.EyesYOffeset },
                    Hair = new AvatarProperty { SelectionKey = loadedProfile.Hair, TintIndex = loadedProfile.HairTintIndex, XOffset = loadedProfile.HairXOffeset, YOffset = loadedProfile.HairYOffeset },
                    Nose = new AvatarProperty { SelectionKey = loadedProfile.Nose, TintIndex = loadedProfile.NoseTintIndex, XOffset = loadedProfile.NoseXOffeset, YOffset = loadedProfile.NoseYOffeset },
                    Mouth = new AvatarProperty { SelectionKey = loadedProfile.Mouth, TintIndex = loadedProfile.MouthTintIndex, XOffset = loadedProfile.MouthXOffeset, YOffset = loadedProfile.MouthYOffeset },
                    Skin = new AvatarProperty { SelectionKey = loadedProfile.Skin, TintIndex = loadedProfile.SkinTintIndex, XOffset = loadedProfile.SkinXOffeset, YOffset = loadedProfile.SkinYOffeset },
                    Hat = new AvatarProperty { SelectionKey = loadedProfile.Hat, TintIndex = loadedProfile.HatTintIndex, XOffset = loadedProfile.HatXOffeset, YOffset = loadedProfile.HatYOffeset }
                }
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<ChatThread>> getChatThreads(string swid, string aesSecret)
    {
        var result = new List<ChatThread>();

        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT ct.*
        FROM chatthreads ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var threadId = reader.GetInt64("id");

            var item = new ChatThread
            {
                ChatThreadId = threadId,
                ChatThreadType = reader.GetString("type"),
                IsTrusted = reader.GetBoolean("istrusted"),
                Members = await getChatThreadsUsers(threadId, aesSecret)
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<long>> GetChatThreadMsgIds(string swid)
    {
        var result = new List<long>();

        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.`timestamp`) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var msgId = reader.GetInt64("msgid");
            result.Add(msgId);
        }

        return result;
    }



    public static async Task<List<AddChatThreadNotification>> getChatThreadNotifications(string swid, string aesSecret, string accesstoken)
    {
        var result = new List<AddChatThreadNotification>();

        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT ct.*
        FROM chatthreads ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var threadId = reader.GetInt64("id");

            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedprofile = await GetProfilesSwid((string)session["swid"]);
            if (loadedprofile == null)
            {
                throw new Exception("From Given Swid from Profile not found");
            }

            var getSrvSett = await GetServerSettings();

            var item = new AddChatThreadNotification
            {
                ChatThread = new ChatThread
                {
                    ChatThreadId = threadId,
                    ChatThreadType = reader.GetString("type"),
                    IsTrusted = reader.GetBoolean("istrusted"),
                    Members = await getChatThreadsUsers(threadId, aesSecret)
                },
                WelcomingUserId = swid,
                SequenceNumber = getSrvSett.Sequence,
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence
            };

            result.Add(item);
        }

        return result;
    }


    public static async Task<List<TextChatMessage>> getChatThreadTexts(long id, string swid)
    {
        var result = new List<TextChatMessage>();

        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.`timestamp`) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.id = @id
        AND ct.istext = 1;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var threadId = reader.GetInt64("id");

            var item = new TextChatMessage
            {
                Text = reader.GetString("text"),
                IsModerated = reader.GetBoolean("ismoderated"),
                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ClientChatMessageId = reader.GetInt64("sessionid"),
                ChatThreadId = threadId,
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = "TEXT"
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<StickerChatMessage>> getChatThreadStickers(long id, string swid)
    {
        var result = new List<StickerChatMessage>();

        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.id = @id
        AND ct.isticker = 1;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var threadId = reader.GetInt64("id");

            var item = new StickerChatMessage
            {
                ContentId = reader.GetString("contentid"),

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ClientChatMessageId = reader.GetInt64("sessionid"),
                ChatThreadId = threadId,
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = "STICKER"
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<GagChatMessage>> getChatThreadGags(long id, string swid)
    {
        var result = new List<GagChatMessage>();

        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.id = @id
        AND ct.isgag = 1;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var threadId = reader.GetInt64("id");

            var item = new GagChatMessage
            {
                ContentId = reader.GetString("contentid"),

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ClientChatMessageId = reader.GetInt64("sessionid"),
                ChatThreadId = threadId,
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = "GAG"
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<VideoChatMessage>> getChatThreadVideos(long id, string swid)
    {
        var result = new List<VideoChatMessage>();

        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.id = @id
        AND ct.isvideo = 1;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var threadId = reader.GetInt64("id");

            var item = new VideoChatMessage
            {
                UgcMediaId = reader.GetString("videoflavorid"),
                VideoId = reader.GetString("videoflavorid"),
                Caption = "Hello World Video",
                Duration = 0,
                Thumbnail = new PhotoFlavor
                {
                    PhotoFlavorId = reader.GetString("photoflavorid"),
                    Width = 140,
                    Height = 90,
                    Encoding = "PNG",
                    Url = "http://127.0.0.1/test.png"
                },
                VideoFlavors = new List<VideoFlavor>
                {
                    new VideoFlavor
                    {
                        VideoFlavorId = reader.GetString("videoflavorid"),
                        Width = 140,
                        Height = 90,
                        Format = "MP4",
                        Bitrate = 300
                    }
                },

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ClientChatMessageId = reader.GetInt64("sessionid"),
                ChatThreadId = threadId,
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = "VIDEO"
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<PhotoChatMessage>> getChatThreadPhotos(long id, string swid)
    {
        var result = new List<PhotoChatMessage>();

        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.id = @id
        AND ct.isphoto = 1;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var threadId = reader.GetInt64("id");

            var item = new PhotoChatMessage
            {
                UgcMediaId = reader.GetString("photoflavorid"),
                PhotoId = reader.GetString("photoflavorid"),
                Caption = "Hello World Picture",
                PhotoFlavors = new List<PhotoFlavor>
                {
                    new PhotoFlavor
                    {
                        PhotoFlavorId = reader.GetString("photoflavorid"),
                        Width = 140,
                        Height = 90,
                        Encoding = "PNG",
                        Url = "http://127.0.0.1/test.png"
                    }
                },

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ClientChatMessageId = reader.GetInt64("sessionid"),
                ChatThreadId = threadId,
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = "PHOTO"
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<GameStateChatMessage>> getChatThreadGameStates(long id, string swid)
    {
        var result = new List<GameStateChatMessage>();

        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.id = @id
        AND ct.isgame = 1;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var threadId = reader.GetInt64("id");

            var item = new GameStateChatMessage
            {
                GameName = reader.GetString("gamename"),
                State = reader.GetString("gamestate"),

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ClientChatMessageId = reader.GetInt64("sessionid"),
                ChatThreadId = threadId,
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = "GAME_STATE"
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<GameEventChatMessage>> getChatThreadGameEvents(long id, string swid)
    {
        var result = new List<GameEventChatMessage>();

        await using var conn = await OpenConnectionAsync();

        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.id = @id
        AND ct.isgameevent = 1;", conn);

        cmd.Parameters.AddWithValue("@id", id);

        cmd.Parameters.AddWithValue("@swid", swid);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var threadId = reader.GetInt64("id");

            var item = new GameEventChatMessage
            {
                GameStateMessageId = reader.GetInt64("msgid"),
                GameName = reader.GetString("gamename"),

                Created = reader.GetInt64("timestamp"),
                ChatMessageId = reader.GetInt64("msgid"),
                ClientChatMessageId = reader.GetInt64("sessionid"),
                ChatThreadId = threadId,
                SenderUserId = reader.GetString("swid"),
                SequenceNumber = reader.GetInt64("msgid"),
                MessageType = "GAME_EVENT"
            };

            result.Add(item);
        }

        return result;
    }

    public static async Task<List<AddChatThreadTextMessageNotification>> getChatThreadTextNotifications(string swid, string accesstoken)
    {
        var result = new List<AddChatThreadTextMessageNotification>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.`timestamp`) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.istext = 1;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedprofile = await GetProfilesSwid((string)session["swid"]);
            if (loadedprofile == null)
            {
                throw new Exception("From Given Swid from Profile not found");
            }

            var getSrvSett = await GetServerSettings();

            var threadId = reader.GetInt64("id");
            var item = new AddChatThreadTextMessageNotification
            {
                Message = new TextChatMessage
                {
                    Text = reader.GetString("text"),
                    IsModerated = reader.GetBoolean("ismoderated"),
                    Created = reader.GetInt64("timestamp"),
                    ChatMessageId = reader.GetInt64("msgid"),
                    ClientChatMessageId = reader.GetInt64("sessionid"),
                    ChatThreadId = threadId,
                    SenderUserId = reader.GetString("swid"),
                    SequenceNumber = reader.GetInt64("msgid"),
                    MessageType = "TEXT"

                },
                SequenceNumber = getSrvSett.Sequence,
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence
            };

            result.Add(item);
        }
        return result;
    }

    public static async Task<List<AddChatThreadStickerMessageNotification>> getChatThreadStickerNotifications(string swid, string accesstoken)
    {
        var result = new List<AddChatThreadStickerMessageNotification>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.isticker = 1;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedprofile = await GetProfilesSwid((string)session["swid"]);
            if (loadedprofile == null)
            {
                throw new Exception("From Given Swid from Profile not found");
            }

            var getSrvSett = await GetServerSettings();

            var threadId = reader.GetInt64("id");
            var item = new AddChatThreadStickerMessageNotification
            {
                Message = new StickerChatMessage
                {
                    ContentId = reader.GetString("contentid"),

                    Created = reader.GetInt64("timestamp"),
                    ChatMessageId = reader.GetInt64("msgid"),
                    ClientChatMessageId = reader.GetInt64("sessionid"),
                    ChatThreadId = threadId,
                    SenderUserId = reader.GetString("swid"),
                    SequenceNumber = reader.GetInt64("msgid"),
                    MessageType = "STICKER"

                },
                SequenceNumber = getSrvSett.Sequence,
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence
            };

            result.Add(item);
        }
        return result;
    }

    public static async Task<List<AddChatThreadGagMessageNotification>> getChatThreadGagNotifications(string swid, string accesstoken)
    {
        var result = new List<AddChatThreadGagMessageNotification>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.isgag = 1;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedprofile = await GetProfilesSwid((string)session["swid"]);
            if (loadedprofile == null)
            {
                throw new Exception("From Given Swid from Profile not found");
            }

            var getSrvSett = await GetServerSettings();

            var threadId = reader.GetInt64("id");
            var item = new AddChatThreadGagMessageNotification
            {
                Message = new GagChatMessage
                {
                    ContentId = reader.GetString("contentid"),

                    Created = reader.GetInt64("timestamp"),
                    ChatMessageId = reader.GetInt64("msgid"),
                    ClientChatMessageId = reader.GetInt64("sessionid"),
                    ChatThreadId = threadId,
                    SenderUserId = reader.GetString("swid"),
                    SequenceNumber = reader.GetInt64("msgid"),
                    MessageType = "GAG"

                },
                SequenceNumber = getSrvSett.Sequence,
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence
            };

            result.Add(item);
        }
        return result;
    }

    public static async Task<List<AddChatThreadVideoMessageNotification>> getChatThreadVideoNotifications(string swid, string accesstoken)
    {
        var result = new List<AddChatThreadVideoMessageNotification>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.isvideo = 1;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedprofile = await GetProfilesSwid((string)session["swid"]);
            if (loadedprofile == null)
            {
                throw new Exception("From Given Swid from Profile not found");
            }

            var getSrvSett = await GetServerSettings();

            var threadId = reader.GetInt64("id");
            var item = new AddChatThreadVideoMessageNotification
            {
                Message = new VideoChatMessage
                {
                    UgcMediaId = reader.GetString("videoflavorid"),
                    VideoId = reader.GetString("videoflavorid"),
                    Caption = "Hello World Video",
                    Duration = 0,
                    Thumbnail = new PhotoFlavor
                    {
                        PhotoFlavorId = reader.GetString("photoflavorid"),
                        Width = 140,
                        Height = 90,
                        Encoding = "PNG",
                        Url = "http://127.0.0.1/test.png"
                    },
                    VideoFlavors = new List<VideoFlavor>
                    {
                        new VideoFlavor
                        {
                            VideoFlavorId = reader.GetString("videoflavorid"),
                            Width = 140,
                            Height = 90,
                            Format = "MP4",
                            Bitrate = 300
                        }
                    },

                    Created = reader.GetInt64("timestamp"),
                    ChatMessageId = reader.GetInt64("msgid"),
                    ClientChatMessageId = reader.GetInt64("sessionid"),
                    ChatThreadId = threadId,
                    SenderUserId = reader.GetString("swid"),
                    SequenceNumber = reader.GetInt64("msgid"),
                    MessageType = "VIDEO"

                },
                SequenceNumber = getSrvSett.Sequence,
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence
            };

            result.Add(item);
        }
        return result;
    }

    public static async Task<List<AddChatThreadPhotoMessageNotification>> getChatThreadPhotoNotifications(string swid, string accesstoken)
    {
        var result = new List<AddChatThreadPhotoMessageNotification>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.isphoto = 1;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedprofile = await GetProfilesSwid((string)session["swid"]);
            if (loadedprofile == null)
            {
                throw new Exception("From Given Swid from Profile not found");
            }

            var getSrvSett = await GetServerSettings();

            var threadId = reader.GetInt64("id");
            var item = new AddChatThreadPhotoMessageNotification
            {
                Message = new PhotoChatMessage
                {
                    UgcMediaId = reader.GetString("photoflavorid"),
                    PhotoId = reader.GetString("photoflavorid"),
                    Caption = "Hello World Picture",
                    PhotoFlavors = new List<PhotoFlavor>
                    {
                        new PhotoFlavor
                        {
                            PhotoFlavorId = reader.GetString("photoflavorid"),
                            Width = 140,
                            Height = 90,
                            Encoding = "PNG",
                            Url = "http://127.0.0.1/test.png"
                        }
                    },

                    Created = reader.GetInt64("timestamp"),
                    ChatMessageId = reader.GetInt64("msgid"),
                    ClientChatMessageId = reader.GetInt64("sessionid"),
                    ChatThreadId = threadId,
                    SenderUserId = reader.GetString("swid"),
                    SequenceNumber = reader.GetInt64("msgid"),
                    MessageType = "PHOTO"

                },
                SequenceNumber = getSrvSett.Sequence,
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence
            };

            result.Add(item);
        }
        return result;
    }

    public static async Task<List<AddChatThreadGameStateMessageNotification>> getChatThreadGameStateNotifications(string swid, string accesstoken)
    {
        var result = new List<AddChatThreadGameStateMessageNotification>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.isgame = 1;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {

            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedprofile = await GetProfilesSwid((string)session["swid"]);
            if (loadedprofile == null)
            {
                throw new Exception("From Given Swid from Profile not found");
            }

            var getSrvSett = await GetServerSettings();

            var threadId = reader.GetInt64("id");
            var item = new AddChatThreadGameStateMessageNotification
            {
                Message = new GameStateChatMessage
                {
                    GameName = reader.GetString("gamename"),
                    State = reader.GetString("gamestate"),

                    Created = reader.GetInt64("timestamp"),
                    ChatMessageId = reader.GetInt64("msgid"),
                    ClientChatMessageId = reader.GetInt64("sessionid"),
                    ChatThreadId = threadId,
                    SenderUserId = reader.GetString("swid"),
                    SequenceNumber = reader.GetInt64("msgid"),
                    MessageType = "GAME_STATE"

                },
                SequenceNumber = getSrvSett.Sequence,
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence
            };

            result.Add(item);
        }
        return result;
    }

    public static async Task<List<AddChatThreadGameEventMessageNotification>> getChatThreadGameEventNotifications(string swid, string accesstoken)
    {
        var result = new List<AddChatThreadGameEventMessageNotification>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(@"
        SELECT ct.*, 
        ROW_NUMBER() OVER (ORDER BY ct.timestamp) AS seq
        FROM chatthreadchats ct
        INNER JOIN chatthreadsusers ctu ON ct.id = ctu.id
        WHERE ctu.swid = @swid
        AND ct.isgameevent = 1;", conn);

        cmd.Parameters.AddWithValue("@swid", swid);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {

            var session = await GetSessionAccessToken(accesstoken);
            if (session == null)
            {
                throw new Exception("Current Session not found");
            }

            var loadedprofile = await GetProfilesSwid((string)session["swid"]);
            if (loadedprofile == null)
            {
                throw new Exception("From Given Swid from Profile not found");
            }

            var getSrvSett = await GetServerSettings();

            var threadId = reader.GetInt64("id");
            var item = new AddChatThreadGameEventMessageNotification
            {
                Message = new GameEventChatMessage
                {
                    GameStateMessageId = reader.GetInt64("msgid"),
                    GameName = reader.GetString("gamename"),

                    Created = reader.GetInt64("timestamp"),
                    ChatMessageId = reader.GetInt64("msgid"),
                    ClientChatMessageId = reader.GetInt64("sessionid"),
                    ChatThreadId = threadId,
                    SenderUserId = reader.GetString("swid"),
                    SequenceNumber = reader.GetInt64("msgid"),
                    MessageType = "GAME_EVENT"

                },
                SequenceNumber = getSrvSett.Sequence,
                Created = (long)session["timestamp"],
                NotificationId = getSrvSett.Sequence
            };

            result.Add(item);
        }
        return result;
    }
    private static void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException(
                "Database not configured. Call DatabaseManager.Configure(connectionString) or TryConfigureFromEnvironment() first.");
    }
}
