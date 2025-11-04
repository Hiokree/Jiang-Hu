using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JiangHu.Patches;
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

        private ConfigEntry<KeyboardShortcut> ShowPlayerHotkey;
        private ConfigEntry<bool> ShowMusicPlayer;
        private ConfigEntry<KeyboardShortcut> ShowSettingsHotkey;
        private ConfigEntry<bool> ShowSettingsManager;
        private ConfigEntry<bool> ShowDescription;
        private ConfigEntry<KeyboardShortcut> ShowDebugToolHotkey;
        private ConfigEntry<bool> ShowDebugTool;
        private DebugTool debugTool;
        private ConfigEntry<bool> BackgroundEnabled;

        void Awake()
        {
            ShowPlayerHotkey = Config.Bind("Jiang Hu World Shaper", "Show Jiang Hu World Shaper Hotkey", new KeyboardShortcut(KeyCode.F4), "Hotkey to show/hide Jiang Hu World Shaper");
            ShowMusicPlayer = Config.Bind("Jiang Hu World Shaper", "Show Jiang Hu World Shaper", false, "Show/hide Jiang Hu World Shaper GUI");
            ShowSettingsHotkey = Config.Bind("Rule Settings Manager", "Show Setting Manager Hotkey", new KeyboardShortcut(KeyCode.F5), "Hotkey to show/hide rule settings GUI");
            ShowSettingsManager = Config.Bind("Rule Settings Manager", "Show Setting Manager", false, "Show/hide rule settings GUI");
            ShowDebugToolHotkey = Config.Bind("Debug Tool", "Show Debug Tool Hotkey", new KeyboardShortcut(KeyCode.F6), "Hotkey to show/hide debug tool GUI");
            ShowDebugTool = Config.Bind("Debug Tool", "Show Debug Tool", false, "Show/hide debug tool GUI");
            ShowDescription = Config.Bind("About JiangHu", "Detailed Mod Info", true, "Show detailed mod information");

            pluginObj = new GameObject("JiangHuPlugin");
            DontDestroyOnLoad(pluginObj);
            pluginObj.hideFlags = HideFlags.HideAndDontSave;

            pluginObj.AddComponent<RemoveAlpha>();

            BackgroundEnabled = Config.Bind("Jiang Hu World Shaper", "Background Enabled", true, "Enable/disable custom background");

            changeBackground = pluginObj.AddComponent<ChangeBackground>();
            changeBackground.SetConfig(BackgroundEnabled);
            changeBackground.Init();


            musicPlayer = pluginObj.AddComponent<MusicPlayer>();
            musicPlayer.SetConfig(ShowPlayerHotkey, ShowMusicPlayer, changeBackground, BackgroundEnabled);

            ruleSettingsManager = pluginObj.AddComponent<RuleSettingsManager>();
            ruleSettingsManager.SetConfig(ShowSettingsManager);

            debugTool = pluginObj.AddComponent<DebugTool>();
            debugTool.SetConfig(ShowDebugTool);

            descriptionLoader = pluginObj.AddComponent<DescriptionLoader>();
            descriptionLoader.SetConfig(ShowDescription);

            PatchUseRepairKitInRaid.Enable();


        }

        void Update()
        {
            if (ShowPlayerHotkey.Value.IsDown())
            {
                ShowMusicPlayer.Value = !ShowMusicPlayer.Value;
            }

            if (ShowSettingsHotkey.Value.IsDown())
            {
                ShowSettingsManager.Value = !ShowSettingsManager.Value;
            }

            if (ShowDebugToolHotkey.Value.IsDown())
            {
                ShowDebugTool.Value = !ShowDebugTool.Value;
            }
        }

        void OnDestroy()
        {
            if (pluginObj != null)
                Destroy(pluginObj);
        }
    }
}