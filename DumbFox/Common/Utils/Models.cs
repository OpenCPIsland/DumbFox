using Common.Utils.Game;
using Sfs2X.Requests;
using SFSLitJson;
using System.Numerics;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Common.Utils
{
    //for DumbFox and DumbFoxWS and Webapi GameAPI
    public class SignedResponse<T>
    {
        public long goodUntil { get; set; } = 0L;
        public string? swid { get; set; }
        public T? data { get; set; }
        public string? signature { get; set; }
    }
}

namespace Common.Utils.Game
{
    //GameAPI Models
    public enum GameApiErrorCodes { INVAILDOREXPIREDACCESSTOKEN = 3, UNKOWN10001 = 10001, UNKOWN10002 = 10002, UNKOWN10003 = 10003 }
    public class BaseCodeError
    {
        public int Code { get; set; } = 3;
    }

    public class ClaimPreregistrationRewardResponse : CPResponse
    {
        public Reward reward { get; set; } = new Reward();
    }
    public class ClaimServerAddedRewardsResponse : CPResponse
    {
        public List<ClaimedServerAddedReward> claimedRewards { get; set; } = [];
    }
    public class ClaimedServerAddedReward
    {
        public ServerAddedRewardId rewardId { get; set; } = new ServerAddedRewardId();
        public Reward reward { get; set; } = new Reward();
    }

    public class CustomEquipmentId : CPResponse
    {
        public long? equipmentId { get; set; } = 0L;
    }
    public class CreateEquipmentResponse : CPResponse
    {
        public long? equipmentId { get; set; } = 0L;
        public PlayerAssets? assets { get; set; } = new PlayerAssets();
    }

    public class ItemSubmissionRequest
    {
        public long scheduledThemeChallengeId { get; set; } = 578L;
        public CatalogSubmissionEquipment equipment { get; set; } = new CatalogSubmissionEquipment();
    }
    public class ItemSubmissionResponse : CPResponse
    {
        public long clothingCatalogItemId { get; set; } = 1L;
        public long newCoinTotal { get; set; } = 0L;
    }

    public class CatalogSubmissionEquipment
    {
        public int definitionId { get; set; } = 0;
        public List<CustomEquipmentPart> parts { get; set; } = new List<CustomEquipmentPart>();
    }
    public class CatalogItemData
    {
        public long clothingCatalogItemId { get; set; } = 1L;
        public string creatorName { get; set; } = "TestPlayer";
        public long cost { get; set; } = 40L;
        public long numberSold { get; set; } = 0L;
        public CustomEquipment equipment { get; set; } = new CustomEquipment();
    }
    public class CurrentThemeData
    {
        public long scheduledThemeChallengeId { get; set; } = 578L;
        public long userSubmissionClothingCatalogId { get; set; } = 0L;
        public CatalogItemData? mostPopularItem { get; set; }
        public Reward submissionReward { get; set; } = new Reward();
    }
    public class CurrentThemeResponse
    {
        public List<CurrentThemeData> themes { get; set; } = [];
    }

    public class CatalogCategoryRequest
    {
        public long scheduledThemeChallengeId { get; set; } = 578L;
        public string cursor { get; set; } = string.Empty;
        public string equipmentCategory { get; set; } = "Feet";
    }

    public class CatalogSectionRequest
    {
        public long scheduledThemeChallengeId { get; set; } = 578L;
        public string cursor { get; set; }  = string.Empty;
    }
    public class CatalogSectionResponse
    {
        public string cursor { get; set; } = string.Empty;
        public List<CatalogItemData> items { get; set; } = new List<CatalogItemData>();
    }

    public class ItemPurchaseRequest
    {
        public long clothingCatalogItemId { get; set; } = 1L;
    }
    public class ItemPurchaseResponse
    {
        public long equipmentId { get; set; } = 0L;
        public long newCoinTotal { get; set; } = 0L;
    }

    public class CPResponse
    {
        public List<SignedResponse<WebServiceEvent>>? wsEvents { get; set; } = [];
    }

    public class ServerAddedRewardId
    {
        public int definitionId { get; set; } = 0;
        public string instanceId { get; set; } = "fishing";
    }


    public class JoinRoomData
    {
        public string swid { get; set; } = "999999999999999";
        public string host { get; set; } = "192.168.178.49";
        public int tcpPort { get; set; } = 9933;
        public int httpsPort { get; set; } = 9934;
        public string room { get; set; } = "Alpine:en_US:Boardwalk::1.0.0;Offline;2016-11-30;NONE";
        public long sessionId { get; set; } = 1L;
        public PlayerRoomData playerRoomData { get; set; } = new PlayerRoomData();
        public object earnedRewards { get; set; } = new { };
        public string userName { get; set; } = "TestPlayer";
        public MembershipRights membershipRights { get; set; } = new MembershipRights();
        public int selectedTubeId { get; set; } = 0;
        public SceneLayout? extraLayoutData { get; set; }
        public string? roomOwnerName { get; set; }
        public bool roomOwner { get; set; } = false;
    }

    public class PlayerRoomData
    {
        public bool member { get; set; } = false;
        public PlayerOutfitDetails outfit { get; set; } = new PlayerOutfitDetails
        {
            sessionId = 1L,
            parts = new List<CustomEquipment>()
        };
        public object[] dailyTaskProgress { get; set; } = [];
        public object consumableInventory { get; set; } = new { inventoryMap = new { } };
        public PlayerAssets assets { get; set; } = new PlayerAssets
        {
            currency = new
            {
                coins = 250
            },
            mascotXP = new { AuntArctic = 1192307, Rockhopper = 166666 },
            collectibleCurrency = new { },
            colourPacks = [],
            decals = [169, 170, 171],
            fabrics = [258, 259, 260],
            emotePacks = [],
            sizzleClips = [],
            equipmentTemplates = [],
            lots = [],
            decorations = [238, 239, 240, 241, 242, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 265, 266, 268, 269, 270, 272, 273, 274, 275, 276],
            structures = [],
            musicTracks = [],
            lighting = [],
            durables = [],
            tubes = [],
            coins = 250,
            partySupplies = [],
        };
        public Profile profile { get; set; } = new Profile();
        public List<QuestStateCollection> quests { get; set; } = [
        new QuestStateCollection{
        questId = "AAC001Q001LeakyShip",
        status = (int)QuestStatus.COMPLETED,
        completedObjectives = ["Obj0FSM", "Obj2FSM ", "Obj3FSM ", "Obj4FSM", "Obj5FSM", "Obj6FSM", "Obj8FSM", "Obj9FSM", "Obj10FSM", "Obj11FSM"],
        timesCompleted = 1
    },new QuestStateCollection{
        questId = "RHC001Q001TreasureQuest",
        status = (int)QuestStatus.AVAILABLE,
    },new QuestStateCollection{
        questId = "RKC001Q001Drop",
        status = (int)QuestStatus.AVAILABLE,
    },new QuestStateCollection{
        questId = "DJC001Q001Plan",
        status = (int)QuestStatus.AVAILABLE,
    }
        ];
    }

