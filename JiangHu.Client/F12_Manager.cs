using BepInEx.Configuration;
using EFT;
using System.Collections.Generic;
using UnityEngine;

namespace JiangHu
{
    public static class F12Manager
    {
        public static ConfigEntry<KeyboardShortcut> WorldShaperHotkey;
        public static ConfigEntry<KeyboardShortcut> ShowSettingsHotkey;
        public static ConfigEntry<KeyboardShortcut> SwapBotHotkey;
        public static ConfigEntry<KeyboardShortcut> BattleScreenHotkey;
        public static ConfigEntry<bool> ShowDescription;

        public static ConfigEntry<KeyboardShortcut> UniversalSpawnHotkey;
        public static ConfigEntry<KeyboardShortcut> RemoveBotHotkey;
        public static ConfigEntry<bool> DisableVanillaBotSpawn;
        public enum HostilityType { Friendly, Enemy, Neutral }
        public static ConfigEntry<HostilityType> BotHostility;
        public static Dictionary<WildSpawnType, ConfigEntry<bool>> BotTypeConfigs = new Dictionary<WildSpawnType, ConfigEntry<bool>>();

        public static ConfigEntry<bool> ShowFriendlyBots;
        public static ConfigEntry<bool> ShowEnemyBots;
        public static ConfigEntry<bool> ShowNeutralBots;
        public static ConfigEntry<bool> ShowTeam;
        public static ConfigEntry<bool> ShowBotIndicator;
        public static ConfigEntry<bool> ShowDistance;
        public static ConfigEntry<bool> ShowBotType;
        public static ConfigEntry<bool> ShowBotName;

        public static ConfigEntry<bool> ShowBotBodyHighlight;
        public static ConfigEntry<bool> HighlightFriendlyBots;
        public static ConfigEntry<bool> HighlightEnemyBots;
        public static ConfigEntry<bool> HighlightNeutralBots;
        public static ConfigEntry<Color> FriendlyBotColor;
        public static ConfigEntry<Color> EnemyBotColor;
        public static ConfigEntry<Color> NeutralBotColor;
        public static ConfigEntry<bool> HighlightTeam;

        public static ConfigEntry<KeyboardShortcut> TeleportBotHotkey;
        public static ConfigEntry<KeyboardShortcut> FreeCameraHotkey;

