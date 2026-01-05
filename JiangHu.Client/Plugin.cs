using BepInEx;
using HarmonyLib;
using JiangHu.ExfilRandomizer;
using JiangHu.Patches;
using UnityEngine;

namespace JiangHu
{
    [BepInPlugin("jianghu", "Jiang Hu", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private GameObject pluginObj;
        private MusicPlayer musicPlayer;
        private ChangeBackground changeBackground;
        private WorldShaper worldShaper;
        private RuleSettingsManager ruleSettingsManager;
        private DescriptionLoader descriptionLoader;


        void Awake()
        {
            F12Manager.Init(Config);

            pluginObj = new GameObject("JiangHuPlugin");
            DontDestroyOnLoad(pluginObj);
            pluginObj.hideFlags = HideFlags.HideAndDontSave;

            changeBackground = pluginObj.AddComponent<ChangeBackground>();
            changeBackground.Init();

            musicPlayer = pluginObj.AddComponent<MusicPlayer>();
            changeBackground.SetMusicPlayer(musicPlayer);

            worldShaper = pluginObj.AddComponent<WorldShaper>();
            worldShaper.SetConfig(F12Manager.WorldShaperHotkey, false, musicPlayer, changeBackground);

            pluginObj.AddComponent<RemoveAlpha>();

            ruleSettingsManager = pluginObj.AddComponent<RuleSettingsManager>();
            ruleSettingsManager.SetConfig(F12Manager.ShowSettingsHotkey);

            descriptionLoader = pluginObj.AddComponent<DescriptionLoader>();
            descriptionLoader.SetConfig(F12Manager.ShowDescription);

            var newMovement = pluginObj.AddComponent<NewMovement>();
            newMovement.SetSwapHotkey(F12Manager.SwapBotHotkey);

            pluginObj.AddComponent<DogtagConditionManager>();
            pluginObj.AddComponent<XPConditionManager>();
            pluginObj.AddComponent<RaidStatusConditionManager>();


            pluginObj.AddComponent<BattleScreenPlugin>();

            pluginObj.AddComponent<DeathMatchUI>();
            pluginObj.AddComponent<DeathMatchCore>();
            pluginObj.AddComponent<DeathMatchPlayer>();
            pluginObj.AddComponent<TeleportCommand>();
            pluginObj.AddComponent<FreeCameraController>();

            var universalSpawner = pluginObj.AddComponent<UniversalBotSpawner>();
            universalSpawner.Init(F12Manager.UniversalSpawnHotkey,
                                  F12Manager.RemoveBotHotkey,  
                                  F12Manager.BotHostility,
                                  F12Manager.BotTypeConfigs);

            var bossNotifier = pluginObj.AddComponent<BossNotificationSystem>();

            // Enable patches
            new MainMenuModifierPatch().Enable();
            new HideProgressCounterUIPatch().Enable();
            new RandomExfilDestinationPatch().Enable();
            new RaidEndDetectionPatch().Enable();
            new PatchSlotItemViewRefresh().Enable();
            new PatchGridViewShow().Enable();

            var harmony = new Harmony("jianghu.all");
            harmony.PatchAll();


        }

        private void UpdateCursorState()
        {
            bool worldShaperVisible = worldShaper != null && worldShaper.IsGUIVisible();
            bool settingsManagerVisible = ruleSettingsManager != null && ruleSettingsManager.IsGUIVisible();

            bool anyGUIOpen = worldShaperVisible || settingsManagerVisible;

            if (anyGUIOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        void Update()
        {
            UpdateCursorState();
        }

        void OnDestroy()
        {
            if (pluginObj != null)
                Destroy(pluginObj);
        }
    }
}