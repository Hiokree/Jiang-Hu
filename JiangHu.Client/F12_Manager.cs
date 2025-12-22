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
        public static ConfigEntry<bool> ShowDescription;

        public static ConfigEntry<KeyboardShortcut> UniversalSpawnHotkey;
        public static ConfigEntry<KeyboardShortcut> RemoveBotHotkey;
        public static ConfigEntry<bool> DisableVanillaBotSpawn;
        public enum HostilityType { Friendly, Enemy, Neutral }
        public static ConfigEntry<HostilityType> BotHostility;
        public static Dictionary<WildSpawnType, ConfigEntry<bool>> BotTypeConfigs = new Dictionary<WildSpawnType, ConfigEntry<bool>>();

        public static ConfigEntry<bool> ShowJiangHuTeammate;
        public static ConfigEntry<bool> ShowJiangHuOpponent;
        public static ConfigEntry<bool> ShowJiangHuBots;
        public static ConfigEntry<bool> ShowAllBots;
        public static ConfigEntry<bool> ShowBotIndicator;
        public static ConfigEntry<bool> ShowDistance;
        public static ConfigEntry<bool> ShowBotType;
        public static ConfigEntry<bool> ShowBotName;

        public static void Init(ConfigFile config)
        {
            const string sectionBotspawn = "- Bot Spawner 点将台 -";
            const string sectionShowBot = "In raid Indicator 局内显示";
            const string section1 = "Regular  常规";
            const string section2 = "Bosses 头目";
            const string section3 = "Followers 狗腿";
            const string section4 = "Infected 丧尸";
            const string section5 = "Special 特殊";


            WorldShaperHotkey = config.Bind(
                "",
                "World Shaper 世界塑造器",
                new KeyboardShortcut(KeyCode.F4),
                new ConfigDescription("Hotkey to show/hide the World Shaper interface (music player + background changer)", null,
                    new ConfigurationManagerAttributes { Order = 553 }));

            ShowSettingsHotkey = config.Bind(
                "",
                "Game Settings Manager 游戏管理中心",
                new KeyboardShortcut(KeyCode.F5),
                new ConfigDescription("Hotkey to show/hide Game settings manager", null,
                    new ConfigurationManagerAttributes { Order = 552 }));

            SwapBotHotkey = config.Bind(
                "",
                "Stellar Transposition 斗转星移",
                new KeyboardShortcut(KeyCode.F),
                new ConfigDescription("Hotkey to instantly swap positions with the bot you're looking at", null,
                    new ConfigurationManagerAttributes { Order = 551 }));

            ShowDescription = config.Bind(
                "",
                "Mod Info 指南手册",
                true,
                new ConfigDescription("Show detailed mod information", null,
                    new ConfigurationManagerAttributes { Order = 550 }));


            UniversalSpawnHotkey = config.Bind(
                sectionBotspawn,
                "Spawn Bot Hotkey 召唤",
                new KeyboardShortcut(KeyCode.F6),
                new ConfigDescription("Spawn bot based on current settings", null,
                    new ConfigurationManagerAttributes { Order = 511 }));

            RemoveBotHotkey = config.Bind(
                sectionBotspawn,
                "Remove Bot Hotkey 移除",
                new KeyboardShortcut(KeyCode.F7),
                new ConfigDescription("Remove bot matching current settings", null,
                    new ConfigurationManagerAttributes { Order = 510 }));

            BotHostility = config.Bind(
                sectionBotspawn,
                "Hostility 敌友",
                HostilityType.Friendly,
                new ConfigDescription("Bot hostility towards player", null,
                    new ConfigurationManagerAttributes { Order = 509 }));

            DisableVanillaBotSpawn = config.Bind(
                sectionBotspawn,
                "Disable Vanilla Bot Spawn 禁止系统刷人机",
                true,
                new ConfigDescription("Remove all vanilla spawned bots", null,
                    new ConfigurationManagerAttributes { Order = 508 }));

            const string guiSection = "";

            ShowBotIndicator = config.Bind(
                sectionShowBot,
                "Show Bot Indicator 开启显示仪",
                true,
                new ConfigDescription("Enable indicator", null,
                    new ConfigurationManagerAttributes { Order = 507 }));

            ShowJiangHuTeammate = config.Bind(
                sectionShowBot,
                "Show JiangHu Teammate 显示江湖队友",
                true,
                new ConfigDescription("Show friendly JiangHu marked bots", null,
                    new ConfigurationManagerAttributes { Order = 506 }));

            ShowJiangHuOpponent = config.Bind(
                sectionShowBot,
                "Show JiangHu Opponent 显示江湖对手",
                true,
                new ConfigDescription("Show enemy JiangHu marked bots (includes DeathMatch bosses)", null,
                    new ConfigurationManagerAttributes { Order = 505 }));

            ShowJiangHuBots = config.Bind(
                sectionShowBot,
                "Show JiangHu Bots 显示江湖众生",
                false,
                new ConfigDescription("Show all JiangHu marked bots", null,
                    new ConfigurationManagerAttributes { Order = 504 }));

            ShowAllBots = config.Bind(
                sectionShowBot,
                "Show All Bots 显示所有人机",
                false,
                new ConfigDescription("Show all bots", null,
                    new ConfigurationManagerAttributes { Order = 503 }));

            ShowBotName = config.Bind(
                sectionShowBot,
                "Show Bot Name 显示名字",
                true,
                new ConfigDescription("Show bot name", null,
                    new ConfigurationManagerAttributes { Order = 502 }));

            ShowBotType = config.Bind(
                sectionShowBot,
                "Show Bot Type 显示类型",
                true,
                new ConfigDescription("Show bot type", null,
                    new ConfigurationManagerAttributes { Order = 501 }));

            ShowDistance = config.Bind(
                sectionShowBot,
                "Show Distance 显示距离",
                true,
                new ConfigDescription("Show distance", null,
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
            AddBotTypeConfig(config, WildSpawnType.followerBoarClose1, "Kaban Close Guard", section3, order++);
            AddBotTypeConfig(config, WildSpawnType.followerBoarClose2, "Kaban Close Guard", section3, order++);
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
        public bool? ShowRangeAsPercent;
        public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer;
        public CustomHotkeyDrawerFunc CustomHotkeyDrawer;
        public delegate void CustomHotkeyDrawerFunc(BepInEx.Configuration.ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);
        public bool? Browsable;
        public string Category;
        public object DefaultValue;
        public bool? HideDefaultButton;
        public bool? HideSettingName;
        public string Description;
        public string DispName;
        public int? Order;
        public bool? ReadOnly;
        public bool? IsAdvanced;
        public System.Func<object, string> ObjToStr;
        public System.Func<string, object> StrToObj;
    }
}