    public class MembershipRights
    {
        public string swid { get; set; } = "999999999999999";
        public bool member { get; set; } = false;
        public bool recurring { get; set; } = false;
        public long recurDate { get; set; } = 0L;
        public long expireDate { get; set; } = 0L;
        public bool allAccessAllowed { get; set; } = false;
        public bool paymentPending { get; set; } = false;
        public bool trialAvailable { get; set; } = false;
        public bool trialPeriod { get; set; } = false;
        public string? vendor { get; set; }
        public string? productId { get; set; }
        public int numRenewals { get; set; } = 0;
        public bool autoRenewing { get; set; } = false;

    }

    public class LocalPlayerData : OtherPlayerData
    {
        public List<MinigameProgress> minigameProgress { get; set; } = [];
        public List<QuestStateCollection> quests { get; set; } = [
        new QuestStateCollection{
        questId = "AAC001Q001LeakyShip",
        status = (int)QuestStatus.COMPLETED,
        completedObjectives = ["Obj0FSM", "Obj2FSM ", "Obj3FSM ", "Obj4FSM", "Obj5FSM", "Obj6FSM", "Obj8FSM", "Obj9FSM", "Obj10FSM", "Obj11FSM"],
        timesCompleted = 1
    },new QuestStateCollection{
        questId = "RHC001Q001TreasureQuest",
        status = (int)QuestStatus.AVAILABLE,
    },new QuestStateCollection{
        questId = "RKC001Q001Drop",
        status = (int)QuestStatus.AVAILABLE,
    },new QuestStateCollection{
        questId = "DJC001Q001Plan",
        status = (int)QuestStatus.AVAILABLE,
    }
        ];
        public int membershipExpireDate { get; set; } = 0;
        public string? subscriptionVendor { get; set; }
        public string? subscriptionProductId { get; set; }
        public bool subscriptionPaymentPending { get; set; } = false;
        public MigrationData? migrationData { get; set; }
        public int[] tutorialData { get; set; } = [1];
        public BreadcrumbsResponse breadcrumbs { get; set; } = new BreadcrumbsResponse();
        public int[] claimedRewardIds { get; set; } = [];
        public DailySpinData dailySpinData { get; set; } = new DailySpinData();
        public SavedIglooLayoutsSummary iglooLayouts { get; set; } = new SavedIglooLayoutsSummary();
        public bool trialAvailable { get; set; } = false;
        public bool recurring { get; set; } = false;
    }
    public class OtherPlayerData
    {
        public ProfileId id { get; set; } = new ProfileId();
        public string name { get; set; } = "TestPlayer";
        public RoomIdentifier? onlineLocation { get; set; }
        public bool member { get; set; } = false;
        public List<CustomEquipment>? outfit { get; set; } = [];
        public Profile profile { get; set; } = new Profile();
        public object mascotXP { get; set; } = new { AuntArctic = 1192307, Rockhopper = 166666 };
        public ZoneId zoneId { get; set; } = new ZoneId();
    }
    public class ProfileId
    {
        public string id { get; set; } = "999999999999999";
        public int type { get; set; } = 1;
    }
    public class Profile
    {
        public int colour { get; set; } = 0;
        public int daysOld { get; set; } = 0;
    }
    public class MinigameProgress
    {
        public string gameId { get; set; } = "";
        public int playCount { get; set; } = 0;
        public int day { get; set; } = 0;
    }
    public class MigrationData
    {
        public string status { get; set; } = "MIGRATED";
        public long? migratedDate { get; set; }
        public LegacyAccountData? legacyAccountData { get; set; }
    }
    public class LegacyAccountData
    {
        public string username { get; set; } = "TestPlayer";
        public bool member { get; set; } = false;
        public long createdDate { get; set; } = 0L;
    }
    public class DailySpinData
    {
        public long timeOfLastSpinInMilliseconds { get; set; } = 0L;
        public int currentChestId { get; set; } = 0;
        public int numPunchesOnCurrentChest { get; set; } = 1;
        public int numChestsReceivedOfCurrentChestId { get; set; } = 0;
    }
    public class BreadcrumbsResponse
    {
        public object[] breadcrumbs { get; set; } = [];
    }

    public class SavedIglooLayoutsSummary
    {
        public int visibility { get; set; } = 0;
        public int activeLayoutId { get; set; } = 0;
        public object[] layouts { get; set; } = [];
        public int activeLayoutServerChangeNotification { get; set; } = 0;
    }

    public class RoomPopulation
    {
        public RoomIdentifier identifier { get; set; } = new RoomIdentifier();
        public int populationScaled { get; set; } = 0;
    }
    public enum langIdList { none, en_US, pt_BR, fr_FR = 4, es_LA = 8, oversized = 0x10 }
    public class RoomIdentifier
    {
        public string world { get; set; } = "Alpine";
        public int language { get; set; } = (int)langIdList.en_US;
        public ZoneId zoneId { get; set; } = new ZoneId { name = "Boardwalk", instanceId = "" };
        public string contentIdentifier { get; set; } = "1.0.0;offline;2016-11-28;NONE";
        public string room { get; set; } = "Boardwalk";
    }
    public class ZoneId
    {
        public string? name { get; set; }
        public string? instanceId { get; set; }
    }

    public enum QuestStatus { AVAILABLE, ACTIVE, SUSPENDED, COMPLETED, LOCKED }
    public class QuestStateCollection
    {
        public string questId { get; set; } = "AAC001Q001LeakyShip";
        public int status { get; set; } = (int)QuestStatus.AVAILABLE;
        public List<string> completedObjectives { get; set; } = new List<string>();
        public int timesCompleted { get; set; } = 0;
        public long? unlockTime { get; set; }
    }

