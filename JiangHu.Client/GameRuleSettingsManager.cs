using BepInEx.Configuration;
using EFT.UI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace JiangHu
{
    public class RuleSettingsManager : MonoBehaviour
    {
        private bool disableVanillaQuests = true;
        private bool unlockVanillaLockedItems = true;
        private bool changePrestigeCondition = true;
        private bool increaseHeadHP = true;
        private bool lockFlea = true;
        private bool enableEmptyVanillaShop = false;
        private bool enableNoInsurance = false;
        private bool enableCashWipeAfterDeath = false;
        private bool addHideoutProductionDSP = true;
        private bool addHideoutProductionLabryskeycard = true;
        private bool unlockAllLabrysQuests = true;
        private bool usePreset = true;
        private bool enableQuestGenerator = true;
        private bool enableJianghuBot = true;
        private bool enableJianghuBotName = true;
        private bool showBotNameGUI = false;
        private bool enableReplaceOneRaidWithOneLife = true;
        public bool enableNewMovement = true;
        private bool enableFastMovement = true;
        private bool enableFastLeaning = true;
        private bool enableFastPoseTransition = true;
        private bool enableJumpHigher = true;
        private bool enableSlide = true;
        private bool enableFastWeapon = true;
        private bool enableWiderFreelook = true;
        private bool showMovementSettingsGUI = false;
        private bool enableMinimalAimpunch = true;
        private bool enableFastAiming = true;
        private bool enableNewTrader = true;
        private bool enableNewQuest = true;
        private bool enableNewItem = true;
        private bool enableArenaMode = true;
        private bool restartArenaMode = false;
        private bool enableGreetingLog = true;
        private bool showCoreModulesGUI = false;
        private bool enableDogtagCollection = true;
        private bool removeVanillaQuestXPReward = true;
        private bool unlockVanillaTraderTraderStanding = true;
        private bool unlockVanillaLockedRecipe = true;
        private bool unlockVanillaLockedCustomization = true;
        private float cashWipeCoefficiency = 0.1f;
        private bool showStoryModeGuide = false;
        private Rect storyModeWindowRect = new Rect(200, 100, 1000, 800);
        private Vector2 storyModeScrollPosition = Vector2.zero;
        private string storyModeContent = "";


        private Rect coreModulesWindowRect = new Rect(350, 150, 360, 160);

        private bool showRuleSettingsGUI = false;
        private Rect ruleSettingsWindowRect = new Rect(350, 150, 600, 600);
        private Rect windowRect = new Rect(300, 100, 500, 830);
        private bool showGUI = false;

        private bool showSettingsGuide = false;
        private Rect guideWindowRect = new Rect(200, 100, 1000, 800);
        private Vector2 guideScrollPosition = Vector2.zero;
        private string settingsGuideContent = "";
        private string currentLanguage = "en";

        private Rect botNameWindowRect = new Rect(250, 150, 400, 270);
        private ConfigEntry<bool> showSettingsManager;

        private Rect movementSettingsWindowRect = new Rect(300, 150, 400, 480);

        public void SetConfig(ConfigEntry<bool> showSettingsManager)
        {
            this.showSettingsManager = showSettingsManager;
            LoadSettingsFromJson();
        }

        void Update()
        {
            if (showSettingsManager.Value && !showGUI)
            {
                showGUI = true;
            }
            else if (!showSettingsManager.Value && showGUI)
            {
                showGUI = false;
                showSettingsGuide = false;
            }
        }

        private void LoadSettingsFromJson()
        {
            string modPath = Path.GetDirectoryName(Application.dataPath);
            string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");
            string json = File.ReadAllText(configPath);
            var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (configDict != null)
            {
                if (configDict.ContainsKey("Disable_Vanilla_Quests") && configDict["Disable_Vanilla_Quests"] is bool)
                    disableVanillaQuests = (bool)configDict["Disable_Vanilla_Quests"];
                if (configDict.ContainsKey("Unlock_VanillaLocked_Items") && configDict["Unlock_VanillaLocked_Items"] is bool)
                    unlockVanillaLockedItems = (bool)configDict["Unlock_VanillaLocked_Items"];
                if (configDict.ContainsKey("Change_Prestige_Conditions") && configDict["Change_Prestige_Conditions"] is bool)
                    changePrestigeCondition = (bool)configDict["Change_Prestige_Conditions"];
                if (configDict.ContainsKey("Increase_HeadHP") && configDict["Increase_HeadHP"] is bool)
                    increaseHeadHP = (bool)configDict["Increase_HeadHP"];
                if (configDict.ContainsKey("Lock_Flea") && configDict["Lock_Flea"] is bool)
                    lockFlea = (bool)configDict["Lock_Flea"];
                if (configDict.ContainsKey("Enable_empty_vanilla_shop") && configDict["Enable_empty_vanilla_shop"] is bool)
                    enableEmptyVanillaShop = (bool)configDict["Enable_empty_vanilla_shop"];
                if (configDict.ContainsKey("Enable_No_Insurance") && configDict["Enable_No_Insurance"] is bool)
                    enableNoInsurance = (bool)configDict["Enable_No_Insurance"];
                if (configDict.ContainsKey("Enable_Cash_Wipe") && configDict["Enable_Cash_Wipe"] is bool)
                    enableCashWipeAfterDeath = (bool)configDict["Enable_Cash_Wipe"];
                if (configDict.ContainsKey("Add_HideoutProduction_DSP") && configDict["Add_HideoutProduction_DSP"] is bool)
                    addHideoutProductionDSP = (bool)configDict["Add_HideoutProduction_DSP"];
                if (configDict.ContainsKey("Add_HideoutProduction_Labryskeycard") && configDict["Add_HideoutProduction_Labryskeycard"] is bool)
                    addHideoutProductionLabryskeycard = (bool)configDict["Add_HideoutProduction_Labryskeycard"];
                if (configDict.ContainsKey("Unlock_All_Labrys_Quests") && configDict["Unlock_All_Labrys_Quests"] is bool)
                    unlockAllLabrysQuests = (bool)configDict["Unlock_All_Labrys_Quests"];
                if (configDict.ContainsKey("Use_Preset") && configDict["Use_Preset"] is bool)
                    usePreset = (bool)configDict["Use_Preset"];
                if (configDict.ContainsKey("Enable_Quest_Generator") && configDict["Enable_Quest_Generator"] is bool)
                    enableQuestGenerator = (bool)configDict["Enable_Quest_Generator"];
                if (configDict.ContainsKey("Enable_Jianghu_Bot") && configDict["Enable_Jianghu_Bot"] is bool)
                    enableJianghuBot = (bool)configDict["Enable_Jianghu_Bot"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName") && configDict["Enable_Jianghu_BotName"] is bool)
                    enableJianghuBotName = (bool)configDict["Enable_Jianghu_BotName"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_ch") && configDict["Enable_Jianghu_BotName_ch"] is bool)
                    botNameLanguageSettings["ch"] = (bool)configDict["Enable_Jianghu_BotName_ch"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_en") && configDict["Enable_Jianghu_BotName_en"] is bool)
                    botNameLanguageSettings["en"] = (bool)configDict["Enable_Jianghu_BotName_en"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_es") && configDict["Enable_Jianghu_BotName_es"] is bool)
                    botNameLanguageSettings["es"] = (bool)configDict["Enable_Jianghu_BotName_es"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_fr") && configDict["Enable_Jianghu_BotName_fr"] is bool)
                    botNameLanguageSettings["fr"] = (bool)configDict["Enable_Jianghu_BotName_fr"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_jp") && configDict["Enable_Jianghu_BotName_jp"] is bool)
                    botNameLanguageSettings["jp"] = (bool)configDict["Enable_Jianghu_BotName_jp"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_po") && configDict["Enable_Jianghu_BotName_po"] is bool)
                    botNameLanguageSettings["po"] = (bool)configDict["Enable_Jianghu_BotName_po"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_ru") && configDict["Enable_Jianghu_BotName_ru"] is bool)
                    botNameLanguageSettings["ru"] = (bool)configDict["Enable_Jianghu_BotName_ru"];
                if (configDict.ContainsKey("Enable_Replace_OneRaid_with_OneLife") && configDict["Enable_Replace_OneRaid_with_OneLife"] is bool)
                    enableReplaceOneRaidWithOneLife = (bool)configDict["Enable_Replace_OneRaid_with_OneLife"];
                if (configDict.ContainsKey("Enable_New_Movement") && configDict["Enable_New_Movement"] is bool)
                    enableNewMovement = (bool)configDict["Enable_New_Movement"];
                if (configDict.ContainsKey("Enable_Fast_Movement") && configDict["Enable_Fast_Movement"] is bool)
                    enableFastMovement = (bool)configDict["Enable_Fast_Movement"];
                if (configDict.ContainsKey("Enable_Fast_Leaning") && configDict["Enable_Fast_Leaning"] is bool)
                    enableFastLeaning = (bool)configDict["Enable_Fast_Leaning"];
                if (configDict.ContainsKey("Enable_Fast_Pose_Transition") && configDict["Enable_Fast_Pose_Transition"] is bool)
                    enableFastPoseTransition = (bool)configDict["Enable_Fast_Pose_Transition"];
                if (configDict.ContainsKey("Enable_Jump_Higher") && configDict["Enable_Jump_Higher"] is bool)
                    enableJumpHigher = (bool)configDict["Enable_Jump_Higher"];
                if (configDict.ContainsKey("Enable_Slide") && configDict["Enable_Slide"] is bool)
                    enableSlide = (bool)configDict["Enable_Slide"];
                if (configDict.ContainsKey("Enable_Fast_Weapon_Switching") && configDict["Enable_Fast_Weapon_Switching"] is bool)
                    enableFastWeapon = (bool)configDict["Enable_Fast_Weapon_Switching"];
                if (configDict.ContainsKey("Enable_Minimal_Aimpunch") && configDict["Enable_Minimal_Aimpunch"] is bool)
                    enableMinimalAimpunch = (bool)configDict["Enable_Minimal_Aimpunch"];
                if (configDict.ContainsKey("Enable_Fast_Aiming") && configDict["Enable_Fast_Aiming"] is bool)
                    enableFastAiming = (bool)configDict["Enable_Fast_Aiming"];
                if (configDict.ContainsKey("Enable_Wider_Freelook_Angle") && configDict["Enable_Wider_Freelook_Angle"] is bool)
                    enableWiderFreelook = (bool)configDict["Enable_Wider_Freelook_Angle"];
                if (configDict.ContainsKey("Enable_New_Trader") && configDict["Enable_New_Trader"] is bool)
                    enableNewTrader = (bool)configDict["Enable_New_Trader"];
                if (configDict.ContainsKey("Enable_New_Quest") && configDict["Enable_New_Quest"] is bool)
                    enableNewQuest = (bool)configDict["Enable_New_Quest"];
                if (configDict.ContainsKey("Enable_New_Item") && configDict["Enable_New_Item"] is bool)
                    enableNewItem = (bool)configDict["Enable_New_Item"];
                if (configDict.ContainsKey("Enable_Arena_Mode") && configDict["Enable_Arena_Mode"] is bool)
                    enableArenaMode = (bool)configDict["Enable_Arena_Mode"];
                if (configDict.ContainsKey("Restart_Arena_Mode") && configDict["Restart_Arena_Mode"] is bool)
                    restartArenaMode = (bool)configDict["Restart_Arena_Mode"];
                if (configDict.ContainsKey("Enable_Greeting_Log") && configDict["Enable_Greeting_Log"] is bool)
                    enableGreetingLog = (bool)configDict["Enable_Greeting_Log"];
                if (configDict.ContainsKey("Enable_Dogtag_Collection") && configDict["Enable_Dogtag_Collection"] is bool)
                    enableDogtagCollection = (bool)configDict["Enable_Dogtag_Collection"];
                if (configDict.ContainsKey("Remove_VanillaQuest_XP_reward") && configDict["Remove_VanillaQuest_XP_reward"] is bool)
                    removeVanillaQuestXPReward = (bool)configDict["Remove_VanillaQuest_XP_reward"];
                if (configDict.ContainsKey("Unlock_VanilaTrader_TraderStanding") && configDict["Unlock_VanilaTrader_TraderStanding"] is bool)
                    unlockVanillaTraderTraderStanding = (bool)configDict["Unlock_VanilaTrader_TraderStanding"];
                if (configDict.ContainsKey("Unlock_VanillaLocked_recipe") && configDict["Unlock_VanillaLocked_recipe"] is bool)
                    unlockVanillaLockedRecipe = (bool)configDict["Unlock_VanillaLocked_recipe"];
                if (configDict.ContainsKey("Unlock_VanillaLocked_Customization") && configDict["Unlock_VanillaLocked_Customization"] is bool)
                    unlockVanillaLockedCustomization = (bool)configDict["Unlock_VanillaLocked_Customization"];

                // Cash wipe coefficient
                if (configDict.ContainsKey("Cash_Wipe_Coefficiency"))
                {
                    if (configDict["Cash_Wipe_Coefficiency"] is long)
                        cashWipeCoefficiency = (long)configDict["Cash_Wipe_Coefficiency"];
                    else if (configDict["Cash_Wipe_Coefficiency"] is double)
                        cashWipeCoefficiency = (float)(double)configDict["Cash_Wipe_Coefficiency"];
                }
            }
        }

        private void SaveSettingsToJson()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                JObject configObj;
                if (File.Exists(configPath))
                {
                    string existingJson = File.ReadAllText(configPath);
                    configObj = JObject.Parse(existingJson);
                }
                else
                {
                    configObj = new JObject();
                }

                configObj["Disable_Vanilla_Quests"] = disableVanillaQuests;
                configObj["Unlock_VanillaLocked_Items"] = unlockVanillaLockedItems;
                configObj["Change_Prestige_Conditions"] = changePrestigeCondition;
                configObj["Increase_HeadHP"] = increaseHeadHP;
                configObj["Lock_Flea"] = lockFlea;
                configObj["Enable_empty_vanilla_shop"] = enableEmptyVanillaShop;
                configObj["Enable_No_Insurance"] = enableNoInsurance;
                configObj["Enable_Cash_Wipe"] = enableCashWipeAfterDeath;
                configObj["Add_HideoutProduction_DSP"] = addHideoutProductionDSP;
                configObj["Add_HideoutProduction_Labryskeycard"] = addHideoutProductionLabryskeycard;
                configObj["Unlock_All_Labrys_Quests"] = unlockAllLabrysQuests;
                configObj["Use_Preset"] = usePreset;
                configObj["Enable_Quest_Generator"] = enableQuestGenerator;
                configObj["Enable_Jianghu_Bot"] = enableJianghuBot;
                configObj["Enable_Jianghu_BotName"] = enableJianghuBotName;
                configObj["Enable_Jianghu_BotName_ch"] = botNameLanguageSettings["ch"];
                configObj["Enable_Jianghu_BotName_en"] = botNameLanguageSettings["en"];
                configObj["Enable_Jianghu_BotName_es"] = botNameLanguageSettings["es"];
                configObj["Enable_Jianghu_BotName_fr"] = botNameLanguageSettings["fr"];
                configObj["Enable_Jianghu_BotName_jp"] = botNameLanguageSettings["jp"];
                configObj["Enable_Jianghu_BotName_po"] = botNameLanguageSettings["po"];
                configObj["Enable_Jianghu_BotName_ru"] = botNameLanguageSettings["ru"];
                configObj["Enable_Replace_OneRaid_with_OneLife"] = enableReplaceOneRaidWithOneLife;
                configObj["Enable_New_Movement"] = enableNewMovement;
                configObj["Enable_Fast_Movement"] = enableFastMovement;
                configObj["Enable_Fast_Leaning"] = enableFastLeaning;
                configObj["Enable_Fast_Pose_Transition"] = enableFastPoseTransition;
                configObj["Enable_Jump_Higher"] = enableJumpHigher;
                configObj["Enable_Slide"] = enableSlide;
                configObj["Enable_Fast_Weapon_Switching"] = enableFastWeapon;
                configObj["Enable_Minimal_Aimpunch"] = enableMinimalAimpunch;
                configObj["Enable_Fast_Aiming"] = enableFastAiming;
                configObj["Enable_Wider_Freelook_Angle"] = enableWiderFreelook;
                configObj["Enable_New_Trader"] = enableNewTrader;
                configObj["Enable_New_Quest"] = enableNewQuest;
                configObj["Enable_New_Item"] = enableNewItem;
                configObj["Enable_Arena_Mode"] = enableArenaMode;
                configObj["Restart_Arena_Mode"] = restartArenaMode;
                configObj["Enable_Greeting_Log"] = enableGreetingLog;
                configObj["Enable_Dogtag_Collection"] = enableDogtagCollection;
                configObj["Remove_VanillaQuest_XP_reward"] = removeVanillaQuestXPReward;
                configObj["Unlock_VanilaTrader_TraderStanding"] = unlockVanillaTraderTraderStanding;
                configObj["Unlock_VanillaLocked_recipe"] = unlockVanillaLockedRecipe;
                configObj["Unlock_VanillaLocked_Customization"] = unlockVanillaLockedCustomization;
                configObj["Cash_Wipe_Coefficiency"] = cashWipeCoefficiency;

                string json = JsonConvert.SerializeObject(configObj, Formatting.Indented);
                File.WriteAllText(configPath, json);

                Debug.Log("✅ [JiangHu] Settings saved to JSON");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [JiangHu] Error saving settings: {ex.Message}");
            }
        }

        private Dictionary<string, bool> botNameLanguageSettings = new Dictionary<string, bool>
        {
            { "ch", false }, { "en", false }, { "es", false }, { "fr", false }, 
            { "jp", false }, { "po", false }, { "ru", false }
        };

        private void LoadStoryModeGuide(string languageCode)
        {
            try
            {
                string guidePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "BepInEx", "plugins", "JiangHu.Client", "story_mode_description", $"{languageCode}.md");

                if (File.Exists(guidePath))
                {
                    storyModeContent = File.ReadAllText(guidePath);
                }
                else
                {
                    storyModeContent = $"# Story Mode Guide - {languageCode}\n\nFile not found at: {guidePath}";
                }
            }
            catch (System.Exception ex)
            {
                storyModeContent = $"# Story Mode Guide\n\nError: {ex.Message}";
            }
        }

        private void LoadSettingsGuide(string languageCode)
        {
            try
            {
                string guidePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "BepInEx", "plugins", "JiangHu.Client", "rule_setting_description", $"{languageCode}.md");

                Debug.Log($"🔍 [JiangHu] Looking for guide at: {guidePath}");
                Debug.Log($"🔍 [JiangHu] File exists: {File.Exists(guidePath)}");

                if (File.Exists(guidePath))
                {
                    settingsGuideContent = File.ReadAllText(guidePath);
                    Debug.Log($"✅ [JiangHu] Loaded guide for: {languageCode}");
                }
                else
                {
                    settingsGuideContent = $"# Settings Guide - {languageCode}\n\nFile not found at: {guidePath}";
                    Debug.LogError($"❌ [JiangHu] Guide file NOT FOUND: {guidePath}");

                    string dirPath = Path.GetDirectoryName(guidePath);
                    if (Directory.Exists(dirPath))
                    {
                        var files = Directory.GetFiles(dirPath, "*.md");
                        Debug.Log($"📁 [JiangHu] Files in directory: {string.Join(", ", files)}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                settingsGuideContent = $"# Settings Guide\n\nError: {ex.Message}";
                Debug.LogError($"❌ [JiangHu] Error loading guide: {ex.Message}");
            }
        }

        void OnGUI()
        {
            if (!showGUI) return; 

            if (showSettingsGuide)
            {
                guideWindowRect = GUI.Window(12348, guideWindowRect, DrawSettingsGuideWindow, "Settings Guide  设置指南");
            }
            else if (showBotNameGUI)
            {
                botNameWindowRect = GUI.Window(12350, botNameWindowRect, DrawBotNameSelectionWindow, "Languages  多国语言");
            }
            else if (showMovementSettingsGUI)
            {
                movementSettingsWindowRect = GUI.Window(12351, movementSettingsWindowRect, DrawMovementSettingsWindow, "Mode Settings  心法设置");
            }
            else if (showRuleSettingsGUI)
            {
                ruleSettingsWindowRect = GUI.Window(12352, ruleSettingsWindowRect, DrawRuleSettingsWindow, "Rule Settings  规则设置");
            }
            else if (showCoreModulesGUI)
            {
                coreModulesWindowRect = GUI.Window(12353, coreModulesWindowRect, DrawCoreModulesWindow, "Core Modules  核心模块");
            }
            if (showStoryModeGuide)
            {
                storyModeWindowRect = GUI.Window(12354, storyModeWindowRect, DrawStoryModeGuideWindow, "Story Mode Guide  剧情模式指南");
            }
            else
            {
                windowRect = GUI.Window(12349, windowRect, DrawSettingsWindow, "Game Setting Manager  江湖设置管理器");
            }
        }

        void DrawSettingsWindow(int windowID)
        {
            if (GUI.Button(new Rect(windowRect.width - 25, 5, 20, 20), "X"))
            {
                showGUI = false;
                showSettingsManager.Value = false;
                return;
            }

            GUI.DragWindow(new Rect(0, 0, windowRect.width - 25, 20));

            GUILayout.Space(10);

            // Game Mode
            GUILayout.BeginVertical("box");
            GUILayout.Label("Game Mode  游戏模式", GUIStyle.none);
            GUILayout.Space(5);
            
            bool storyMode = GUILayout.Toggle(usePreset, " Story Mode  剧情模式");
            if (storyMode != usePreset) { usePreset = storyMode; SaveSettingsToJson(); }
            GUILayout.Space(5);
            GUILayout.Label("Can only change settings marked with ***  仅可修改带***的设置");
            GUILayout.Space(5);
            if (GUILayout.Button("Guide  指南"))
            {
                LoadStoryModeGuide(currentLanguage);
                showStoryModeGuide = true;
            }

            bool freeMode = GUILayout.Toggle(!usePreset, " Free Mode  自由模式");
            if (freeMode == usePreset) { usePreset = !freeMode; SaveSettingsToJson(); }
            GUILayout.Space(5);
            GUILayout.Label("Can change each setting freely  可自由修改各项设置");
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // FPS Mode Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Competitive FPS Style  竞技射击风格", GUIStyle.none);
            GUILayout.Space(5);

            bool newEnableMovement = GUILayout.Toggle(enableNewMovement, " Enable Floating Steps Over Ripples  开启凌波微步");
            if (newEnableMovement != enableNewMovement)
            {
                enableNewMovement = newEnableMovement;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            if (GUILayout.Button("Mode Settings  心法设置"))
            {
                showMovementSettingsGUI = true;
            }
            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Dance on the Razor's Edge Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("New Arena  惊鸿猎", GUIStyle.none);
            GUILayout.Space(5);

            bool ArenaMode = GUILayout.Toggle(enableArenaMode, " Enable Arena  开启惊鸿猎");
            if (ArenaMode != enableArenaMode) { enableArenaMode = ArenaMode; SaveSettingsToJson(); }
            GUILayout.Space(5);
            bool restartRaidMode = GUILayout.Toggle(restartArenaMode, " Restart Arena  重置惊鸿猎");
            if (restartRaidMode != restartArenaMode) { restartArenaMode = restartRaidMode; SaveSettingsToJson(); }

            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Three Body Mode Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Three Body Collect  三体搜集", GUIStyle.none);
            GUILayout.Space(5);

            bool newDogtagCollection = GUILayout.Toggle(enableDogtagCollection, " Enable (Use JiangHu Bot Name)  开启。需使用江湖人机名字");
            if (newDogtagCollection != enableDogtagCollection)
            {
                enableDogtagCollection = newDogtagCollection;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Bot Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Bot  人机", GUIStyle.none);
            GUILayout.Space(5);

            bool newEnableBot = GUILayout.Toggle(enableJianghuBot, " Enable Jianghu Bot  江湖人机");
            if (newEnableBot != enableJianghuBot)
            {
                enableJianghuBot = newEnableBot;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            bool newEnableBotName = GUILayout.Toggle(enableJianghuBotName, " Enable Jianghu Bot Names  江湖人机名字");
            if (newEnableBotName != enableJianghuBotName)
            {
                enableJianghuBotName = newEnableBotName;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            if (GUILayout.Button("Languages  多国语言 ***"))
            {
                showBotNameGUI = true;
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);


            // Quest Generator Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Quest Generator  任务生成器", GUIStyle.none);
            GUILayout.Space(5);

            bool newQuestGen = GUILayout.Toggle(enableQuestGenerator, " Enable (Need ‘Disable Vanilla Quests’)  开启。需先禁用原版任务 ***");
            if (newQuestGen != enableQuestGenerator)
            {
                enableQuestGenerator = newQuestGen;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Guide
            GUILayout.BeginVertical("box");
            GUILayout.Label("More Settings  更多设置", GUIStyle.none);
            GUILayout.Space(5);
            if (GUILayout.Button("More Rule Settings    更多规则设置"))
            {
                showRuleSettingsGUI = true;
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Core Modules Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Core Modules  核心模块", GUIStyle.none);
            GUILayout.Space(5);

            if (GUILayout.Button("Core Modules Settings  核心模块设置"))
            {
                showCoreModulesGUI = true;
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Guide
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Settings Guide    设置指南"))
            {
                LoadSettingsGuide(currentLanguage);
                showSettingsGuide = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace(); 
            GUI.DragWindow(new Rect(0, windowRect.height - 30, windowRect.width, 30));
        }

        void DrawSettingsGuideWindow(int windowID)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;

            if (GUI.Button(new Rect(guideWindowRect.width - 25, 5, 20, 20), "X"))
            {
                showSettingsGuide = false;
                return;
            }

            float buttonWidth = (guideWindowRect.width - 40) / 7f;
            float buttonY = 40f;

            // Languages in alphabetical order
            if (GUI.Button(new Rect(20, buttonY, buttonWidth, 35), "中文", buttonStyle))
            {
                currentLanguage = "ch";
                LoadSettingsGuide("ch");
            }
            if (GUI.Button(new Rect(20 + buttonWidth, buttonY, buttonWidth, 35), "English", buttonStyle))
            {
                currentLanguage = "en";
                LoadSettingsGuide("en");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 2, buttonY, buttonWidth, 35), "Español", buttonStyle))
            {
                currentLanguage = "es";
                LoadSettingsGuide("es");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 3, buttonY, buttonWidth, 35), "Français", buttonStyle))
            {
                currentLanguage = "fr";
                LoadSettingsGuide("fr");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 4, buttonY, buttonWidth, 35), "日本語", buttonStyle))
            {
                currentLanguage = "jp";
                LoadSettingsGuide("jp");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 5, buttonY, buttonWidth, 35), "Português", buttonStyle))
            {
                currentLanguage = "po";
                LoadSettingsGuide("po");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 6, buttonY, buttonWidth, 35), "Русский", buttonStyle))
            {
                currentLanguage = "ru";
                LoadSettingsGuide("ru");
            }

            GUI.DragWindow(new Rect(0, 0, guideWindowRect.width - 120, 25));

            float scrollViewY = buttonY + 45f;
            float scrollViewHeight = guideWindowRect.height - scrollViewY - 10f;

            GUILayout.BeginArea(new Rect(10, scrollViewY, guideWindowRect.width - 20, scrollViewHeight));
            guideScrollPosition = GUILayout.BeginScrollView(guideScrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUIStyle contentStyle = new GUIStyle(GUI.skin.label);
            contentStyle.fontSize = 16;
            contentStyle.wordWrap = true;
            GUILayout.Label(settingsGuideContent);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawBotNameSelectionWindow(int windowID)
        {
            if (GUI.Button(new Rect(botNameWindowRect.width - 25, 5, 20, 20), "X"))
            {
                showBotNameGUI = false;
                return;
            }

            GUI.DragWindow(new Rect(0, 0, botNameWindowRect.width - 25, 20));
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");

            bool newChinese = GUILayout.Toggle(botNameLanguageSettings["ch"], " 使用中文名字");
            if (newChinese != botNameLanguageSettings["ch"])
            {
                botNameLanguageSettings["ch"] = newChinese;
                SaveSettingsToJson();
            }
            GUILayout.Space(10);

            bool newEnglish = GUILayout.Toggle(botNameLanguageSettings["en"], " Use English Bot Names");
            if (newEnglish != botNameLanguageSettings["en"])
            {
                botNameLanguageSettings["en"] = newEnglish;
                SaveSettingsToJson();
            }
            GUILayout.Space(10);

            bool newSpanish = GUILayout.Toggle(botNameLanguageSettings["es"], " usar nombres de bot en español");
            if (newSpanish != botNameLanguageSettings["es"])
            {
                botNameLanguageSettings["es"] = newSpanish;
                SaveSettingsToJson();
            }
            GUILayout.Space(10);

            bool newFrench = GUILayout.Toggle(botNameLanguageSettings["fr"], " utiliser des noms de bot en français");
            if (newFrench != botNameLanguageSettings["fr"])
            {
                botNameLanguageSettings["fr"] = newFrench;
                SaveSettingsToJson();
            }
            GUILayout.Space(10);

            bool newJapanese = GUILayout.Toggle(botNameLanguageSettings["jp"], " 日本語のボット名を使用する");
            if (newJapanese != botNameLanguageSettings["jp"])
            {
                botNameLanguageSettings["jp"] = newJapanese;
                SaveSettingsToJson();
            }
            GUILayout.Space(10);

            bool newPortuguese = GUILayout.Toggle(botNameLanguageSettings["po"], " usar nomes de bot em português");
            if (newPortuguese != botNameLanguageSettings["po"])
            {
                botNameLanguageSettings["po"] = newPortuguese;
                SaveSettingsToJson();
            }
            GUILayout.Space(10);

            bool newRussian = GUILayout.Toggle(botNameLanguageSettings["ru"], " использовать русские имена ботов");
            if (newRussian != botNameLanguageSettings["ru"])
            {
                botNameLanguageSettings["ru"] = newRussian;
                SaveSettingsToJson();
            }

            GUILayout.EndVertical();
        }

        void DrawMovementSettingsWindow(int windowID)
        {
            if (GUI.Button(new Rect(movementSettingsWindowRect.width - 25, 5, 20, 20), "X"))
            {
                showMovementSettingsGUI = false;
                return;
            }

            GUI.DragWindow(new Rect(0, 0, movementSettingsWindowRect.width - 25, 20));
            GUILayout.Space(10);

            GUIStyle whiteLabelStyle = new GUIStyle(GUI.skin.label);
            whiteLabelStyle.normal.textColor = Color.white;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Basic movement (default on)  基础身法（默认开启）", GUIStyle.none);
            GUILayout.Space(5);
            GUILayout.Label("Clean & Smooth, Bunny Hopping, no inertia, etc.", whiteLabelStyle);
            GUILayout.Space(5);
            GUILayout.Label("干净、流畅，可连跳，去除惯性等", whiteLabelStyle);
            GUILayout.EndVertical();         
            GUILayout.Space(5);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Fast Pace Movement  快速身法", GUIStyle.none);
            GUILayout.Space(5);

            bool newFastMove = GUILayout.Toggle(enableFastMovement, " Fast Movement  快速移动");
            if (newFastMove != enableFastMovement) { enableFastMovement = newFastMove; SaveSettingsToJson(); }
            GUILayout.Space(5);

            bool newFastLean = GUILayout.Toggle(enableFastLeaning, " Fast Leaning  快速侧身");
            if (newFastLean != enableFastLeaning) { enableFastLeaning = newFastLean; SaveSettingsToJson(); }
            GUILayout.Space(5);

            bool newFastPose = GUILayout.Toggle(enableFastPoseTransition, " Fast Pose Transition  快速姿势切换");
            if (newFastPose != enableFastPoseTransition) { enableFastPoseTransition = newFastPose; SaveSettingsToJson(); }
            GUILayout.Space(5);

            bool newJumpHigher = GUILayout.Toggle(enableJumpHigher, " Jump Higher  轻功");
            if (newJumpHigher != enableJumpHigher) { enableJumpHigher = newJumpHigher; SaveSettingsToJson(); }
            GUILayout.Space(5);

            bool newSlide = GUILayout.Toggle(enableSlide, " Sprint Slide  滑铲");
            if (newSlide != enableSlide) { enableSlide = newSlide; SaveSettingsToJson(); }
            GUILayout.Space(5);
            GUILayout.EndVertical();
            GUILayout.Space(5);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Weapon Handling  武器操控", GUIStyle.none);
            GUILayout.Space(5);
            bool newFastWeapon = GUILayout.Toggle(enableFastWeapon, " Fast Weapon Switch  快速切枪");
            if (newFastWeapon != enableFastWeapon) { enableFastWeapon = newFastWeapon; SaveSettingsToJson(); }
            GUILayout.Space(5);

            bool newFastAiming = GUILayout.Toggle(enableFastAiming, " Fast Aiming  快速瞄准");
            if (newFastAiming != enableFastAiming) { enableFastAiming = newFastAiming; SaveSettingsToJson(); }
            GUILayout.Space(5);

            bool newMinimalAimpunch = GUILayout.Toggle(enableMinimalAimpunch, " Minimal Aimpunch  减少被击中晃动");
            if (newMinimalAimpunch != enableMinimalAimpunch) { enableMinimalAimpunch = newMinimalAimpunch; SaveSettingsToJson(); }
            GUILayout.Space(5);

            GUILayout.EndVertical();
            GUILayout.Space(5);

            GUILayout.BeginVertical("box");
            GUILayout.Label("View  视野", GUIStyle.none);
            GUILayout.Space(5);
            bool newWiderLook = GUILayout.Toggle(enableWiderFreelook, " Wider Freelook  更宽自由视角");
            if (newWiderLook != enableWiderFreelook) { enableWiderFreelook = newWiderLook; SaveSettingsToJson(); }
            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        void DrawRuleSettingsWindow(int windowID)
        {
            if (GUI.Button(new Rect(ruleSettingsWindowRect.width - 25, 5, 20, 20), "X"))
            {
                showRuleSettingsGUI = false;
                return;
            }

            GUI.DragWindow(new Rect(0, 0, ruleSettingsWindowRect.width - 25, 20));


            // Upper Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Gameplay Rules", GUIStyle.none);
            GUILayout.Space(5);

            bool newDisableQuests = GUILayout.Toggle(disableVanillaQuests, " Disable Vanilla Quests  禁原版任务");
            if (newDisableQuests != disableVanillaQuests)
            {
                disableVanillaQuests = newDisableQuests;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);

            bool newUnlockItems = GUILayout.Toggle(unlockVanillaLockedItems, " Unlock Vanilla Locked Items  解锁原版锁定物品");
            if (newUnlockItems != unlockVanillaLockedItems)
            {
                unlockVanillaLockedItems = newUnlockItems;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);

            bool newRemoveXPReward = GUILayout.Toggle(removeVanillaQuestXPReward, " Remove Vanilla Quest XP Reward  移除原版任务经验奖励");
            if (newRemoveXPReward != removeVanillaQuestXPReward)
            {
                removeVanillaQuestXPReward = newRemoveXPReward;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);

            bool newUnlockTraderStanding = GUILayout.Toggle(unlockVanillaTraderTraderStanding, " Unlock Traders and Max all Standing  解锁商人并满好感度");
            if (newUnlockTraderStanding != unlockVanillaTraderTraderStanding)
            {
                unlockVanillaTraderTraderStanding = newUnlockTraderStanding;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);

            bool newUnlockRecipe = GUILayout.Toggle(unlockVanillaLockedRecipe, " Unlock Vanilla Locked Recipes  解锁原版锁定配方");
            if (newUnlockRecipe != unlockVanillaLockedRecipe)
            {
                unlockVanillaLockedRecipe = newUnlockRecipe;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);

            bool newUnlockCustomization = GUILayout.Toggle(unlockVanillaLockedCustomization, " Unlock Vanilla Locked Customization  解锁原版锁定装扮");
            if (newUnlockCustomization != unlockVanillaLockedCustomization)
            {
                unlockVanillaLockedCustomization = newUnlockCustomization;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);

            bool newLockFlea = GUILayout.Toggle(lockFlea, " Lock Flea Market  锁跳蚤市场");
            if (newLockFlea != lockFlea)
            {
                lockFlea = newLockFlea;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            bool newNoInsurance = GUILayout.Toggle(enableNoInsurance, " Disable Insurance  禁保险");
            if (newNoInsurance != enableNoInsurance)
            {
                enableNoInsurance = newNoInsurance;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            bool newEmptyShop = GUILayout.Toggle(enableEmptyVanillaShop, " Empty Trader Shops  禁商店");
            if (newEmptyShop != enableEmptyVanillaShop)
            {
                enableEmptyVanillaShop = newEmptyShop;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            bool newCashWipe = GUILayout.Toggle(enableCashWipeAfterDeath, " Cash Wipe on Death (Protects first million)  死亡清空现金 (100万保底)");
            if (newCashWipe != enableCashWipeAfterDeath)
            {
                enableCashWipeAfterDeath = newCashWipe;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);

            GUILayout.Label("     Cash Wipe Coefficient  现金清除系数");
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(25);
            float newCoefficiency = GUILayout.HorizontalSlider(cashWipeCoefficiency, 0f, 1f, GUILayout.Width(200));
            GUILayout.Label(cashWipeCoefficiency.ToString("F2"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            if (newCoefficiency != cashWipeCoefficiency)
            {
                cashWipeCoefficiency = newCoefficiency;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);

            bool newHeadHP = GUILayout.Toggle(increaseHeadHP, " Increase Head HP  大头");
            if (newHeadHP != increaseHeadHP)
            {
                increaseHeadHP = newHeadHP;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            bool newUnlockLabrysQuests = GUILayout.Toggle(unlockAllLabrysQuests, " Unlock All Labrys Quests  解锁迷宫任务");
            if (newUnlockLabrysQuests != unlockAllLabrysQuests)
            {
                unlockAllLabrysQuests = newUnlockLabrysQuests;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            bool newLabKey = GUILayout.Toggle(addHideoutProductionLabryskeycard, " Hideout Recipe: Labrys Keycard  制造迷宫钥匙");
            if (newLabKey != addHideoutProductionLabryskeycard)
            {
                addHideoutProductionLabryskeycard = newLabKey;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            bool newOneLife = GUILayout.Toggle(enableReplaceOneRaidWithOneLife, " Replace new quest 1 Raid requirement with 1 Life  新任务的单局完成改为一命完成 ***");
            if (newOneLife != enableReplaceOneRaidWithOneLife)
            {
                enableReplaceOneRaidWithOneLife = newOneLife;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            GUILayout.EndVertical();

            // Bottom Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Core Rules", GUIStyle.none);
            GUILayout.Space(5);

            bool newPrestige = GUILayout.Toggle(changePrestigeCondition, " Change Prestige Conditions  改变升级荣誉条件");
            if (newPrestige != changePrestigeCondition)
            {
                changePrestigeCondition = newPrestige;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            bool newDSP = GUILayout.Toggle(addHideoutProductionDSP, " Hideout Recipe: encoded DSP  制造访问灯塔道具");
            if (newDSP != addHideoutProductionDSP)
            {
                addHideoutProductionDSP = newDSP;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        void DrawCoreModulesWindow(int windowID)
        {
            if (GUI.Button(new Rect(coreModulesWindowRect.width - 25, 5, 20, 20), "X"))
            {
                showCoreModulesGUI = false;
                return;
            }

            GUI.DragWindow(new Rect(0, 0, coreModulesWindowRect.width - 25, 20));
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");

            bool newTrader = GUILayout.Toggle(enableNewTrader, " Enable New Trader  启用新商人");
            if (newTrader != enableNewTrader) { enableNewTrader = newTrader; SaveSettingsToJson(); }
            GUILayout.Space(5);

            bool newQuest = GUILayout.Toggle(enableNewQuest, " Enable New Quest  启用新任务");
            if (newQuest != enableNewQuest) { enableNewQuest = newQuest; SaveSettingsToJson(); }
            GUILayout.Space(5);

            bool newItem = GUILayout.Toggle(enableNewItem, " Enable New Item  启用新物品");
            if (newItem != enableNewItem) { enableNewItem = newItem; SaveSettingsToJson(); }
            GUILayout.Space(5);

            bool newGreetingLog = GUILayout.Toggle(enableGreetingLog, " Enable Greeting Log  启用问候日志");
            if (newGreetingLog != enableGreetingLog) { enableGreetingLog = newGreetingLog; SaveSettingsToJson(); }

            GUILayout.EndVertical();
        }

        void DrawStoryModeGuideWindow(int windowID)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;

            if (GUI.Button(new Rect(storyModeWindowRect.width - 25, 5, 20, 20), "X"))
            {
                showStoryModeGuide = false;
                return;
            }

            float buttonWidth = (storyModeWindowRect.width - 40) / 7f;
            float buttonY = 40f;

            // Languages in alphabetical order
            if (GUI.Button(new Rect(20, buttonY, buttonWidth, 35), "中文", buttonStyle))
            {
                currentLanguage = "ch";
                LoadStoryModeGuide("ch");
            }
            if (GUI.Button(new Rect(20 + buttonWidth, buttonY, buttonWidth, 35), "English", buttonStyle))
            {
                currentLanguage = "en";
                LoadStoryModeGuide("en");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 2, buttonY, buttonWidth, 35), "Español", buttonStyle))
            {
                currentLanguage = "es";
                LoadStoryModeGuide("es");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 3, buttonY, buttonWidth, 35), "Français", buttonStyle))
            {
                currentLanguage = "fr";
                LoadStoryModeGuide("fr");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 4, buttonY, buttonWidth, 35), "日本語", buttonStyle))
            {
                currentLanguage = "jp";
                LoadStoryModeGuide("jp");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 5, buttonY, buttonWidth, 35), "Português", buttonStyle))
            {
                currentLanguage = "po";
                LoadStoryModeGuide("po");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 6, buttonY, buttonWidth, 35), "Русский", buttonStyle))
            {
                currentLanguage = "ru";
                LoadStoryModeGuide("ru");
            }

            GUI.DragWindow(new Rect(0, 0, storyModeWindowRect.width - 120, 25));

            float scrollViewY = buttonY + 45f;
            float scrollViewHeight = storyModeWindowRect.height - scrollViewY - 10f;

            GUILayout.BeginArea(new Rect(10, scrollViewY, storyModeWindowRect.width - 20, scrollViewHeight));
            storyModeScrollPosition = GUILayout.BeginScrollView(storyModeScrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUIStyle contentStyle = new GUIStyle(GUI.skin.label);
            contentStyle.fontSize = 16;
            contentStyle.wordWrap = true;
            GUILayout.Label(storyModeContent);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}