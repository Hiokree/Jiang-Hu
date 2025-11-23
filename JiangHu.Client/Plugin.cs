using BepInEx;
using BepInEx.Configuration;
using EFT;
using GPUInstancer;
using HarmonyLib;
using JiangHu.Patches;
using UnityEngine;
using System.Reflection;

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

        void Awake()
        {
            ShowPlayerHotkey = Config.Bind("JiangHu World Shaper", "Show World Shaper Hotkey", new KeyboardShortcut(KeyCode.F4), "Hotkey to show/hide World Shaper");
            ShowMusicPlayer = Config.Bind("JiangHu World Shaper", "Show World Shaper", false, "Show/hide World Shaper");
            ShowSettingsHotkey = Config.Bind("Game Settings Manager", "Show Setting Manager Hotkey", new KeyboardShortcut(KeyCode.F5), "Hotkey to show/hide Game settings manager");
            ShowSettingsManager = Config.Bind("Game Settings Manager", "Show Setting Manager", false, "Show/hide Game settings manager");
            ShowDescription = Config.Bind("About JiangHu", "Detailed Mod Info", true, "Show detailed mod information");


            pluginObj = new GameObject("JiangHuPlugin");
            DontDestroyOnLoad(pluginObj);
            pluginObj.hideFlags = HideFlags.HideAndDontSave;

            pluginObj.AddComponent<XPConditionManager>();
            pluginObj.AddComponent<NewMovement>();
            pluginObj.AddComponent<RemoveAlpha>();

            changeBackground = pluginObj.AddComponent<ChangeBackground>();
            changeBackground.Init();

            musicPlayer = pluginObj.AddComponent<MusicPlayer>();

            var worldShaper = pluginObj.AddComponent<WorldShaper>();
            worldShaper.SetConfig(ShowPlayerHotkey, ShowMusicPlayer, musicPlayer, changeBackground);

            ruleSettingsManager = pluginObj.AddComponent<RuleSettingsManager>();
            ruleSettingsManager.SetConfig(ShowSettingsManager);

            descriptionLoader = pluginObj.AddComponent<DescriptionLoader>();
            descriptionLoader.SetConfig(ShowDescription);

            PatchUseRepairKitInRaid.Enable();
        }

        private void UpdateCursorState()
        {
            bool anyGUIOpen = ShowSettingsManager.Value;

            if (anyGUIOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        void Update()
        {
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