    public class PlayerOutfit
    {
        public List<long>? parts { get; set; }
    }
    public enum TypeEnum { DECAL, FABRIC };
    public class CustomEquipment
    {
        public int definitionId { get; set; } = 0;
        public List<CustomEquipmentPart> parts { get; set; } = [];
        public long equipmentId { get; set; } = 0;
        public long dateTimeCreated { get; set; } = 0L;
        public string source { get; set; } = "None";
        public int? sourceId { get; set; } = 0;
    }
    public class CustomEquipmentCustomization
    {
        public int type { get; set; } = (int)TypeEnum.DECAL;
        public int definitionId { get; set; } = 0;
        public int index { get; set; } = 0;
        public float scale { get; set; } = 0.0f;
        public float rotation { get; set; } = 0.0f;
        public bool repeat { get; set; } = false;
        public float uoffset { get; set; } = 0.0f;
        public float voffset { get; set; } = 0.0f;
    }
    public class CustomEquipmentPart
    {
        public int slotIndex { get; set; } = 0;
        public List<CustomEquipmentCustomization> customizations { get; set; } = [];
    }

    public class PCSessionStartResponse
    {
        public string? SessionId { get; set; }
        public APPSessionSummary SessionSummary { get; set; } = new APPSessionSummary();
    }
    public class APPSessionSummary
    {
        public string? Email { get; set; }
        public object[] ExternalAuthorizations { get; set; } = [];
        public string? FirstName { get; set; }
        public string HomeCountry { get; set; } = "JPN";
        public string? LastName { get; set; }
        public string Login { get; set; } = "999999999999999";
        public string? SubscriberReference { get; set; }
        public int SubscriberId { get; set; } = 0;
        public string SubscriberLanguage { get; set; } = "en-US";
        public string TermsAndConditionsAccepted { get; set; } = "2016-11-30T12:11:01.100Z";
    }

    public class Reward
    {
        public long coins { get; set; } = 0;
        public object? currency { get; set; }
        public object? mascotXP { get; set; }
        public object? collectibleCurrency { get; set; }
        public int[]? colourPacks { get; set; }
        public int[]? decals { get; set; }
        public int[]? fabrics { get; set; }
        public int[]? emotePacks { get; set; }
        public int[]? sizzleClips { get; set; }
        public int[]? equipmentTemplates { get; set; }
        public int[]? equipmentInstances { get; set; }
        public int[]? lots { get; set; }
        public int[]? decorationInstances { get; set; }
        public int[]? structureInstances { get; set; }
        public int[]? decorationPurchaseRights { get; set; }
        public int[]? structurePurchaseRights { get; set; }
        public int[]? musicTracks { get; set; }
        public int[]? lighting { get; set; }
        public int[]? durables { get; set; }
        public int[]? tubes { get; set; }
        public int savedOutfitSlots { get; set; } = 0;
        public int iglooSlots { get; set; } = 0;
        public int[]? consumables { get; set; }
        public int[]? partySupplies { get; set; }
    }

    public class PlayerAssets
    {
        public object? currency { get; set; }
        public object? mascotXP { get; set; }
        public object? collectibleCurrency { get; set; }
        public int[]? colourPacks { get; set; }
        public int[]? decals { get; set; }
        public int[]? fabrics { get; set; }
        public int[]? emotePacks { get; set; }
        public int[]? sizzleClips { get; set; }
        public int[]? equipmentTemplates { get; set; }
        public int[]? lots { get; set; }
        public int[]? decorations { get; set; }
        public int[]? structures { get; set; }
        public int[]? musicTracks { get; set; }
        public int[]? lighting { get; set; }
        public int[]? durables { get; set; }
        public int[]? tubes { get; set; }
        public int savedOutfitSlots { get; set; } = 0;
        public int iglooSlots { get; set; } = 0;
        public int coins { get; set; } = 0;
        public int[]? partySupplies { get; set; }
    }
    public class PlayerOutfitDetails
    {
        public long? sessionId { get; set; } = 0L;
        public List<CustomEquipment>? parts { get; set; } = new List<CustomEquipment>();
    }

    public class SceneLayout : MutableSceneLayout
    {
        public long? createdDate { get; set; } = 0L;
        public long? lastModifiedDate { get; set; } = 0L;
        public bool memberOnly { get; set; } = false;
    }
    public enum DecorationType
    {
        Decoration,
        Structure
    }
    public class DecorationLayout
    {
        public class Id
        {
            public string name = "1_Dummy(Clone)";
            public string? parent { get; set; }
        }

        public Id id { get; set; } = new Id { };
        public DecorationType type { get; set; } = DecorationType.Decoration;
        public long definitionId { get; set; } = 0L;
        public Vector3 position { get; set; } = new Vector3();
        public Quaternion rotation { get; set; } = new Quaternion();
        public Vector3 normal { get; set; } = new Vector3();
        public float scale { get; set; } = 1.0f;
        public Dictionary<string, string> customProperties { get; set; } = new Dictionary<string, string>{};
    }
    public class MutableSceneLayout
    {
        public string name { get; set; } = "";
        public string zoneId { get; set; } = "DefaultIgloo";
        public int lightingId { get; set; } = 0;
        public int musicId { get; set; } = 0;
        public Dictionary<string, string>? extraInfo { get; set; } = new Dictionary<string, string>{ { "SittingLocations", "[]" } };
        public List<DecorationLayout> decorationsLayout { get; set; } = [];
    }

    public class ChatMessage
    {
        public long senderSessionId { get; set; } = 1L;
        public string? message { get; set; } = "Hello World!";
        public int emotion { get; set; } = 0;
        public bool moderated { get; set; } = false;
        public string questId { get; set; } = "";
        public string objective { get; set; } = "";
    }
    public class PublicKeyData
    {
        public string? exponent { get; set; }
        public string? modulus { get; set; }
    }
    public class EncryptionKey
    {
        public string? keyId { get; set; }
        public string? encryptedSymmetricKey { get; set; }
    }

    public class WebServiceEvent
    {
        public int? type { get; set; } = 0;
        public dynamic? details { get; set; }
    }
}