        public static void Init(ConfigFile config)
        {
            const string sectionBotspawn = "- Bot Spawner 点将台 -";
            const string sectionShowBot = "- Bot Marker 万象 -";
            const string section1 = "Regular  常规";
            const string section2 = "Bosses 头目";
            const string section3 = "Followers 狗腿";
            const string section4 = "Infected 丧尸";
            const string section5 = "Special 特殊";
            const string sectionDM = "- Wonderland 乐园 -";



            WorldShaperHotkey = config.Bind(
                "",
                "World Shaper 世界塑造器",
                new KeyboardShortcut(KeyCode.F4),
                new ConfigDescription("Hotkey to show/hide the World Shaper interface (music player + background changer)", null,
                    new ConfigurationManagerAttributes { Order = 564 }));

            ShowSettingsHotkey = config.Bind(
                "",
                "Game Settings Manager 游戏管理中心",
                new KeyboardShortcut(KeyCode.F5),
                new ConfigDescription("Hotkey to show/hide Game settings manager", null,
                    new ConfigurationManagerAttributes { Order = 563 }));

            BattleScreenHotkey = config.Bind(
                "",
                "Battle Screen 锋镝录",
                new KeyboardShortcut(KeyCode.F8),
                new ConfigDescription("Hotkey to show/hide the Battle Screen interface", null,
                    new ConfigurationManagerAttributes { Order = 562 }));

            SwapBotHotkey = config.Bind(
                "",
                "Stellar Transposition 斗转星移",
                new KeyboardShortcut(KeyCode.F),
                new ConfigDescription("Hotkey to instantly swap positions with the bot you're looking at", null,
                    new ConfigurationManagerAttributes { Order = 561 }));

            ShowDescription = config.Bind(
                "",
                "Mod Info 指南手册",
                true,
                new ConfigDescription("Show detailed mod information", null,
                    new ConfigurationManagerAttributes { Order = 560 }));

            TeleportBotHotkey = config.Bind(
                sectionDM,
                "Move Bot piece  移动人机棋子",
                new KeyboardShortcut(KeyCode.T),
                new ConfigDescription("Select and place bot  拿起，落子", null,
                    new ConfigurationManagerAttributes { Order = 551 }));

            FreeCameraHotkey = config.Bind(
                sectionDM,
                "Free Camera 自由视角",
                new KeyboardShortcut(KeyCode.F11),
                new ConfigDescription("Toggle free camera mode", null,
                    new ConfigurationManagerAttributes { Order = 550 }));

            ShowBotBodyHighlight = config.Bind(
                sectionShowBot,
                "Enable Highlight 开启画皮",
                true,
                new ConfigDescription("Enable colored body highlight on bots", null,
                    new ConfigurationManagerAttributes { Order = 527 }));

            HighlightFriendlyBots = config.Bind(
                sectionShowBot,
                "Highlight Friendly Bots 画皮队友",
                true,
                new ConfigDescription("friendly bots 队友", null,
                    new ConfigurationManagerAttributes { Order = 526 }));

            HighlightEnemyBots = config.Bind(
                sectionShowBot,
                "Highlight Enemy Bots 画皮对手",
                true,
                new ConfigDescription("enemy bots 对手", null,
                    new ConfigurationManagerAttributes { Order = 525 }));

            HighlightNeutralBots = config.Bind(
                sectionShowBot,
                "Highlight Neutral Bots 画皮中立",
                false,
                new ConfigDescription("neutral/unaffiliated 中立", null,
                    new ConfigurationManagerAttributes { Order = 524 }));

            HighlightTeam = config.Bind(
                sectionShowBot,
                "Highlight Bot War Team 画皮混战队伍",
                true,
                new ConfigDescription("Show DeathMatch enemy teams with their team colors 使用战队颜色显示混战队伍", null,
                    new ConfigurationManagerAttributes { Order = 523 }));

            FriendlyBotColor = config.Bind(
                sectionShowBot,
                "Friendly Highlight 队友画色",
                new Color(0.27f, 0.72f, 0.27f, 0.7f), // Green
                new ConfigDescription("Highlight Color for friendly", null,
                    new ConfigurationManagerAttributes { Order = 522 }));

            EnemyBotColor = config.Bind(
                sectionShowBot,
                "Enemy Highlight 对手画色",
                new Color(0.96f, 0.27f, 0.02f, 0.7f), // Red
                new ConfigDescription("Highlight Color for enemy", null,
                    new ConfigurationManagerAttributes { Order = 521 }));

            NeutralBotColor = config.Bind(
                sectionShowBot,
                "Neutral Highlight 中立画色",
                new Color(0.5f, 0.5f, 0.5f, 0.5f), // Gray
                new ConfigDescription("Highlight Color for neutral", null,
                    new ConfigurationManagerAttributes { Order = 520 }));

            ShowBotIndicator = config.Bind(
                sectionShowBot,
                "Show Bot Indicator 开启草标",
                true,
                new ConfigDescription("Enable indicator", null,
                    new ConfigurationManagerAttributes { Order = 517 }));

            ShowFriendlyBots = config.Bind(
                sectionShowBot,
                "Show Teammate 队友草标",
                true,
                new ConfigDescription("Show friendly bots", null,
                    new ConfigurationManagerAttributes { Order = 516 }));

            ShowEnemyBots = config.Bind(
                sectionShowBot,
                "Show Opponent 对手草标",
                true,
                new ConfigDescription("Show enemy bots", null,
                    new ConfigurationManagerAttributes { Order = 515 }));

            ShowNeutralBots = config.Bind(
                sectionShowBot,
                "Show Neutral Bots 中立草标",
                false,
                new ConfigDescription("Show Neutral bots", null,
                    new ConfigurationManagerAttributes { Order = 514 }));

            ShowTeam = config.Bind(
                sectionShowBot,
                "Show Bot War Team 混战队伍草标",
                true,
                new ConfigDescription("Show Bot War enemy teams 使用战队颜色显示混战对手队伍", null,
                    new ConfigurationManagerAttributes { Order = 513 })); 

            ShowBotName = config.Bind(
                sectionShowBot,
                "Show Bot Name 显示名字",
                true,
                new ConfigDescription("Show bot name", null,
                    new ConfigurationManagerAttributes { Order = 512 }));

            ShowBotType = config.Bind(
                sectionShowBot,
                "Show Bot Type 显示类型",
                true,
                new ConfigDescription("Show bot type", null,
                    new ConfigurationManagerAttributes { Order = 511 }));

            ShowDistance = config.Bind(
                sectionShowBot,
                "Show Distance 显示距离",
                true,
                new ConfigDescription("Show distance", null,
                    new ConfigurationManagerAttributes { Order = 510 }));

            const string guiSection = "";


            UniversalSpawnHotkey = config.Bind(
                sectionBotspawn,
                "Spawn Bot Hotkey 召唤",
                new KeyboardShortcut(KeyCode.F6),
                new ConfigDescription("Spawn bot based on current settings", null,
                    new ConfigurationManagerAttributes { Order = 503 }));

            RemoveBotHotkey = config.Bind(
                sectionBotspawn,
                "Remove Bot Hotkey 移除",
                new KeyboardShortcut(KeyCode.F7),
                new ConfigDescription("Remove bot matching current settings", null,
                    new ConfigurationManagerAttributes { Order = 502 }));

            BotHostility = config.Bind(
                sectionBotspawn,
                "Hostility 敌友",
                HostilityType.Friendly,
                new ConfigDescription("Bot hostility towards player", null,
                    new ConfigurationManagerAttributes { Order = 501 }));

            DisableVanillaBotSpawn = config.Bind(
                sectionBotspawn,
                "Disable Vanilla Bot Spawn 禁止系统刷人机",
                true,
                new ConfigDescription("Remove all vanilla spawned bots", null,
                    new ConfigurationManagerAttributes { Order = 500 }));

            // Regular Bots Section
            int order = 400;
            AddBotTypeConfig(config, WildSpawnType.pmcBEAR, "PMC BEAR", section1, order++);
            AddBotTypeConfig(config, WildSpawnType.pmcUSEC, "PMC USEC", section1, order++);
            AddBotTypeConfig(config, WildSpawnType.assault, "Scav", section1, order++);
            AddBotTypeConfig(config, WildSpawnType.pmcBot, "Raider", section1, order++);
            AddBotTypeConfig(config, WildSpawnType.exUsec, "Rogue", section1, order++);
            AddBotTypeConfig(config, WildSpawnType.arenaFighterEvent, "Smuggler", section1, order++);
            AddBotTypeConfig(config, WildSpawnType.sectantWarrior, "Cultist Warrior", section1, order++);
            AddBotTypeConfig(config, WildSpawnType.marksman, "Scav Sniper", section1, order++);

            // Bosses Section
            order = 300;
            AddBotTypeConfig(config, WildSpawnType.bossBully, "Reshala", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossGluhar, "Gluhar", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossKojaniy, "Shturman", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossSanitar, "Sanitar", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossKnight, "Knight", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.followerBigPipe, "Big Pipe", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.followerBirdEye, "Bird Eye", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossBoar, "Kaban", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossKolontay, "Kolontay", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.sectantPriest, "Cultist Priest", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossZryachiy, "Zryachiy", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossPartisan, "Partisan", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossKilla, "Killa", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossTagilla, "Tagilla", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossTagillaAgro, "Tagilla (Aggro)", section2, order++);
            AddBotTypeConfig(config, WildSpawnType.bossKillaAgro, "Killa (Aggro)", section2, order++);


            // Followers Section
            order = 200;
            AddBotTypeConfig(config, WildSpawnType.followerKojaniy, "Shturman Follower", section3, order++);
            AddBotTypeConfig(config, WildSpawnType.followerSanitar, "Sanitar Follower", section3, order++);
            AddBotTypeConfig(config, WildSpawnType.followerZryachiy, "Zryachiy Follower", section3, order++);
            AddBotTypeConfig(config, WildSpawnType.followerBoar, "Kaban Follower", section3, order++);
            AddBotTypeConfig(config, WildSpawnType.followerBoarClose1, "Kaban Close Guard 1", section3, order++);
            AddBotTypeConfig(config, WildSpawnType.followerBoarClose2, "Kaban Close Guard 2", section3, order++);
            AddBotTypeConfig(config, WildSpawnType.bossBoarSniper, "Kaban Sniper Guard", section3, order++);
            AddBotTypeConfig(config, WildSpawnType.followerKolontayAssault, "Kolontay Assault Follower", section3, order++);
            AddBotTypeConfig(config, WildSpawnType.followerKolontaySecurity, "Kolontay Security Follower", section3, order++);
            AddBotTypeConfig(config, WildSpawnType.tagillaHelperAgro, "Tagilla Helper", section3, order++);

            // Infected Section
            order = 100;
            AddBotTypeConfig(config, WildSpawnType.infectedAssault, "Infected Scav", section4, order++);
            AddBotTypeConfig(config, WildSpawnType.infectedPmc, "Infected PMC", section4, order++);
            AddBotTypeConfig(config, WildSpawnType.infectedCivil, "Infected Civilian", section4, order++);
            AddBotTypeConfig(config, WildSpawnType.infectedLaborant, "Infected Lab Worker", section4, order++);
            AddBotTypeConfig(config, WildSpawnType.infectedTagilla, "Infected Tagilla", section4, order++);

            // Special Bots Section
            order = 000;
            AddBotTypeConfig(config, WildSpawnType.cursedAssault, "Cursed Scav", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.crazyAssaultEvent, "Crazy Scav", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.sectantPredvestnik, "Cultist Herald", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.sectantPrizrak, "Cultist Ghost", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.sectantOni, "Cultist Oni", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.sectactPriestEvent, "Cultist Priest (Event)", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.peacefullZryachiyEvent, "Peaceful Zryachiy", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.ravangeZryachiyEvent, "Enraged Zryachiy", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.shooterBTR, "BTR Gunner", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.gifter, "Santa", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.skier, "Skier", section5, order++);
            AddBotTypeConfig(config, WildSpawnType.peacemaker, "Peacekeeper", section5, order++);
        }

        private static void AddBotTypeConfig(ConfigFile config, WildSpawnType botType,
                                           string displayName, string section, int order)
        {
            var entry = config.Bind(
                section,
                displayName,
                false,
                new ConfigDescription($"Enable {displayName} bot spawning", null,
                    new ConfigurationManagerAttributes { Order = order }));

            BotTypeConfigs[botType] = entry;
        }
    }

    internal sealed class ConfigurationManagerAttributes
    {
        public System.Action<ConfigEntryBase> CustomDrawer;
        public int? Order;
    }
}