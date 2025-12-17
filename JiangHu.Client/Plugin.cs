using BepInEx;
using BepInEx.Configuration;
using EFT;
using HarmonyLib;
using JiangHu.ExfilRandomizer;
using JiangHu.Patches;
using System;
using UnityEngine;

namespace JiangHu
{
    [BepInPlugin("jianghu.music", "Jiang Hu", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private MusicPlayer musicPlayer;
        private DescriptionLoader descriptionLoader;
        private ChangeBackground changeBackground;
        private RuleSettingsManager ruleSettingsManager;
        private GameObject pluginObj;

        private ConfigEntry<KeyboardShortcut> ShowSettingsHotkey;
        private ConfigEntry<bool> ShowSettingsManager;
        private ConfigEntry<bool> ShowDescription;
        private ConfigEntry<KeyboardShortcut> ShowPlayerHotkey;
        private ConfigEntry<bool> ShowMusicPlayer;

        private DeathMatch DeathMatch;

        private ConfigEntry<KeyboardShortcut> SpawnPMCHotkey;
        private ConfigEntry<KeyboardShortcut> SwapBotHotkey;

        void Awake()
        {
            ShowPlayerHotkey = Config.Bind("JiangHu World Shaper  世界塑造器", "Hotkey", new KeyboardShortcut(KeyCode.F4), "Hotkey to show/hide World Shaper");
            ShowMusicPlayer = Config.Bind("JiangHu World Shaper  世界塑造器", "Show World Shaper", false, "Show/hide World Shaper");
            ShowSettingsHotkey = Config.Bind("Game Settings Manager  游戏设置管理器", "Hotkey", new KeyboardShortcut(KeyCode.F5), "Hotkey to show/hide Game settings manager");
            ShowSettingsManager = Config.Bind("Game Settings Manager  游戏设置管理器", "Show Setting Manager", false, "Show/hide Game settings manager");
            SpawnPMCHotkey = Config.Bind("PMC Teammates 人机队友", "Hotkey",
               new KeyboardShortcut(KeyCode.F8), "Hotkey to spawn a PMC teammate");
            SwapBotHotkey = Config.Bind(
                "Stellar Transposition  斗转星移",
                "Hotkey",
                new KeyboardShortcut(KeyCode.F10),
                "Hotkey to instantly swap positions with the bot you're looking at"
            );
            ShowDescription = Config.Bind("About JiangHu 江湖手册", "Detailed Mod Info", true, "Show detailed mod information");


            pluginObj = new GameObject("JiangHuPlugin");
            DontDestroyOnLoad(pluginObj);
            pluginObj.hideFlags = HideFlags.HideAndDontSave;

            pluginObj.AddComponent<DogtagConditionManager>();
            pluginObj.AddComponent<XPConditionManager>();
            pluginObj.AddComponent<RaidStatusConditionManager>();

            var newMovement = pluginObj.AddComponent<NewMovement>();
            newMovement.SetSwapHotkey(SwapBotHotkey);


            pluginObj.AddComponent<RemoveAlpha>();

            changeBackground = pluginObj.AddComponent<ChangeBackground>();
            changeBackground.Init();

            musicPlayer = pluginObj.AddComponent<MusicPlayer>();

            changeBackground.SetMusicPlayer(musicPlayer);


            var worldShaper = pluginObj.AddComponent<WorldShaper>();
            worldShaper.SetConfig(ShowPlayerHotkey, ShowMusicPlayer, musicPlayer, changeBackground);

            ruleSettingsManager = pluginObj.AddComponent<RuleSettingsManager>();
            ruleSettingsManager.SetConfig(ShowSettingsManager);

            descriptionLoader = pluginObj.AddComponent<DescriptionLoader>();
            descriptionLoader.SetConfig(ShowDescription);

            DeathMatch = pluginObj.AddComponent<DeathMatch>();
            DeathMatch.Init();


            var pmcSpawner = pluginObj.AddComponent<PMCBotSpawner>();
            pmcSpawner.Init(SpawnPMCHotkey);

            new MainMenuModifierPatch().Enable();
            new HideProgressCounterUIPatch().Enable();
            new RandomExfilDestinationPatch().Enable();
            new RaidEndDetectionPatch().Enable();

            new PatchSlotItemViewRefresh().Enable();
            new PatchGridViewShow().Enable();

            

            var harmony = new Harmony("jianghu.all");
            harmony.PatchAll();

            new DeathMatchButtonPatch().Enable();

            BossSpawnSystem.initialSpawnDone = false;

        }

        private void UpdateCursorState()
        {
            bool anyGUIOpen = ShowMusicPlayer.Value || ShowSettingsManager.Value;

            if (anyGUIOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        void Update()
        {
            if (ruleSettingsManager == null)
            {
                return;
            }
            if (ShowSettingsHotkey.Value.IsDown())
            {
                ShowSettingsManager.Value = !ShowSettingsManager.Value;
            }

            UpdateCursorState();
        }

        void OnDestroy()
        {

            if (pluginObj != null)
                Destroy(pluginObj);
        }
    }
}