namespace Common.Utils.Mix
{
    //Mix Models
    public enum BaseResponseStatus
    {
        OK,
        UNAUTHORIZED_MIX_SESSION,
        UNAUTHORIZED_ONEID_TOKEN,
        UNAUTHORIZED_BANNED
    }
    public class BaseResponse
    {
        public string Status { get; set; } = "OK";
    }

    public class BaseUserRequest
    {
        public string UserId { get; set; } = "999999999999";
        public long? Timestamp { get; set; } = 0L;
    }

    public class BaseNotification
    {
        public long? Created { get; set; }
        public long? NotificationId { get; set; }
        public long? SequenceNumber { get; set; }
    }

    public class BaseChatThreadAddMessageRequest
    {
        public long ChatThreadId { get; set; } = 1L;
        public long ClientChatMessageId { get; set; } = 1L;
    }

    public enum ChatMessageTypes
    {
        GAG,
        GAME_EVENT,
        GAME_STATE,
        MEMBER_LIST_CHANGED,
        PHOTO,
        STICKER,
        TEXT,
        VIDEO,
        MUSIC
    }
    public class BaseChatMessage
    {
        public long? Created { get; set; }
        public long? ChatMessageId { get; set; }
        public long? ClientChatMessageId { get; set; }
        public long? ChatThreadId { get; set; }
        public string SenderUserId { get; set; } = "999999999999";
        public long? SequenceNumber { get; set; }
        public string MessageType { get; set; } = ChatMessageTypes.TEXT.ToString();
    }

    public class GetRegistrationTextResponse : BaseResponse
    {
        public List<RegistrationText>? RegistrationText { get; set; }
    }

    public class RegistrationText
    {
        public string? TextCode { get; set; }
        public string? Text { get; set; }
    }

    public class StartUserSessionResponse : BaseResponse
    {
        public string? EncryptedSymmetricKey { get; set; }
        public int SessionId { get; set; } = 1;
        public string? HashedUserId { get; set; }
    }

    public class GetStateRequest : BaseUserRequest
    {
        public long? GameStateSince { get; set; } = 10L;
        public string? ClientVersion { get; set; }
        public long? AvatarSince { get; set; } = 0L;
    }

    public class GetStateResponse : BaseResponse
    {
        public List<User> Users { get; set; } = [];
        public List<UserNickname> UserNicknames { get; set; } = [];
        public List<Friendship> Friendships { get; set; } = [];
        public List<string> OfficialAccounts { get; set; } = [];
        public List<ChatThread> ChatThreads { get; set; } = [];
        public List<ChatThreadNickname> ChatThreadNicknames { get; set; } = [];
        public List<ChatThreadUnreadMessageCount> ChatThreadUnreadMessageCount { get; set; } = [];
        public List<long?> ChatThreadLatestMessageSequenceNumbers { get; set; } = [];
        public List<long?> ChatThreadLastSeenMessageSequenceNumbers { get; set; } = [];
        public List<FriendshipInvitation> FriendshipInvitations { get; set; } = [];
        public List<GameStateChatMessage> GameStateChatMessages { get; set; } = [];
        public List<Alert> Alerts { get; set; } = [];
        public List<int?> PollIntervals { get; set; } = [5, 10, 30];
        public List<int?> PokeIntervals { get; set; } = [30];
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public long NotificationSequenceCounter { get; set; } = 10;
        public int NotificationSequenceThreshold { get; set; } = 7000;
        public int NotificationIntervalsJitter { get; set; } = 500;
    }

    public class GetNotificationsSinceSequenceRequest : BaseUserRequest
    {
        public long SequenceNumber { get; set; } = 10L;
        public List<long> ExcludeNotificationSequenceNumbers { get; set; } = new List<long>();
    }

    public class GetNotificationsResponse : BaseResponse
    {
        public long LastNotificationTimestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public long NotificationSequenceCounter { get; set; } = 0;
        public List<AddChatThreadNotification> AddChatThread { get; set; } = [];
        public List<AddChatThreadMembershipNotification> AddChatThreadMembership { get; set; } = [];
        public List<AddChatThreadGagMessageNotification> AddChatThreadGagMessage { get; set; } = [];
        public List<AddChatThreadMemberListChangedMessageNotification> AddChatThreadMemberListChangedMessage { get; set; } = [];
        public List<AddChatThreadPhotoMessageNotification> AddChatThreadPhotoMessage { get; set; } = [];
        public List<AddChatThreadStickerMessageNotification> AddChatThreadStickerMessage { get; set; } = [];
        public List<AddChatThreadTextMessageNotification> AddChatThreadTextMessage { get; set; } = [];
        public List<AddChatThreadVideoMessageNotification> AddChatThreadVideoMessage { get; set; } = [];
        public List<AddChatThreadNicknameNotification> AddChatThreadNickname { get; set; } = [];
        public List<AddChatThreadGameStateMessageNotification> AddChatThreadGameStateMessage { get; set; } = [];
        public List<UpdateChatThreadGameStateMessageNotification> UpdateChatThreadGameStateMessage { get; set; } = [];
        public List<AddChatThreadGameEventMessageNotification> AddChatThreadGameEventMessage { get; set; } = [];
        public List<AddFriendshipNotification> AddFriendship { get; set; } = [];
        public List<AddFollowshipNotification> AddFollowship { get; set; } = [];
        public List<AddFriendshipInvitationNotification> AddFriendshipInvitation { get; set; } = [];
        public List<AddNicknameNotification> AddNickname { get; set; } = [];
        public List<AddAlertNotification> AddAlert { get; set; } = [];
        public List<ClearAlertNotification> ClearAlert { get; set; } = [];
        public List<ClearMemberChatHistoryNotification> ClearMemberChatHistory { get; set; } = [];
        public List<ClearUnreadMessageCountNotification> ClearUnreadMessageCount { get; set; } = [];
        public List<RemoveChatThreadMembershipNotification> RemoveChatThreadMembership { get; set; } = [];
        public List<RemoveChatThreadNicknameNotification> RemoveChatThreadNickname { get; set; } = [];
        public List<RemoveFriendshipInvitationNotification> RemoveFriendshipInvitation { get; set; } = [];
        public List<RemoveFriendshipNotification> RemoveFriendship { get; set; } = [];
        public List<RemoveFollowshipNotification> RemoveFollowship { get; set; } = [];
        public List<RemoveFriendshipTrustNotification> RemoveFriendshipTrust { get; set; } = [];
        public List<RemoveNicknameNotification> RemoveNickname { get; set; } = [];
        public List<SetAvatarNotification> SetAvatar { get; set; } = [];
        public List<UpdateChatThreadTrustStatusNotification> UpdateChatThreadTrustStatus { get; set; } = [];
    }

