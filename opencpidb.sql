/*
 Navicat Premium Dump SQL

 Source Server         : localhost_3306
 Source Server Type    : MySQL
 Source Server Version : 110202 (11.2.2-MariaDB)
 Source Host           : localhost:3306
 Source Schema         : opencpidb

 Target Server Type    : MySQL
 Target Server Version : 110202 (11.2.2-MariaDB)
 File Encoding         : 65001

 Date: 01/05/2026 14:30:22
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for accounts
-- ----------------------------
DROP TABLE IF EXISTS `accounts`;
CREATE TABLE `accounts`  (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `swid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '999999999999999',
  `referenceid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001',
  `email` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '@',
  `penguinword` varchar(60) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'invaild_bcrypt',
  `status` enum('DELETED','BANNED','LOCKED','ACTIVE','NO_EMAIL_VERFIED','NOT_ACTIVE','PERMA') CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'ACTIVE',
  `username` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'TestPlayer',
  `displayname` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'TestPlayer',
  `proposedstatus` enum('NONE','NOT_ACCPETED','ON_PROGRESS','ACCPETED') CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'NONE',
  `registeredday` varchar(28) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '2017-03-31T12:05:35.070Z',
  `gender` tinyint NOT NULL DEFAULT 0,
  `ageassume` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'CHILD',
  `birthday` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '1970-01-01',
  `region` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'US',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 29 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for alerts
-- ----------------------------
DROP TABLE IF EXISTS `alerts`;
CREATE TABLE `alerts`  (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `swid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '999999999999999',
  `level` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'CYBERCRIMINAL',
  `message` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'Hello this is a test Alert Message',
  `timestamp` bigint NOT NULL DEFAULT 0,
  `hasseen` tinyint NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 2 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for chatthreadchats
-- ----------------------------
DROP TABLE IF EXISTS `chatthreadchats`;
CREATE TABLE `chatthreadchats`  (
  `id` bigint NOT NULL DEFAULT 1,
  `msgid` bigint NOT NULL AUTO_INCREMENT,
  `sessionid` bigint NOT NULL DEFAULT 1,
  `timestamp` bigint NOT NULL DEFAULT 0,
  `swid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '999999999999999',
  `isadded` tinyint NOT NULL DEFAULT 0,
  `nickname` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'TestPlayer',
  `contentid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `photoflavorid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `videoflavorid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `gamename` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `gamestate` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `text` varchar(100) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'Hello World!',
  `ismoderated` tinyint NOT NULL DEFAULT 0,
  `istext` tinyint NOT NULL DEFAULT 0,
  `isgag` tinyint NOT NULL DEFAULT 0,
  `isticker` tinyint NOT NULL DEFAULT 0,
  `isvideo` tinyint NOT NULL DEFAULT 0,
  `isphoto` tinyint NOT NULL DEFAULT 0,
  `isgame` tinyint NOT NULL DEFAULT 0,
  `isgameevent` tinyint NOT NULL DEFAULT 0,
  PRIMARY KEY (`msgid`) USING BTREE,
  INDEX `id`(`id` ASC) USING BTREE,
  CONSTRAINT `id` FOREIGN KEY (`id`) REFERENCES `chatthreads` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 42 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for chatthreads
-- ----------------------------
DROP TABLE IF EXISTS `chatthreads`;
CREATE TABLE `chatthreads`  (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `type` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'ONE_ON_ONE',
  `timestamp` bigint NOT NULL DEFAULT 0,
  `istrusted` tinyint NOT NULL DEFAULT 0,
  `isgroup` tinyint NOT NULL DEFAULT 0,
  `messagecounts` bigint NOT NULL DEFAULT 0,
  `expires_at` datetime NULL DEFAULT (current_timestamp() + interval 30 day),
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 2 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for chatthreadsusers
-- ----------------------------
DROP TABLE IF EXISTS `chatthreadsusers`;
CREATE TABLE `chatthreadsusers`  (
  `id` bigint NOT NULL DEFAULT 1,
  `swid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '999999999999999',
  `displayname` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'TestPlayer',
  `isowner` tinyint NOT NULL DEFAULT 0,
  `ismuted` tinyint NOT NULL DEFAULT 0,
  `iskicked` tinyint NOT NULL DEFAULT 0,
  INDEX `id2`(`id` ASC) USING BTREE,
  CONSTRAINT `id2` FOREIGN KEY (`id`) REFERENCES `chatthreads` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for clothingcatalogthemeinvcostus
-- ----------------------------
DROP TABLE IF EXISTS `clothingcatalogthemeinvcostus`;
CREATE TABLE `clothingcatalogthemeinvcostus`  (
  `clothingcatalogitemid` bigint NOT NULL DEFAULT 0,
  `swid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `definitionid` int NOT NULL DEFAULT 0,
  `slotindex` int NOT NULL DEFAULT 1,
  `partindex` int NOT NULL DEFAULT 0,
  `type` int NOT NULL DEFAULT 1,
  `scale` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `rotation` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `uoffset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `voffset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `repeatt` tinyint NOT NULL DEFAULT 0,
  INDEX `id`(`clothingcatalogitemid` ASC) USING BTREE,
  INDEX `pikachu`(`swid` ASC, `clothingcatalogitemid` ASC) USING BTREE,
  CONSTRAINT `clothingcatalogthemeinvcostus_ibfk_1` FOREIGN KEY (`swid`, `clothingcatalogitemid`) REFERENCES `inventorys` (`swid`, `id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for clothingcatalogthemeinvs
-- ----------------------------
DROP TABLE IF EXISTS `clothingcatalogthemeinvs`;
CREATE TABLE `clothingcatalogthemeinvs`  (
  `scheduledthemechallengeid` bigint NOT NULL DEFAULT 578,
  `equipmentcategory` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'Feet',
  `clothingcatalogitemid` bigint NOT NULL AUTO_INCREMENT,
  `swid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '999999999999999',
  `creatorname` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'TestPlayer',
  `price` bigint NOT NULL DEFAULT 40,
  `numbersold` bigint NOT NULL DEFAULT 0,
  `definitionid` int NOT NULL DEFAULT 1,
  `equipmentid` bigint NOT NULL DEFAULT 0,
  `datetimecreated` bigint NOT NULL DEFAULT 0,
  PRIMARY KEY (`clothingcatalogitemid`) USING BTREE,
  INDEX `swid`(`swid` ASC, `clothingcatalogitemid` ASC) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 13 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for clothingcatalogthemes
-- ----------------------------
DROP TABLE IF EXISTS `clothingcatalogthemes`;
CREATE TABLE `clothingcatalogthemes`  (
  `scheduledthemechallengeid` bigint NOT NULL DEFAULT 578,
  `usersubmissionclothingcatalogid` bigint NOT NULL DEFAULT 0,
  `coins` bigint NOT NULL DEFAULT 20
) ENGINE = InnoDB CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for friends
-- ----------------------------
DROP TABLE IF EXISTS `friends`;
CREATE TABLE `friends`  (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `meID` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '999999999999999',
  `hisID` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'Fake Friend',
  `username` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'TestPlayer',
  `hisusername` varchar(13) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'Mix Bot',
  `istrusted` tinyint NOT NULL DEFAULT 0,
  `isfriend` tinyint NOT NULL DEFAULT 0,
  `isrequested` tinyint NOT NULL DEFAULT 1,
  `isdeleted` tinyint NOT NULL DEFAULT 0,
  `timestamp` bigint NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_friends_me_lookup`(`meID` ASC, `isfriend` ASC, `isdeleted` ASC) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 37 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for inventorycostumes
-- ----------------------------
DROP TABLE IF EXISTS `inventorycostumes`;
CREATE TABLE `inventorycostumes`  (
  `id` bigint NOT NULL DEFAULT 0,
  `swid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `definitionid` int NOT NULL DEFAULT 0,
  `slotindex` int NOT NULL DEFAULT 1,
  `partindex` int NOT NULL DEFAULT 0,
  `type` int NOT NULL DEFAULT 1,
  `scale` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `rotation` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `uoffset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `voffset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `repeatt` tinyint NOT NULL DEFAULT 0,
  INDEX `id`(`id` ASC) USING BTREE,
  INDEX `pikachu`(`swid` ASC, `id` ASC) USING BTREE,
  CONSTRAINT `pikachu` FOREIGN KEY (`swid`, `id`) REFERENCES `inventorys` (`swid`, `id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for inventorys
-- ----------------------------
DROP TABLE IF EXISTS `inventorys`;
CREATE TABLE `inventorys`  (
  `swid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '999999999999999',
  `id` bigint NOT NULL AUTO_INCREMENT,
  `sourceid` int NOT NULL DEFAULT 0,
  `definitionid` int NOT NULL DEFAULT 1,
  `source` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'None',
  `datetimecreated` bigint NOT NULL DEFAULT 0,
  `iswearing` tinyint NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `swid`(`swid` ASC, `id` ASC) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 12 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for profiles
-- ----------------------------
DROP TABLE IF EXISTS `profiles`;
CREATE TABLE `profiles`  (
  `swid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '{00000000-0000-0000-0000-000000000000}',
  `registertimestamp` bigint NOT NULL DEFAULT 0,
  `langid` smallint NOT NULL DEFAULT 1,
  `lang` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'en-US',
  `lastscene` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'DefaultIgloo',
  `lastworld` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'Alpine',
  `instance` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `colour` int NOT NULL DEFAULT 0,
  `auntarctic` int NOT NULL DEFAULT 0,
  `djcadence` int NOT NULL DEFAULT 0,
  `rockhopper` int NOT NULL DEFAULT 0,
  `rookie` int NOT NULL DEFAULT 0,
  `coins` bigint NOT NULL DEFAULT 250,
  `chocolite` bigint NOT NULL DEFAULT 0,
  `coconut` bigint NOT NULL DEFAULT 0,
  `coin` bigint NOT NULL DEFAULT 0,
  `confettiflower` bigint NOT NULL DEFAULT 0,
  `crystalcluster` bigint NOT NULL DEFAULT 0,
  `crystalice` bigint NOT NULL DEFAULT 0,
  `crystalsmall` bigint NOT NULL DEFAULT 0,
  `crystalsnowflake` bigint NOT NULL DEFAULT 0,
  `flourflower` bigint NOT NULL DEFAULT 0,
  `pearl` bigint NOT NULL DEFAULT 0,
  `rainbowdrop` bigint NOT NULL DEFAULT 0,
  `seasponge` bigint NOT NULL DEFAULT 0,
  `seaweed` bigint NOT NULL DEFAULT 0,
  `shell` bigint NOT NULL DEFAULT 0,
  `windchime` bigint NOT NULL DEFAULT 0,
  `member` tinyint NOT NULL DEFAULT 1,
  `selectedTubeId` int NOT NULL DEFAULT 0,
  `tutorialsteps` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `Accessory` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '3',
  `AccessoryTintIndex` int NOT NULL DEFAULT 17,
  `AccessoryXOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `AccessoryYOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `Costume` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '15',
  `CostumeTintIndex` int NOT NULL DEFAULT 0,
  `CostumeXOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `CostumeYOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `Brow` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '9',
  `BrowTintIndex` int NOT NULL DEFAULT 0,
  `BrowXOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `BrowYOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.00417972784489393',
  `Eyes` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '54',
  `EyesTintIndex` int NOT NULL DEFAULT 11,
  `EyesXOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `EyesYOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0121355056762695',
  `Hair` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '69',
  `HairTintIndex` int NOT NULL DEFAULT 7,
  `HairXOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `HairYOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `Nose` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '106',
  `NoseTintIndex` int NOT NULL DEFAULT 0,
  `NoseXOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `NoseYOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0116169452667236',
  `Mouth` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '194',
  `MouthTintIndex` int NOT NULL DEFAULT 0,
  `MouthXOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '-0.00126063823699951',
  `MouthYOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `Skin` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '123',
  `SkinTintIndex` int NOT NULL DEFAULT 9,
  `SkinXOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `SkinYOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `Hat` varchar(4) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0',
  `HatTintIndex` int NOT NULL DEFAULT 0,
  `HatXOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  `HatYOffeset` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0.0',
  PRIMARY KEY (`swid`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for quests
-- ----------------------------
DROP TABLE IF EXISTS `quests`;
CREATE TABLE `quests`  (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `swid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '999999999999999',
  `questId` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'AAC001Q001LeakyShip',
  `status` int NOT NULL DEFAULT 0,
  `completedObjectives` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `timesCompleted` tinyint NOT NULL DEFAULT 0,
  `unlockTime` varchar(13) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 17 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for room_users
-- ----------------------------
DROP TABLE IF EXISTS `room_users`;
CREATE TABLE `room_users`  (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `currentroomid` bigint NULL DEFAULT 1,
  `displayname` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `sess` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '1',
  `tube` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '0',
  `colour` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT '0',
  `outfit` longblob NULL,
  `on_quest` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `proto` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `con` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `bubble` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `con_prop` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `loc` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `awayfromkeyboard` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `tempheadstatus` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 2 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for rooms
-- ----------------------------
DROP TABLE IF EXISTS `rooms`;
CREATE TABLE `rooms`  (
  `id` bigint NOT NULL DEFAULT 0,
  `name` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'Lobby',
  `owner` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `password` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '',
  `groupid` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'default',
  `maxuser` int NOT NULL DEFAULT 39,
  `maxspectators` int NOT NULL DEFAULT 100,
  `maxroomvariables` int NOT NULL DEFAULT 50,
  `isdynamic` tinyint NOT NULL DEFAULT 0,
  `ishidden` tinyint NOT NULL DEFAULT 0,
  `isgame` tinyint NOT NULL DEFAULT 0,
  `roomrights` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT 'USER_VARIABLES_UPDATE_EVENT,USER_ENTER_EVENT,USER_EXIT_EVENT,USER_COUNT_CHANGE_EVENT',
  `defaultaoi` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '900,900,900',
  `lowermaplimit` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '-900.00,-900.00,-900.00',
  `highermaplimit` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '900, 900, 900',
  `defaultspawn` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '10.68, 0.3953104, -13.38',
  `usercount` int NOT NULL DEFAULT 0,
  `expires_at` datetime NULL DEFAULT (current_timestamp() + interval 120 minute)
) ENGINE = InnoDB CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for sessions
-- ----------------------------
DROP TABLE IF EXISTS `sessions`;
CREATE TABLE `sessions`  (
  `seeid` bigint NOT NULL AUTO_INCREMENT,
  `session_id` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `expires_at` datetime NULL DEFAULT (current_timestamp() + interval 1 day),
  PRIMARY KEY (`seeid`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 23 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for sessionusers
-- ----------------------------
DROP TABLE IF EXISTS `sessionusers`;
CREATE TABLE `sessionusers`  (
  `seeid` bigint NULL DEFAULT NULL,
  `swid` varchar(128) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `access_token` varchar(64) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `refresh_token` varchar(64) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `aesSecret` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `aesSecretChat` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT '',
  `aesSecretSfs2x` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT '',
  `onquest` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT '',
  `username` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
  `sessionId` bigint NULL DEFAULT 0,
  `isclient` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT 'CLUBPENGUINISLAND',
  `expires_at` datetime NULL DEFAULT (current_timestamp() + interval 1 day),
  INDEX `test`(`seeid` ASC) USING BTREE,
  CONSTRAINT `test` FOREIGN KEY (`seeid`) REFERENCES `sessions` (`seeid`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Event structure for cleanup_sessions
-- ----------------------------
DROP EVENT IF EXISTS `cleanup_sessions`;
delimiter ;;
CREATE EVENT `cleanup_sessions`
ON SCHEDULE
EVERY '30' MINUTE STARTS '2026-04-06 04:37:53'
DO DELETE FROM sessions WHERE expires_at < NOW()
;;
delimiter ;

-- ----------------------------
-- Event structure for delete_rooms
-- ----------------------------
DROP EVENT IF EXISTS `delete_rooms`;
delimiter ;;
CREATE EVENT `delete_rooms`
ON SCHEDULE
EVERY '60' MINUTE STARTS '2026-04-06 03:58:24'
DO DELETE FROM rooms WHERE expires_at < NOW()
;;
delimiter ;

SET FOREIGN_KEY_CHECKS = 1;
