namespace Common.Utils.Game;

public sealed record ShareableConsumableDefinition(
    string Type,
    int Cost,
    int ActionCount,
    string RecipientType,
    bool MemberOnly);

public sealed class UsedConsumable
{
    public string type { get; set; } = "";
    public int partialCount { get; set; }
}

public sealed class ConsumableInventory
{
    public Dictionary<string, InventoryItemStock> inventoryMap { get; set; } = new(StringComparer.Ordinal);
}

public sealed class InventoryItemStock
{
    public int itemCount { get; set; }
    public int partialCount { get; set; }
    public long lastPurchaseTimestamp { get; set; }

    public int GetItemCount() => itemCount + (partialCount > 0 ? 1 : 0);
}

public sealed class ConsumablePurchaseResponse : CPResponse
{
    public PlayerAssets assets { get; set; } = new();
    public SignedResponse<ConsumableInventory> inventory { get; set; } = new();
}

public enum GameLocomotionAction : byte
{
    Interact = 2
}

public enum InteractionObjectType
{
    Local,
    Server,
    Player
}

public static class ShareableConsumables
{
    private static readonly IReadOnlyDictionary<string, ShareableConsumableDefinition> Definitions =
        new Dictionary<string, ShareableConsumableDefinition>(StringComparer.Ordinal)
        {
            ["BirthdayCakeGroup"] = new("BirthdayCakeGroup", 25, 6, "BirthdayCakeServerAdded", true),
            ["HotChocolateGroup"] = new("HotChocolateGroup", 25, 6, "HotChocolateServerAdded", true),
            ["PizzaGroup"] = new("PizzaGroup", 25, 6, "PizzaServerAdded", true),
            ["QuestStinkyCheeseTray"] = new("QuestStinkyCheeseTray", 0, 6, "StinkyCheeseSlice", false),
            ["SquidStickGroup"] = new("SquidStickGroup", 25, 6, "SquidStickServerAdded", true),
            ["StinkyCheeseTray"] = new("StinkyCheeseTray", 25, 6, "StinkyCheeseServerAdded", true),
            ["FishdogGroup"] = new("FishdogGroup", 25, 5, "FishdogServerAdded", true),
            ["GlowstickPack"] = new("GlowstickPack", 25, 6, "GlowStickServerAdded", true),
            ["PizzaPlanetGroup"] = new("PizzaPlanetGroup", 25, 6, "PizzaPlanetServerAdded", true),
            ["SeaweedSmoothieTray"] = new("SeaweedSmoothieTray", 25, 4, "SeaweedSmoothieServerAdded", true),
            ["ScavengerHuntGroup"] = new("ScavengerHuntGroup", 30, 1, "ScavengerHuntLobbyServerAdded", false),
            ["FishBucketGroup"] = new("FishBucketGroup", 40, 3, "FishBucketLobbyServerAdded", false),
            ["FindFourGroup"] = new("FindFourGroup", 30, 1, "", false),
            ["SpicyPizzaGroup"] = new("SpicyPizzaGroup", 25, 6, "SpicyPizzaServerAdded", true),
            ["HalloweenBasketTray"] = new("HalloweenBasketTray", 25, 3, "HalloweenBasketServerAdded", true),
            ["HotChocolateGroupNonMember"] = new("HotChocolateGroupNonMember", 0, 6, "HotChocolateServerAdded", false),
            ["PizzaGroupNonMember"] = new("PizzaGroupNonMember", 0, 6, "PizzaServerAdded", false),
            ["SquidStickGroupNonMember"] = new("SquidStickGroupNonMember", 0, 6, "SquidStickServerAdded", false),
            ["CandyCane"] = new("CandyCane", 25, 4, "CandyCaneServerAdded", true),
            ["FluffyCakeTray"] = new("FluffyCakeTray", 25, 6, "FluffyCakeServerAdded", true),
            ["RainbowSmoothieTray"] = new("RainbowSmoothieTray", 25, 4, "RainbowSmoothieServerAdded", true),
            ["BirthdayCakeGroupNonMember"] = new("BirthdayCakeGroupNonMember", 25, 6, "BirthdayCakeServerAdded", false),
            ["IceCreamTray"] = new("IceCreamTray", 20, 3, "IceCreamServerAdded", true),
            ["PopscornTray"] = new("PopscornTray", 25, 3, "PopscornServerAdded", true),
            ["20thBirthdayCakeGroup"] = new("20thBirthdayCakeGroup", 25, 6, "20thBirthdayCakeServerAdded", true),
            ["Science_BeakerGroup"] = new("Science_BeakerGroup", 35, 4, "Science_BeakerServerAdded", true),
            ["Science_BeakerGreenGroup"] = new("Science_BeakerGreenGroup", 35, 4, "Science_BeakerGreenServerAdded", true),
            ["PartyBlaster"] = new("PartyBlaster", 25, 1, "", true),
            ["PartyBlasterNonMember"] = new("PartyBlasterNonMember", 25, 1, "", false),
            ["SuperPartyBlaster"] = new("SuperPartyBlaster", 50, 1, "", true),
            ["MegaPartyBlaster"] = new("MegaPartyBlaster", 100, 1, "", true),
            ["MegaPartyBlasterNonMember"] = new("MegaPartyBlasterNonMember", 100, 1, "", false),
            ["HolidayPartyBlaster"] = new("HolidayPartyBlaster", 25, 1, "", true),
            ["HalloweenPartyBlaster"] = new("HalloweenPartyBlaster", 50, 1, "", true)
        };

    public static bool TryGet(string type, out ShareableConsumableDefinition definition) =>
        Definitions.TryGetValue(type, out definition!);

    public static IReadOnlyCollection<ShareableConsumableDefinition> All => Definitions.Values.ToArray();
}

public sealed record PartyBlasterDefinition(int TotalCoins, float RewardRadius, float TimeToLiveSeconds);

public static class PartyBlasters
{
    private static readonly IReadOnlyDictionary<string, PartyBlasterDefinition> Definitions =
        new Dictionary<string, PartyBlasterDefinition>(StringComparer.Ordinal)
        {
            ["PartyBlaster"] = new(25, 2f, 5f),
            ["PartyBlasterNonMember"] = new(25, 2f, 5f),
            ["SuperPartyBlaster"] = new(50, 2.5f, 5f),
            ["MegaPartyBlaster"] = new(100, 3.5f, 5f),
            ["MegaPartyBlasterNonMember"] = new(100, 3.5f, 5f),
            ["HolidayPartyBlaster"] = new(25, 2f, 5f),
            ["HalloweenPartyBlaster"] = new(50, 2.5f, 5f)
        };

    public static bool TryGet(string type, out PartyBlasterDefinition definition) =>
        Definitions.TryGetValue(type, out definition!);
}