    public class SetDisplayNameRequest : BaseUserRequest
    {
        public string DisplayName { get; set; } = "TestPlayer";
    }
    public class SetDisplayNameResponse : BaseResponse
    {
        public string DisplayName { get; set; } = "TestPlayer";
    }

    public class SetAvatarRequest : BaseUserRequest
    {
        public Avatar Avatar { get; set; } = new Avatar();
    }
    public class SetAvatarResponse : BaseResponse
    {
        public SetAvatarNotification Notification { get; set; } = new SetAvatarNotification();
    }

    public class OfficialAccountsResponse : BaseResponse
    {
        public List<GuestOfficialAccount> OfficialAccountIds { get; set; } = [];
    }

    public class DisplayNameSearchRequest : BaseUserRequest
    {
        public string DisplayName { get; set; } = "TestPlayer";
    }
    public class DisplayNameSearchResponse : BaseResponse 
    {
        public Avatar Avatar { get; set; } = new Avatar();
        public string DisplayName { get; set; } = "TestPlayer";
        public string? FirstName { get; set; } = "Test";
    }

    public class AddFriendshipInvitationRequest : BaseUserRequest
    {
        public string InviteeDisplayName { get; set; } = "TestPlayer";
        public bool? IsTrusted { get; set; } = false;
    }
    public class AddFriendshipInvitationResponse : BaseResponse
    {
        public AddFriendshipInvitationNotification Notification { get; set; } = new AddFriendshipInvitationNotification();
    }
    public class RemoveFriendshipInvitationRequest : BaseUserRequest
    {
        public long InvitationId { get; set; } = 1L;
    }
    public class RemoveFriendshipInvitationResponse : BaseResponse
    {
        public RemoveFriendshipInvitationNotification Notification { get; set; } = new RemoveFriendshipInvitationNotification();
    }
    public class AddFriendshipRequest : BaseUserRequest
    {
        public long InvitationId { get; set; } = 1L;
        public bool? IsTrusted { get; set; } = false;
    }
    public class AddFriendshipResponse : BaseResponse
    {
        public AddFriendshipNotification Notification { get; set; } = new AddFriendshipNotification();
    }
    public class RemoveFriendshipRequest : BaseUserRequest
    {
        public string FriendUserId { get; set; } = "999999999999998";
    }
    public class RemoveFriendshipResponse : BaseResponse
    {
        public RemoveFriendshipNotification Notification { get; set; } = new RemoveFriendshipNotification();
    }
    public class GetFriendshipRecommendationResponse : BaseResponse
    {
        public List<FriendRecommendation> FriendRecommendations { get; set; } = new List<FriendRecommendation>();
    }
    public class RemoveFriendshipTrustRequest : BaseUserRequest
    {
        public string FriendUserId { get; set; } = "999999999999998";
    }
    public class RemoveFriendshipTrustResponse : BaseResponse
    {
        public RemoveFriendshipTrustNotification Notification { get; set; } = new RemoveFriendshipTrustNotification();
    }

    public class GagChatMessage : BaseChatMessage
    {
        public string ContentId { get; set; } = string.Empty;
        public string TargetUserId { get; set; } = string.Empty;
    }


    public enum AlertType
    {
        Cybercriminal,
        Cyberbully,
        DatingTeens,
        OffensiveUser,
        OversharingChild,
        PotentialPaedophile,
        SexPest,
        Spammer,
        Terrorist,
        Troll,
        VulnerablePerson,
        Unknown
    }
    public class AlertTypeUtils
    {
        public static AlertType FromString(string str)
        {
            switch (str)
            {
                case "CYBERCRIMINAL":
                    return AlertType.Cybercriminal;
                case "CYBERBULLY":
                    return AlertType.Cyberbully;
                case "DATING_TEENS":
                    return AlertType.DatingTeens;
                case "OFFENSIVE_USER":
                    return AlertType.OffensiveUser;
                case "OVERSHARING_CHILD":
                    return AlertType.OversharingChild;
                case "POTENTIAL_PAEDOPHILE":
                    return AlertType.PotentialPaedophile;
                case "SEX_PEST":
                    return AlertType.SexPest;
                case "SPAMMER":
                    return AlertType.Spammer;
                case "TERRORIST":
                    return AlertType.Terrorist;
                case "TROLL":
                    return AlertType.Troll;
                case "VULNERABLE_PERSON":
                    return AlertType.VulnerablePerson;
                default:
                    return AlertType.Unknown;
            }
        }

        public static string ToString(AlertType type)
        {
            switch (type)
            {
                case AlertType.Cybercriminal:
                    return "CYBERCRIMINAL";
                case AlertType.Cyberbully:
                    return "CYBERBULLY";
                case AlertType.DatingTeens:
                    return "DATING_TEENS";
                case AlertType.OffensiveUser:
                    return "OFFENSIVE_USER";
                case AlertType.OversharingChild:
                    return "OVERSHARING_CHILD";
                case AlertType.PotentialPaedophile:
                    return "POTENTIAL_PAEDOPHILE";
                case AlertType.SexPest:
                    return "SEX_PEST";
                case AlertType.Spammer:
                    return "SPAMMER";
                case AlertType.Terrorist:
                    return "TERRORIST";
                case AlertType.Troll:
                    return "TROLL";
                case AlertType.VulnerablePerson:
                    return "VULNERABLE_PERSON";
                default:
                    return "Unknown";
            }
        }
    }

    public class Alert
    {
        public long? AlertId { get; set; }
        public string Level { get; set; } = AlertTypeUtils.ToString(AlertType.Cybercriminal);
        public string Text { get; set; } = string.Empty;
        public long? Timestamp { get; set; }
    }
    public class AddAlertNotification : BaseNotification
    {
        public Alert Alert { get; set; } = new Alert();
    }
    public class ClearAlertNotification : BaseNotification
    {
        public List<long?> AlertIds { get; set; } = [];
    }

