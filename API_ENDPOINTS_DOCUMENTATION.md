# Club Penguin Island - Web Services API Endpoints Documentation

This document catalogs all REST API endpoints required to recreate the Club Penguin Island web services. The client uses 4 main service hosts (named in the Tweaker Debug Menu):

| Tweaker Name | Setting Key | Description |
|--------------|-------------|-------------|
| **Game** | `CPAPIServicehost` | Main game API - gameplay, quests, inventory, rewards |
| **Login** | `GuestControllerHostUrl` | Disney Guest Controller - authentication, registration |
| **Login Part 2** | `GuestControllerCDNUrl` | Guest Controller CDN/additional auth services |
| **Account** | `MixAPIHostUrl` | Disney Mix API - friends, moderation, notifications |

Additionally:
- **News** (`CPWebsiteAPIServicehost`) - WordPress API for news feed
- **Monitoring** (`CPMonitoringServicehost`) - Diagnostics logging

---

## Table of Contents
1. [Game API (CPAPIServicehost)](#1-game-api-cpapiservicehost)
   - [Player Endpoints](#player-endpoints)
   - [Game/Room Endpoints](#gameroom-endpoints)
   - [Quest Endpoints](#quest-endpoints)
   - [Task Endpoints](#task-endpoints)
   - [Inventory Endpoints](#inventory-endpoints)
   - [Reward Endpoints](#reward-endpoints)
   - [Consumable Endpoints](#consumable-endpoints)
   - [Igloo Endpoints](#igloo-endpoints)
   - [Catalog Endpoints](#catalog-endpoints)
   - [Breadcrumb Endpoints](#breadcrumb-endpoints)
   - [Tutorial Endpoints](#tutorial-endpoints)
   - [Captcha Endpoints](#captcha-endpoints)
   - [Chat Endpoints](#chat-endpoints)
   - [Minigame Endpoints](#minigame-endpoints)
   - [Moderation Endpoints](#moderation-endpoints)
   - [Disney Store Endpoints](#disney-store-endpoints)
   - [IAP Endpoints](#iap-endpoints)
   - [Tube Endpoints](#tube-endpoints)
   - [Event Endpoints](#event-endpoints)
   - [Encryption Endpoints](#encryption-endpoints)
   - [Membership Endpoints](#membership-endpoints)
   - [QA/Debug Endpoints](#qadebug-endpoints)
2. [Guest Controller API (GuestControllerHostUrl)](#2-guest-controller-api-guestcontrollerhosturl)
3. [Mix API (MixAPIHostUrl)](#3-mix-api-mixapihosturl)
4. [Website API (CPWebsiteAPIServicehost)](#4-website-api-cpwebsiteapiservicehost)
5. [Monitoring API (CPMonitoringServicehost)](#5-monitoring-api-cpmonitoringservicehost)

---

## 1. Game API (CPAPIServicehost)

Base URI configuration key: `cp-api-base-uri`

### Player Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `GET` | `/player/v1/` | `GetLocalPlayerDataOperation` | Get current player's full data (quests, inventory, mascotXP, etc.) |
| `POST` | `/player/v1/profile` | `SetProfileOperation` | Update player profile (color, etc.) |
| `POST` | `/player/v1/outfit` | `SetOutfitOperation` | Save equipped outfit |
| `POST` | `/player/v1/displayName` | - | Set display name (during tutorial) |
| `GET` | `/player/v1/durable/equip/{type}` | `EquipDurableOperation` | Equip a durable prop (jackhammer, etc.) |
| `GET` | `/player/v1/preregistration` | `GetPreregistrationDataOperation` | Get preregistration status |
| `POST` | `/player/v1/preregistration` | `PreregisterOperation` | Submit preregistration |
| `PUT` | `/player/v1/referral` | `SetReferralOperation` | Set referral display name |
| `POST` | `/player/v1/legacy/migrate` | `MigrateLegacyAccountOperation` | Migrate legacy CP account |
| `GET` | `/player/v1/id/{swid}` | `GetOtherPlayerDataBySwidOperation` | Get other player by SWID |
| `POST` | `/player/v1/id` | `GetOtherPlayerDataBySwidsOperation` | Get multiple players by SWIDs |
| `GET` | `/player/v1/name/{displayName}` | `GetOtherPlayerDataByDisplayNameOperation` | Get player by display name |
| `POST` | `/player/v1/names` | `GetOtherPlayerDataByDisplayNamesOperation` | Get multiple players by names |
| `GET` | `/player/v1/session/{sessionId}` | `GetOtherPlayerDataBySessionIdOperation` | Get player by session ID |
| `POST` | `/player/v1/id/online` | `GetOnlinePlayersBySwidsOperation` | Get online players by SWIDs |

### Game/Room Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `GET` | `/game/v1/rooms` | `GetRoomsOperation` | List available rooms (with language, worlds, rooms, limit params) |
| `POST` | `/game/v1/worlds/{world}/language/{language}/rooms/{room}/players` | `PostRoomPlayersOperation` | Join a room - returns game server connection info |
| `POST` | `/game/v1/igloos/language/{language}/players` | `PostIglooPlayersOperation` | Join an igloo |
| `DELETE` | `/game/v1/session/{sessionId}` | `DeleteSessionOperation` | End session / leave room |
| `GET` | `/game/v1/roomPopulation/{world}` | `GetRoomPopulationOperation` | Get room populations for a world |
| `GET` | `/game/v1/world/roomPopulation/{room}` | `GetRoomPopulationForWorldsOperation` | Get room population across worlds |

### Quest Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `POST` | `/quest/v1/progress` | `SetProgressOperation` | Update quest objectives (SignedResponse) |
| `POST` | `/quest/v1/{questId}` | `SetStatusOperation` | Set quest status (body is status number as text) |

### Task Endpoints (Daily Challenges)

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `POST` | `/task/v1` | `SetTaskProgressOperation` | Update daily task progress |
| `POST` | `/task/v1/reward` | `ClaimTaskRewardOperation` | Claim daily task reward (body is taskId as text) |

### Inventory Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `GET` | `/inventory/v1/equipment` | `GetInventoryOperation` | Get player's custom equipment |
| `POST` | `/inventory/v1/equipment` | `CreateCustomEquipmentOperation` | Create new custom equipment |
| `DELETE` | `/inventory/v1/equipment/{equipmentId}` | `DeleteCustomEquipmentOperation` | Delete custom equipment |
| `GET` | `/inventory/v1/outfits` | `GetSavedOutfitsOperation` | Get saved outfit slots |
| `POST` | `/inventory/v1/outfit` | `SaveSavedOutfitOperation` | Save outfit to slot |
| `DELETE` | `/inventory/v1/outfit/{outfitSlot}` | `DeleteSavedOutfitOperation` | Delete saved outfit |

### Reward Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `PUT` | `/reward/v1` | `AddRewardOperation` | Add rewards (from game server events) |
| `POST` | `/reward/v1/room` | `AddRoomRewardsOperation` | Add in-room rewards |
| `PUT` | `/reward/v1/claim/{type}` | `ClaimRewardOperation` | Claim a reward by type/ID |
| `GET` | `/reward/v1/preregistration` | `ClaimPreregistrationRewardOperation` | Claim preregistration rewards |
| `POST` | `/reward/v1/claimServerAdded` | `ClaimServerAddedRewardsOperation` | Claim server-added rewards (welcome kit) |
| `POST` | `/reward/v1/claimDailySpin` | `ClaimDailySpinRewardOperation` | Claim daily spin reward |
| `POST` | `/reward/v1/claimQuickNotification` | `ClaimQuickNotificationRewardOperation` | Claim quick notification reward |
| `POST` | `/reward/v1/exchangeall` | `ExchangeAllForCoinsOperation` | Exchange items for coins |
| `GET` | `/reward/v1/calculateexchangeall` | `CalculateExchangeForCoinsOperation` | Calculate exchange value |

### Consumable Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `GET` | `/consumable/v1` | `GetConsumableInventoryOperation` | Get consumable inventory |
| `PUT` | `/consumable/v1/{type}/{count}` | `PurchaseConsumableOperation` | Purchase consumables |
| `DELETE` | `/consumable/v1/{type}` | `UseConsumableOperation` | Use a consumable |
| `POST` | `/consumable/v1/partial` | `StorePartialConsumableOperation` | Store partial consumable usage |

### Igloo Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `PUT` | `/igloo/v1` | `UpdateIglooDataOperation` | Update igloo data (visibility, active layout) |
| `POST` | `/igloo/v1/layout` | `CreateIglooLayoutOperation` | Create new igloo layout |
| `GET` | `/igloo/v1/layout/{sceneLayoutId}` | `GetIglooLayoutOperation` | Get specific layout |
| `PUT` | `/igloo/v1/layout/{sceneLayoutId}` | `UpdateIglooLayoutOperation` | Update existing layout |
| `DELETE` | `/igloo/v1/layout/{sceneLayoutId}` | `DeleteIglooLayoutOperation` | Delete layout |
| `POST` | `/igloo/v1/iglooId/layout/active` | `GetActiveIglooLayoutOperation` | Get active igloo layout for player |
| `GET` | `/igloo/v1/decorations` | `GetDecorationsOperation` | Get player's decorations |
| `POST` | `/igloo/v1/decorations/{type}/{definitionId}` | `CreateDecorationOperation` | Create decoration |
| `POST` | `/igloo/v1/decorations/{decorationId}/increment/{count}` | `PurchaseDecorationOperation` | Purchase decoration |
| `DELETE` | `/igloo/v1/decorations/{decorationId}` | `DeleteDecorationOperation` | Delete decoration |
| `GET` | `/igloo/v1/igloos/friends` | `GetFriendsIgloosOperation` | Get friends' igloos |
| `GET` | `/igloo/v1/igloos/popular` | `GetPopularIgloosOperation` | Get popular igloos |
| `POST` | `/igloo/v1/igloos/populations/zoneIds` | `GetIglooPopulationsByZoneIdsOperation` | Get igloo populations |

### Catalog Endpoints (Clothing Catalog / Daily Challenges)

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `GET` | `/catalog/v1/clothing/themes/stats` | `GetCurrentThemeOperation` | Get current theme challenge stats |
| `GET` | `/catalog/v1/clothing/user/stats` | `GetUserStatsOperation` | Get user catalog stats |
| `POST` | `/catalog/v1/clothing/items/{catalogSection}` | `BaseCatalogSectionOperation` | Get catalog items by section (recent, popular, friends, category) |
| `POST` | `/catalog/v1/clothing/purchase` | `ItemPurchaseOperation` | Purchase clothing item |
| `POST` | `/catalog/v1/clothing/submit` | `ItemSubmissionOperation` | Submit item to catalog |

### Breadcrumb Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `POST` | `/breadcrumb/v1/breadcrumb` | `AddBreadcrumbIdsOperation` | Add breadcrumbs (UI hints) |
| `POST` | `/breadcrumb/v1/removebreadcrumbs` | `RemoveBreadcrumbIdsOperation` | Remove breadcrumbs |

### Tutorial Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `POST` | `/tutorial/v1/tutorial` | `SetTutorialOperation` | Mark tutorial step complete |

### Captcha Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `GET` | `/captcha/v1` | `GetCaptchaOperation` | Get captcha challenge (type, width, height params) |
| `POST` | `/captcha/v1/solution` | `PostCaptchaSolutionOperation` | Submit captcha solution |

### Chat Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `POST` | `/chat/v1/message/verify` | `VerifyChatMessageOperation` | Verify/moderate chat message |

### Minigame Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `POST` | `/minigame/v1/fishing/cast` | `FishingCastOperation` | Cast fishing rod |
| `POST` | `/minigame/v1/fishing/catch` | `FishingCatchOperation` | Catch fish |

### Moderation Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `POST` | `/moderation/v1/report/name/{displayName}` | `ReportPlayerOperation` | Report a player |

### Disney Store Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `PUT` | `/disneystore/v1/purchase/{type}/{count}` | `PurchaseDisneyStoreItemOperation` | Purchase Disney Store item |

### IAP Endpoints (In-App Purchases)

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `GET` | `/iap/v1/products` | `GetProductsOperation` | Get IAP products |
| `POST` | `/iap/v1/purchase` | `PurchaseOperation` | Process purchase |
| `POST` | `/iap/v1/checkrestore` | `CheckRestoreOperation` | Check restore status |
| `POST` | `/iap-pc/v1/session` | `PCSessionStartOperation` | Start PC IAP session |
| `POST` | `/iap-pc/v1/products` | `PCGetProductDetailsOperation` | Get PC product details |

### Tube Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `PUT` | `/tube/v1/equip/{type}` | `EquipTubeOperation` | Equip snow tube |

### Event Endpoints (Scheduled Events)

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `GET` | `/event/v1/cfc` | `GetCFCDonationsOperation` | Get CFC (Coins for Change) donations |
| `POST` | `/event/v1/cfc/{coins}` | `PostCFCDonationOperation` | Make CFC donation |

### Encryption Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `POST` | `/encryption-trusted/v1/encryptionKey` | `GetEncryptionKeyOperation` | Get encryption key for secure requests |

### Membership Endpoints

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `POST` | `/membership/v1/qa/membershipgrant` | `QAMembershipGrantOperation` | Grant membership (QA) |

### QA/Debug Endpoints

These endpoints are prefixed with `/qa/` and are used for testing:

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `DELETE` | `/tutorial/v1/qa/cleartutorial` | `ClearTutorialOperation` | Clear tutorial progress |
| `DELETE` | `/breadcrumb/v1/qa/clearbreadcrumbs` | `QAClearBreadcrumbsOperation` | Clear all breadcrumbs |
| `DELETE` | `/quest/v1/qa/{questId}` | `QAClearQuestOperation` | Clear quest progress |
| `POST` | `/quest/v1/qa/progress` | `QASetProgressOperation` | Set quest progress |
| `POST` | `/quest/v1/qa/{questId}` | `QASetStatusOperation` | Set quest status |
| `DELETE` | `/quest/v1/qa/locks/{questId}` | `QAUnlockQuestOperation` | Unlock quest |
| `POST` | `/task/v1/qa` | `QASetTaskProgressOperation` | Set task progress |
| `DELETE` | `/minigame/v1/qa/progress` | `QAClearMinigameProgressOperation` | Clear minigame progress |
| `DELETE` | `/reward/v1/qa/rooms` | `ClearRoomRewardsOperation` | Clear room rewards |
| `POST` | `/reward/v1/qa` | `QASetRewardOperation` | Set rewards |
| `PUT` | `/reward/v1/qa/claim/{type}/{time}` | `QAClaimRewardOperation` | Claim reward with timestamp |
| `POST` | `/reward/v1/qa/dailySpin` | `QASetDailySpinDataOperation` | Set daily spin data |
| `POST` | `/consumable/v1/qa/{type}` | `QASetTypeCountOperation` | Set consumable count |
| `DELETE` | `/inventory/v1/qa/equipment` | `QADeleteAllEquipmentOperation` | Delete all equipment |
| `POST` | `/igloo/v1/qa/decorations/{type}/{definitionId}` | `QACreateDecorationOperation` | Create decoration |
| `DELETE` | `/igloo/v1/qa/decorations` | `QADeleteAllDecorationsOperation` | Delete all decorations |
| `DELETE` | `/igloo/v1/qa/layouts` | `QADeleteAllIgloosOperation` | Delete all igloo layouts |
| `PUT` | `/disneystore/v1/qa/purchase/{type}/{count}/{time}` | `QAPurchaseDisneyStoreItemOperation` | QA purchase with timestamp |

---

## 2. Guest Controller API (GuestControllerHostUrl)

Base path: `/jgc/v5`

These endpoints handle Disney account authentication and management.

### Authentication Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/client/{client-id}/api-key` | Get API key for authentication |
| `POST` | `/client/{client-id}/guest/login?expand=profile,displayName,marketing` | Login with credentials |
| `POST` | `/client/{client-id}/guest/refresh-auth/{refresh-token}` | Refresh authentication token |
| `POST` | `/client/{client-id}/guest/register[?autogenerateUsername=true]` | Register new account |

### Configuration Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/client/{client-id}/configuration/site[?countryCode=XX]` | Get site configuration |

### Validation Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/client/{client-id}/validate` | Validate registration data |

### Profile Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/client/{client-id}/guest/{swid}` | Get profile |
| `POST` | `/client/{client-id}/guest/{swid}` | Update profile |

### Recovery Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/client/{client-id}/notification/recover-password[?langPref=XX]` | Send password recovery |
| `POST` | `/client/{client-id}/notification/recover-username[?langPref=XX]` | Send username recovery |
| `POST` | `/client/{client-id}/notification/resolve-mase[?langPref=XX]` | Resolve multiple account |
| `POST` | `/client/{client-id}/notification/upgrade-nrt[?langPref=XX]` | Upgrade NRT account |
| `POST` | `/client/{client-id}/notification/parental-approval/{swid}[?langPref=XX]` | Send parental approval email |
| `POST` | `/client/{client-id}/notification/email-verification[?langPref=XX]` | Send verification email |

### Adult Verification Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/client/{client-id}/guest/{swid}/adult-verification-status` | Get verification status |
| `POST` | `/client/{client-id}/guest/{swid}/adult-verification-ssn` | Verify adult (US SSN) |
| `POST` | `/client/{client-id}/guest/{swid}/adult-verification-quiz` | Verify adult (quiz) |

### Guardian/Child Linking Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/client/{client-id}/guardian/{swid}/children` | Get claimable children |
| `GET` | `/client/{client-id}/guardian/{swid}/children/linked` | Get linked children |
| `POST` | `/client/{client-id}/guardian/{swid}/children/child/{childSwid}` | Link child |
| `PUT` | `/client/{client-id}/guardian/{swid}/children` | Link claimable children |
| `GET` | `/client/{client-id}/guardian/guardians/child/{swid}` | Get guardians |

### Permission Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/client/{client-id}/guest/activity-permission/{swid}` | Get permissions |
| `POST` | `/client/{client-id}/guest/activity-permission/{swid}` | Request permission |
| `PUT` | `/client/{client-id}/guest/activity-permission/{childSwid}` | Approve permission |

---

## 3. Mix API (MixAPIHostUrl)

These endpoints handle friend management, notifications, and moderation.

### User/Search Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/users/byDisplayName` | Get users by display name |
| `POST` | `/users/byUserId` | Get users by user ID |
| `POST` | `/search/displayname` | Search for display names |

### State Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/state` | Get full user state (friends, invitations, alerts) |
| `PUT` | `/presence` | Set presence status |
| `POST` | `/presence` | Get presence status |

### Alert Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `PUT` | `/alert/clear` | Clear alerts |

### Display Name Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `PUT` | `/displayname` | Set display name |
| `POST` | `/displayname/validate` | Validate display names (batch) |
| `POST` | `/displayname/validate/v2` | Validate single display name |

### Friendship Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `PUT` | `/friendship` | Add friendship (accept invitation) |
| `POST` | `/friendship/delete` | Remove friendship |
| `POST` | `/friendship/recommend` | Get friend recommendations |
| `PUT` | `/friendship/invitation` | Send friend invitation |
| `POST` | `/friendship/invitation/delete` | Remove friend invitation |
| `POST` | `/friendship/trust/delete` | Remove friendship trust |

### Notification Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/notifications` | Get notifications |
| `POST` | `/notifications/sinceSequence` | Get notifications since sequence |

### Session Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `PUT` | `/session/user` | Start user session |
| `POST` | `/session/user/delete` | Delete user session |

### Moderation Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `PUT` | `/moderation/reportPlayer` | Report a player |
| `PUT` | `/moderation/text` | Moderate text (chat filter) |

### Push Notification Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/pushNotificationsSetting` | Toggle push notification |
| `POST` | `/pushNotificationsSetting/delete` | Delete push notification setting |

### Geolocation Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/geolocation` | Get geolocation |

### Language Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/languagePreference` | Set language preference |

### Registration Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/registration/text` | Get registration text |

### PIL Check Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/pil/check` | Check PIL status |

### Integration Test Support (Debug)

| Method | Path | Description |
|--------|------|-------------|
| `PUT` | `/integrationTestSupport/moderation/tempBan` | Temp ban for testing |
| `POST` | `/integrationTestSupport/user/alert` | Trigger alert |
| `POST` | `/integrationTestSupport/user/anonymize` | Anonymize user |
| `POST` | `/integrationTestSupport/user/notificationCounter/delete` | Delete notification counter |
| `POST` | `/integrationTestSupport/user/session/expire` | Expire session |

---

## 4. Website API (CPWebsiteAPIServicehost)

Base URI configuration key: `cp-website-api-base-uri`

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `GET` | `/wp-json/snowflake/v1/social` | `GetLatestPostTimeOperation` | Get latest blog post time |

---

## 5. Monitoring API (CPMonitoringServicehost)

Base URI configuration key: `cp-monitoring-base-uri`

| Method | Path | Operation | Description |
|--------|------|-----------|-------------|
| `POST` | `/diagnostics/v1/log` | `PostDiagnosticsLogOperation` | Send diagnostics logs |

---

## Request/Response Headers

### Standard Request Headers

```
Authorization: GAE {cp-api-client-token}
X-CP-Client-Version: {version}
X-CP-Content-Version: {live|qa|stage}
X-CP-Sub-Content-Version: {date}
Content-Type: application/json
Cache-Control: no-cache, no-store, must-revalidate
```

### Encrypted Requests (Marked with `[Encrypted]` attribute)

```
X-Encryption-Key-Id: {key-id}
```

### Guest Controller Headers

```
Authorization: APIKEY {api-key}  (for API key auth)
Authorization: BEARER {access-token}  (for access token auth)
Content-Type: application/json
Accept: application/json
Cache-Control: no-cache,no-store,must-revalidate
```

### Mix API Headers

```
Content-Type: application/json  (or encrypted content type)
X-Mix-OneIdToken: {guest-controller-access-token}
X-Mix-ClientToken: {mix-client-token}
X-Mix-UserSessionId: {session-id}  (if encrypted session active)
Cache-Control: no-cache,no-store,must-revalidate
```

---

## Common Response Wrapper Types

### SignedResponse<T>
Used for responses that need verification:
```json
{
  "goodUntil": 1704293600000,
  "swid": "{SWID}",
  "data": { ... },
  "signature": "..."
}
```

### QuestChangeResponse
Used for quest status updates:
```json
{
  "questId": "AAC001",
  "questStateCollection": { /* SignedResponse<List<QuestState>> */ },
  "reward": { /* optional */ }
}
```

### PlayerAssets
Common player assets structure:
```json
{
  "coins": 1000,
  "mascotXP": { "AuntArctic": 1000000 },
  "collectibleCurrencies": {},
  "colourPacks": [],
  "decals": [],
  "fabrics": [],
  "emotePacks": [],
  "sizzleClips": [],
  "equipmentTemplates": [],
  "lots": [],
  "decorations": [],
  "structures": [],
  "musicTracks": [],
  "lighting": [],
  "durables": [15],
  "partySupplies": [],
  "tubes": [],
  "savedOutfitSlots": 0
}
```

---

## Implementation Notes

1. **Unified Server**: All 4 APIs can be hosted on a single server with different base paths:
   - `/api/cp-api/` for Game API
   - `/jgc/v5/` for Guest Controller
   - `/api/mix/` for Mix API
   - `/wp-json/` for Website API

2. **Authentication Flow**:
   1. Client calls Guest Controller `/api-key` to get API key
   2. Client calls Guest Controller `/guest/login` with credentials
   3. Receives `access_token` and `refresh_token`
   4. All subsequent Mix API calls use `X-Mix-OneIdToken` header
   5. All subsequent CP API calls use `Authorization: GAE {token}` header

3. **Session Management**:
   - The `sessionId` returned from `/game/v1/worlds/.../players` is used for game server connection
   - This session ID is passed to the SmartFox game server for authentication

4. **Signed Responses**:
   - Many endpoints return `SignedResponse<T>` wrappers
   - The `signature` field can be any string for private servers
   - `goodUntil` is a timestamp for cache expiration

5. **Empty Request Bodies**:
   - POST/PUT/DELETE requests must have `{}` as minimum body
   - The client adds this automatically if body is empty

---

## Endpoint Priority for Basic Gameplay

Essential endpoints for basic single-player functionality:

1. **Login/Session**:
   - Guest Controller: `/guest/login`, `/guest/refresh-auth`
   - Mix API: `/session/user`, `/state`

2. **Player Data**:
   - `/player/v1/` (GET - load player)
   - `/player/v1/profile` (POST - save profile)
   - `/player/v1/outfit` (POST - save outfit)

3. **Joining Rooms**:
   - `/game/v1/rooms` (GET - list rooms)
   - `/game/v1/worlds/{world}/language/{language}/rooms/{room}/players` (POST - join)

4. **Inventory/Equipment**:
   - `/inventory/v1/equipment` (GET/POST/DELETE)

5. **Quests**:
   - `/quest/v1/{questId}` (POST - update status)
   - `/quest/v1/progress` (POST - update objectives)

6. **Rewards**:
   - `/reward/v1` (PUT - add rewards)
   - `/reward/v1/claim/{type}` (PUT - claim)
   - `/reward/v1/claimServerAdded` (POST - welcome kit)

7. **Tutorial**:
   - `/tutorial/v1/tutorial` (POST)