    public class Avatar
    {
        public int AvatarId { get; set; } = 1;
        public int SlotId { get; set; } = 1;
        public AvatarProperty Accessory { get; set; } = new AvatarProperty();
        public AvatarProperty Brow { get; set; } = new AvatarProperty();
        public AvatarProperty Costume { get; set; } = new AvatarProperty();
        public AvatarProperty Eyes { get; set; } = new AvatarProperty();
        public AvatarProperty Hair { get; set; } = new AvatarProperty();
        public AvatarProperty Nose { get; set; } = new AvatarProperty();
        public AvatarProperty Mouth { get; set; } = new AvatarProperty();
        public AvatarProperty Skin { get; set; } = new AvatarProperty();
        public AvatarProperty Hat { get; set; } = new AvatarProperty();
    }

    public class AvatarProperty
    {
        public string SelectionKey { get; set; } = "0";
        public int TintIndex { get; set; } = 0;
        public double XOffset { get; set; } = 0.0;
        public double YOffset { get; set; } = 0.0;
    }

    public class SetAvatarNotification : BaseNotification
    {
        public Avatar Avatar { get; set; } = new Avatar();
    }

    public class GuestOfficialAccount
    {
        public string OaId { get; set; } = "OA-";
        public string OaName { get; set; } = "";
        public bool? IsAvailable { get; set; }
        public bool? CanUnfollow { get; set; }
    }

    public class User
    {
        public string UserId { get; set; } = "999999999999999";
        public string? HashedUserId { get; set; }
        public string DisplayName { get; set; } = "TestPlayer";
        public string? FirstName { get; set; }
        public Avatar? Avatar { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public string? Nickname { get; set; }
    }
    public class UserModeratedAction
    {
        public string Action { get; set; } = string.Empty;
        public string ModeratedUserId { get; set; } = string.Empty;
        public string MediaId { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class UserNickname
    {
        public string NicknamedUserId { get; set; } = "999999999999999";
        public string Nickname { get; set; } = "TestPlayer";
    }
    public class AddNicknameNotification : BaseNotification
    {
        public UserNickname Nickname { get; set; } = new UserNickname();
    }
    public class RemoveNicknameNotification : BaseNotification
    {
        public string NicknamedUserId = "999999999999999";
    }

    public class FriendRecommendation
    {
        public string DisplayName { get; set; } = "TestPlayer";
        public string FirstName { get; set; } = "Test";
        public Avatar Avatar { get; set; } = new Avatar();
    }
    public class Friendship
    {
        public string FriendUserId { get; set; } = "999999999999999";
        public bool? IsTrusted { get; set; } 
    }
    public class AddFriendshipNotification : BaseNotification
    {
        public User Friend { get; set; } = new User();
        public long? FriendshipInvitationId { get; set; }
        public bool? IsTrusted { get; set; }
    }
    public class RemoveFriendshipNotification : BaseNotification
    {
        public string FriendUserId { get; set; } = "999999999999999";
    }
    public class RemoveFriendshipTrustNotification : BaseNotification
    {
        public string FriendUserId { get; set; } = "999999999999999";
    }

    public class FriendshipInvitation
    {
        public long? FriendshipInvitationId { get; set; }
        public string FriendDisplayName { get; set; } = "TestPlayer";
        public bool? IsInviter { get; set; }
        public bool? IsTrusted { get; set; }
    }
    public class AddFriendshipInvitationNotification : BaseNotification
    {
        public FriendshipInvitation Invitation { get; set; } = new FriendshipInvitation();
        public User Friend { get; set; } = new User();
    }

    public class RemoveFriendshipInvitationNotification : BaseNotification
    {
        public long? InvitationId { get; set; }
    }

    public class AddFollowshipNotification : BaseNotification
    {
        public GuestOfficialAccount OfficialAccount { get; set; } = new GuestOfficialAccount();
    }
    public class RemoveFollowshipNotification : BaseNotification
    {
        public string OfficialAccountId { get; set; } = "OA-72844169130089556";
    }

    public class ClearMemberChatHistoryNotification : BaseNotification
    {
        public List<long?> ChatThreadIds { get; set; } = [];
    }
    public class ClearUnreadMessageCountNotification : BaseNotification
    {
        public List<long?> ChatThreadIds { get; set; } = [];
    }
    public class ChatThreadUnreadMessageCount
    {
        public long? ChatThreadId { get; set; }
        public int? UnreadMessageCount { get; set; }
    }

    public class UpdateChatThreadTrustStatusNotification : BaseNotification
    {
        public long? ChatThreadId { get; set; }
        public bool? IsTrusted { get; set; }
    }

    public class AddChatThreadMembershipNotification : BaseNotification
    {
        public long? ChatThreadId { get; set; }
        public List<User> Members { get; set; } = [];
        public string WelcomingUserId { get; set; } = "999999999999999";
    }
    public class RemoveChatThreadMembershipNotification : BaseNotification
    {
        public long? ChatThreadId { get; set; }
        public string MemberUserId { get; set; } = string.Empty;
    }
    public class AddChatThreadNotification : BaseNotification
    {
        public ChatThread ChatThread { get; set; } = new ChatThread();
        public string WelcomingUserId { get; set; } = "999999999999999";
    }
    public class AddChatThreadGagMessageNotification : BaseNotification
    {
        public GagChatMessage Message { get; set; } = new GagChatMessage();
    }
    public enum ChatThreadTypes
    {
        ONE_ON_ONE,
        GROUP,
        OFFICIAL_ACCOUNT
    }
    public class AddChatThreadRequest : BaseUserRequest
    {
        public string ChatThreadType { get; set; } = ChatThreadTypes.ONE_ON_ONE.ToString();
        public List<string> MemberUserIds { get; set; } = new List<string> { "999999999999999" };
    }
    public class AddChatThreadResponse : BaseResponse
    {
        public AddChatThreadNotification Notification { get; set; } = new AddChatThreadNotification();
    }
    public class GetChatThreadMessagesRequest : BaseUserRequest
    {
        public long ChatThreadId { get; set; } = 1;
        public List<long?> ExcludedChatMessageIds { get; set; } = new List<long?> { 0 };
        public int? MaximumMessageCount { get; set; }
        public long? TimestampOffset { get; set; }
        public string Cursor { get; set; } = string.Empty;
    }
    public class GetChatThreadMessagesBySequenceRangesRequest : BaseUserRequest
    {
        public long ChatThreadId { get; set; } = 1L;
        public List<long> Ranges { get; set; } = new List<long>();
    }
    public class GetChatThreadMessagesResponse : BaseResponse
    {
        public string Cursor { get; set; } = string.Empty;
        public List<TextChatMessage> Text { get; set; } = [];
        public List<StickerChatMessage> Sticker { get; set; } = [];
        public List<GagChatMessage> Gag { get; set; } = [];
        public List<VideoChatMessage> Video { get; set; } = [];
        public List<PhotoChatMessage> Photo { get; set; } = [];
        public List<GameStateChatMessage> GameState { get; set; } = [];
        public List<GameEventChatMessage> GameEvent { get; set; } = [];
        public List<MemberListChangedChatMessage> MemberListChanged { get; set; } = [];
    }

    public class AddChatThreadTextMessageRequest : BaseChatThreadAddMessageRequest
    {
        public string Text { get; set; } = "Hello World!";
    }
    public class AddChatThreadTextMessageResponse : BaseResponse
    {
        public AddChatThreadTextMessageNotification Notification { get; set; } = new AddChatThreadTextMessageNotification();
    }
    public class AddChatThreadStickerMessageRequest : BaseChatThreadAddMessageRequest
    {
        public string ContentId { get; set; } = "1";
    }
    public class AddChatThreadStickerMessageResponse : BaseResponse
    {
        public AddChatThreadStickerMessageNotification Notification { get; set; } = new AddChatThreadStickerMessageNotification();
    }
    public class AddChatThreadGagMessageRequest : BaseChatThreadAddMessageRequest
    {
        public string ContentId { get; set; } = "1";
    }
    public class AddChatThreadGagMessageResponse : BaseResponse
    {
        public AddChatThreadGagMessageNotification Notification { get; set; } = new AddChatThreadGagMessageNotification();
    }
    public class AddChatThreadVideoMessageRequest : BaseChatThreadAddMessageRequest
    {
        public string UgcMediaId { get; set; } = "1";
    }
    public class AddChatThreadVideoMessageResponse : BaseResponse
    {
        public AddChatThreadVideoMessageNotification Notification { get; set; } = new AddChatThreadVideoMessageNotification();
    }
    public class AddChatThreadPhotoMessageRequest : BaseChatThreadAddMessageRequest
    {
        public string UgcMediaId { get; set; } = "1";
    }
    public class AddChatThreadPhotoMessageResponse : BaseResponse
    {
        public AddChatThreadPhotoMessageNotification Notification { get; set; } = new AddChatThreadPhotoMessageNotification();
    }
    public class AddChatThreadGameStateMessageRequest : BaseChatThreadAddMessageRequest
    {
        public string GameName { get; set; } = string.Empty;
        public string Payload { get; set; } = "{}";
    }
    public class AddChatThreadGameStateMessageResponse : BaseResponse
    {
        public AddChatThreadGameStateMessageNotification Notification { get; set; } = new AddChatThreadGameStateMessageNotification();
    }


    public class ChatThread
    {
        public long? ChatThreadId { get; set; }
        public string ChatThreadType { get; set; } = ChatThreadTypes.ONE_ON_ONE.ToString();
        public bool? IsTrusted { get; set; }
        public List<User> Members { get; set; } = [];
        public string OfficialAccountId { get; set; } = "OA-72844169130089556";
    }
    public class AddChatThreadMemberListChangedMessageNotification : BaseNotification
    {
        public MemberListChangedChatMessage Message { get; set; } = new MemberListChangedChatMessage();
    }
    public class AddChatThreadPhotoMessageNotification : BaseNotification
    {
        public PhotoChatMessage Message { get; set; } = new PhotoChatMessage();
    }
    public class AddChatThreadStickerMessageNotification : BaseNotification
    {
        public StickerChatMessage Message { get; set; } = new StickerChatMessage();
    }
    public class AddChatThreadTextMessageNotification : BaseNotification
    {
        public TextChatMessage Message { get; set; } = new TextChatMessage();
    }
    public class AddChatThreadVideoMessageNotification : BaseNotification
    {
        public VideoChatMessage Message { get; set; } = new VideoChatMessage();
    }
    public class AddChatThreadGameStateMessageNotification : BaseNotification
    {
        public GameStateChatMessage Message { get; set; } = new GameStateChatMessage();
    }
    public class UpdateChatThreadGameStateMessageNotification : BaseNotification
    {
        public string Result { get; set; } = string.Empty;
        public GameStateChatMessage Message { get; set; } = new GameStateChatMessage();
    }
    public class UpdateChatThreadGroupStatusNotification : BaseNotification
    {
        public long? ChatThreadId { get; set; }
        public bool? IsGroup { get; set; }
    }
    public class AddChatThreadGameEventMessageNotification : BaseNotification
    {
        public GameEventChatMessage Message { get; set; } = new GameEventChatMessage();
    }

    public class ChatThreadNickname
    {
        public long? ChatThreadId { get; set; }
        public string Nickname { get; set; } = "TestPlayer";
    }
    public class AddChatThreadNicknameNotification : BaseNotification
    {
        public ChatThreadNickname ChatThreadNickname { get; set; } = new ChatThreadNickname();
    }
    public class RemoveChatThreadNicknameNotification : BaseNotification
    {
        public long? ChatThreadId { get; set; }
    }

    public class VideoFlavor
    {
        public string VideoFlavorId { get; set; } = string.Empty;
        public int? Bitrate { get; set; }
        public string Format { get; set; } = string.Empty;
        public int? Height { get; set; }
        public int? Width { get; set; }
    }

    public class VideoChatMessage : BaseChatMessage
    {
        public string UgcMediaId = string.Empty;
        public string VideoId = string.Empty;
        public string Caption = string.Empty;
        public PhotoFlavor Thumbnail { get; set; } = new PhotoFlavor();
        public int? Duration { get; set; }
        public List<VideoFlavor> VideoFlavors { get; set; } = [];
    }
    public class TextChatMessage : BaseChatMessage
    {
        public string Text { get; set; } = "Hello World!";
        public bool? IsModerated { get; set; }
    }
    public class StickerChatMessage : BaseChatMessage
    {
        public string ContentId { get; set; } = string.Empty;
    }
    public class PhotoFlavor
    {
        public string PhotoFlavorId { get; set; } = string.Empty;
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string Encoding { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
    public class PhotoChatMessage : BaseChatMessage
    {
        public string UgcMediaId { get; set; } = string.Empty;
        public string PhotoId { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public List<PhotoFlavor> PhotoFlavors { get; set; } = [];
    }
    public class MemberListChangedChatMessage : BaseChatMessage
    {
        public List<string> MemberUserIds { get; set; } = [];
        public bool? IsAdded { get; set; }
    }
    public class GameEventChatMessage : BaseChatMessage
    {
        public long? GameStateMessageId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public string Payload { get; set; } = "{}";
    }
    public class GameStateChatMessage : BaseChatMessage
    {
        public string GameName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    public class PushToken
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
    }
}

namespace Common.Utils.Jgc
{
    //JGC Models
    public class DefaultBaseResponse : GuestControllerWebCallResponse
    {
        public object? data { get; set; }
    }

    public class LogInResponse : GuestControllerWebCallResponse
    {
        public LogInData data { get; set; } = null!;
    }

    public class RefreshData
    {
        public string? etag { get; set; }
        public Token token { get; set; } = new Token();
    }
    public class RefreshResponse : GuestControllerWebCallResponse
    {
        public RefreshData? data { get; set; }
    }

    public class LogInRequest
    {
        public string loginValue { get; set; } = "TestPlayer";
        public string password { get; set; } = "invaild_bcrypt_hash";
    }

    public class ValidateResponse : GuestControllerWebCallResponse
    {
        public object? data { get; set; }
    }

    public class ValidateRequest
    {
        public string email { get; set; } = "test@opencpi.com";
        public string displayName { get; set; } = "TestPlayer";
        public string username { get; set; } = "TestPlayer";
        public string password { get; set; } = "testing123";
    }

    public class RegisterDisplayName
    {
        public string proposedDisplayName { get; set; } = "TestPlayer";
    }

    public class BaseRegisterProfile
    {
        public string dateOfBirth { get; set; } = "1970-01-01";
        public string testProfileFlag { get; set; } = "N";
        public string username { get; set; } = "TestPlayer";
        public string? firstName { get; set; }
        public string? lastName { get; set; }
        public string? email { get; set; }
        public string? parentEmail { get; set; }
        public string languagePreference { get; set; } = "en_US";
        public string region { get; set; } = "US";
    }

    public class ProfileResponse : GuestControllerWebCallResponse
    {
        public ProfileData data { get; set; } = new ProfileData();
    }
    public class ProfileData
    {
        public Profile profile { get; set; } = new Profile();
        public List<MarketingItem> marketing { get; set; } = [];
        public DisplayNameData displayName { get; set; } = new DisplayNameData();
    }
    public class DisplayNameData
    {
        public string? displayName { get; set; }
        public string? proposedDisplayName { get; set; }
        public string proposedStatus { get; set; } = "ACCEPTED";
        public DateTime? moderatedStatusDate { get; set; }
    }

    public class RegisterRequest
    {
        public string password { get; set; } = "testing123";
        public BaseRegisterProfile profile { get; set; } = new BaseRegisterProfile();
        public List<MarketingItem> marketing { get; set; } = [];
        public RegisterDisplayName displayName { get; set; } = new RegisterDisplayName();
        public List<string> legalAssertions { get; set; } = [];
    }

    public class GuestApiErrorCollection
    {
        public string keyCategory { get; set; } = "FAILURE_BY_DESIGN";
        public string? conversationId { get; set; }
        public string? correlationId { get; set; }
        public List<GuestApiError>? errors { get; set; }
    }

    public class GuestApiError
    {
        public string code { get; set; } = "CLIENT_NOT_FOUND";
        public string category { get; set; } = "FAILURE_BY_DESIGN";
        public string? inputName { get; set; }
        public string? errorId { get; set; }
        public string? timestamp { get; set; }
        public TemporaryToken? data { get; set; }
        public string? developerMessage { get; set; }

    }

    public class TemporaryToken
    {
        public string accessToken { get; set; } = "a3974938f39de8e92983847812323a31";
        public string? tokenType { get; set; }
        public uint expiresIn { get; set; } = 0;
        public string swid { get; set; } = "999999999999999";
    }

    public class LogInData
    {
        public string? etag { get; set; } = null;
        public Profile profile { get; set; } = null!;
        public DisplayName displayName { get; set; } = null!;
        public List<MarketingItem> marketing { get; set; } = [];
        public Token token { get; set; } = null!;
    }

    public class Profile
    {
        public string swid { get; set; } = "999999999999999";
        public string username { get; set; } = "TestPlayer";
        public string? prefix { get; set; }
        public string? firstName { get; set; }
        public string? middleName { get; set; }
        public string? lastName { get; set; }
        public string? suffix { get; set; }
        public string languagePreference { get; set; } = "en_US";
        public string? region { get; set; }
        public string? email { get; set; }
        public string? parentEmail { get; set; }
        public string? dateOfBirth { get; set; }
        public string gender { get; set; } = "0";
        public string ageBand { get; set; } = "ADULT";
        public bool ageBandAssumed { get; set; } = true;
        public string countryCodeDetected { get; set; } = "JP";
        public bool emailVerified { get; set; } = false;
        public bool parentEmailVerified { get; set; } = false;
        public string status { get; set; } = "ACTIVE";
        public bool isAdultVerified { get; set; } = false;
    }

    public class DisplayName
    {
        public string? displayName { get; set; }
        public string? proposedDisplayName { get; set; }
        public string proposedStatus { get; set; } = "ACCEPTED";
        public DateTime? moderatedStatusDate { get; set; }
    }

    public class Token
    {
        public string? access_token { get; set; }
        public string? refresh_token { get; set; }
        public string? swid { get; set; }
        public int? ttl { get; set; }
        public long? refresh_ttl { get; set; }
    }

    public class MarketingItem
    {
        public string code { get; set; } = string.Empty;
        public bool subscribed { get; set; } = false;
    }

    public class GuestControllerWebCallResponse
    {
        public GuestApiErrorCollection? error { get; set; }
    }
}
