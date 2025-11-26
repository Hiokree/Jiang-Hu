using BepInEx.Configuration;
using EFT.UI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JiangHu
{
    public class RuleSettingsManager : MonoBehaviour
    {
        private bool disableVanillaQuests = true;
        private bool unlockAllItemsByNewQuest = true;
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
        private bool enableNewRaidMode = true;
        private bool restartNewRaidMode = false;
        private bool enableGreetingLog = true;
        private bool showCoreModulesGUI = false;
        private bool enableDogtagCollection = true;


        private Rect coreModulesWindowRect = new Rect(350, 150, 360, 160);

        private bool showRuleSettingsGUI = false;
        private Rect ruleSettingsWindowRect = new Rect(350, 150, 550, 420);
        private Rect windowRect = new Rect(300, 100, 500, 700);
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
            var configDict = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);

            if (configDict != null)
            {
                if (configDict.ContainsKey("Disable_Vanilla_Quests"))
                    disableVanillaQuests = configDict["Disable_Vanilla_Quests"];
                if (configDict.ContainsKey("Unlock_AllItems_By_NewQuest"))
                    unlockAllItemsByNewQuest = configDict["Unlock_AllItems_By_NewQuest"];
                if (configDict.ContainsKey("Change_Prestige_Conditions"))
                    changePrestigeCondition = configDict["Change_Prestige_Conditions"];
                if (configDict.ContainsKey("Increase_HeadHP"))
                    increaseHeadHP = configDict["Increase_HeadHP"];
                if (configDict.ContainsKey("Lock_Flea"))
                    lockFlea = configDict["Lock_Flea"];
                if (configDict.ContainsKey("Enable_empty_vanilla_shop"))
                    enableEmptyVanillaShop = configDict["Enable_empty_vanilla_shop"];
                if (configDict.ContainsKey("Enable_No_Insurance"))
                    enableNoInsurance = configDict["Enable_No_Insurance"];
                if (configDict.ContainsKey("Enable_Cash_Wipe"))
                    enableCashWipeAfterDeath = configDict["Enable_Cash_Wipe"];
                if (configDict.ContainsKey("Add_HideoutProduction_DSP"))
                    addHideoutProductionDSP = configDict["Add_HideoutProduction_DSP"];
                if (configDict.ContainsKey("Add_HideoutProduction_Labryskeycard"))
                    addHideoutProductionLabryskeycard = configDict["Add_HideoutProduction_Labryskeycard"];
                if (configDict.ContainsKey("Unlock_All_Labrys_Quests"))
                    unlockAllLabrysQuests = configDict["Unlock_All_Labrys_Quests"];
                if (configDict.ContainsKey("Use_Preset"))
                    usePreset = configDict["Use_Preset"];
                if (configDict.ContainsKey("Enable_Quest_Generator"))
                    enableQuestGenerator = configDict["Enable_Quest_Generator"];
                if (configDict.ContainsKey("Enable_Jianghu_Bot"))
                    enableJianghuBot = configDict["Enable_Jianghu_Bot"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName"))
                    enableJianghuBotName = configDict["Enable_Jianghu_BotName"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_ch"))
                    botNameLanguageSettings["ch"] = configDict["Enable_Jianghu_BotName_ch"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_en"))
                    botNameLanguageSettings["en"] = configDict["Enable_Jianghu_BotName_en"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_es"))
                    botNameLanguageSettings["es"] = configDict["Enable_Jianghu_BotName_es"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_fr"))
                    botNameLanguageSettings["fr"] = configDict["Enable_Jianghu_BotName_fr"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_jp"))
                    botNameLanguageSettings["jp"] = configDict["Enable_Jianghu_BotName_jp"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_po"))
                    botNameLanguageSettings["po"] = configDict["Enable_Jianghu_BotName_po"];
                if (configDict.ContainsKey("Enable_Jianghu_BotName_ru"))
                    botNameLanguageSettings["ru"] = configDict["Enable_Jianghu_BotName_ru"];
                if (configDict.ContainsKey("Enable_Replace_OneRaid_with_OneLife"))
                    enableReplaceOneRaidWithOneLife = configDict["Enable_Replace_OneRaid_with_OneLife"];
                if (configDict.ContainsKey("Enable_New_Movement"))
                    enableNewMovement = configDict["Enable_New_Movement"];
                if (configDict.ContainsKey("Enable_Fast_Movement"))
                    enableFastMovement = configDict["Enable_Fast_Movement"];
                if (configDict.ContainsKey("Enable_Fast_Leaning"))
                    enableFastLeaning = configDict["Enable_Fast_Leaning"];
                if (configDict.ContainsKey("Enable_Fast_Pose_Transition"))
                    enableFastPoseTransition = configDict["Enable_Fast_Pose_Transition"];
                if (configDict.ContainsKey("Enable_Jump_Higher"))
                    enableJumpHigher = configDict["Enable_Jump_Higher"];
                if (configDict.ContainsKey("Enable_Slide"))
                    enableSlide = configDict["Enable_Slide"];
                if (configDict.ContainsKey("Enable_Fast_Weapon_Switching"))
                    enableFastWeapon = configDict["Enable_Fast_Weapon_Switching"];
                if (configDict.ContainsKey("Enable_Minimal_Aimpunch"))
                    enableMinimalAimpunch = configDict["Enable_Minimal_Aimpunch"];
                if (configDict.ContainsKey("Enable_Fast_Aiming"))
                    enableFastAiming = configDict["Enable_Fast_Aiming"];
                if (configDict.ContainsKey("Enable_Wider_Freelook_Angle"))
                    enableWiderFreelook = configDict["Enable_Wider_Freelook_Angle"];
                if (configDict.ContainsKey("Enable_New_Trader"))
                    enableNewTrader = configDict["Enable_New_Trader"];
                if (configDict.ContainsKey("Enable_New_Quest"))
                    enableNewQuest = configDict["Enable_New_Quest"];
                if (configDict.ContainsKey("Enable_New_Item"))
                    enableNewItem = configDict["Enable_New_Item"];
                if (configDict.ContainsKey("Enable_New_RaidMode"))
                    enableNewRaidMode = configDict["Enable_New_RaidMode"];
                if (configDict.ContainsKey("Restart_New_RaidMode"))
                    restartNewRaidMode = configDict["Restart_New_RaidMode"];
                if (configDict.ContainsKey("Enable_Greeting_Log"))
                    enableGreetingLog = configDict["Enable_Greeting_Log"];
                if (configDict.ContainsKey("Enable_Dogtag_Collection"))
                    enableDogtagCollection = configDict["Enable_Dogtag_Collection"];
            }              
        }

        private void SaveSettingsToJson()
        {
            try
            {
                var configDict = new Dictionary<string, bool>
                {
                    { "Disable_Vanilla_Quests", disableVanillaQuests },
                    { "Unlock_AllItems_By_NewQuest", unlockAllItemsByNewQuest },
                    { "Change_Prestige_Conditions", changePrestigeCondition },
                    { "Increase_HeadHP", increaseHeadHP },
                    { "Lock_Flea", lockFlea },
                    { "Enable_empty_vanilla_shop", enableEmptyVanillaShop },
                    { "Enable_No_Insurance", enableNoInsurance },
                    { "Enable_Cash_Wipe", enableCashWipeAfterDeath },
                    { "Add_HideoutProduction_DSP", addHideoutProductionDSP },
                    { "Add_HideoutProduction_Labryskeycard", addHideoutProductionLabryskeycard },
                    { "Unlock_All_Labrys_Quests", unlockAllLabrysQuests },
                    { "Use_Preset", usePreset },
                    { "Enable_Quest_Generator", enableQuestGenerator },
                    { "Enable_Jianghu_Bot", enableJianghuBot },
                    { "Enable_Jianghu_BotName", enableJianghuBotName },
                    { "Enable_Jianghu_BotName_ch", botNameLanguageSettings["ch"] },
                    { "Enable_Jianghu_BotName_en", botNameLanguageSettings["en"] },
                    { "Enable_Jianghu_BotName_es", botNameLanguageSettings["es"] },
                    { "Enable_Jianghu_BotName_fr", botNameLanguageSettings["fr"] },
                    { "Enable_Jianghu_BotName_jp", botNameLanguageSettings["jp"] },
                    { "Enable_Jianghu_BotName_po", botNameLanguageSettings["po"] },
                    { "Enable_Jianghu_BotName_ru", botNameLanguageSettings["ru"] },
                    { "Enable_Replace_OneRaid_with_OneLife", enableReplaceOneRaidWithOneLife },
                    { "Enable_New_Movement", enableNewMovement },
                    { "Enable_Fast_Movement", enableFastMovement },
                    { "Enable_Fast_Leaning", enableFastLeaning },
                    { "Enable_Fast_Pose_Transition", enableFastPoseTransition },
                    { "Enable_Jump_Higher", enableJumpHigher },
                    { "Enable_Slide", enableSlide },
                    { "Enable_Fast_Weapon_Switching", enableFastWeapon },
                    { "Enable_Minimal_Aimpunch", enableMinimalAimpunch },
                    { "Enable_Fast_Aiming", enableFastAiming },
                    { "Enable_Wider_Freelook_Angle", enableWiderFreelook },
                    { "Enable_New_Trader", enableNewTrader },
                    { "Enable_New_Quest", enableNewQuest },
                    { "Enable_New_Item", enableNewItem },
                    { "Enable_New_RaidMode", enableNewRaidMode },
                    { "Restart_New_RaidMode", restartNewRaidMode },
                    { "Enable_Greeting_Log", enableGreetingLog },
                    { "Enable_Dogtag_Collection", enableDogtagCollection },
                };

                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                string json = JsonConvert.SerializeObject(configDict, Formatting.Indented);
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

            // Core Modules Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Core Modules", GUIStyle.none);
            GUILayout.Space(5);

            if (GUILayout.Button("Core Modules Settings  核心模块设置"))
            {
                showCoreModulesGUI = true;
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // FPS Mode Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Competitive FPS Mode    竞技射击模式", GUIStyle.none);
            GUILayout.Space(5);

            bool newEnableMovement = GUILayout.Toggle(enableNewMovement, " Enable Floating Steps Over Ripples    开启凌波微步");
            if (newEnableMovement != enableNewMovement)
            {
                enableNewMovement = newEnableMovement;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            if (GUILayout.Button("Mode Settings    心法设置"))
            {
                showMovementSettingsGUI = true;
            }
            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Dance on the Razor's Edge Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("New Arena    惊鸿猎", GUIStyle.none);
            GUILayout.Space(5);

            bool newRaidMode = GUILayout.Toggle(enableNewRaidMode, " Enable Arena    开启惊鸿猎");
            if (newRaidMode != enableNewRaidMode) { enableNewRaidMode = newRaidMode; SaveSettingsToJson(); }
            GUILayout.Space(5);
            bool restartRaidMode = GUILayout.Toggle(restartNewRaidMode, " Restart Arena    重置惊鸿猎");
            if (restartRaidMode != restartNewRaidMode) { restartNewRaidMode = restartRaidMode; SaveSettingsToJson(); }

            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Three Body Mode Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Three Body Mode    三体模式", GUIStyle.none);
            GUILayout.Space(5);

            bool newDogtagCollection = GUILayout.Toggle(enableDogtagCollection, " Enable (Use JiangHu Bot Name)    开启。需使用江湖人机名字");
            if (newDogtagCollection != enableDogtagCollection)
            {
                enableDogtagCollection = newDogtagCollection;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Bot Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Bot    人机", GUIStyle.none);
            GUILayout.Space(5);

            bool newEnableBot = GUILayout.Toggle(enableJianghuBot, " Enable Jianghu Bot    使用江湖人机");
            if (newEnableBot != enableJianghuBot)
            {
                enableJianghuBot = newEnableBot;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            bool newEnableBotName = GUILayout.Toggle(enableJianghuBotName, " Enable Jianghu Bot Names    使用江湖人机名字");
            if (newEnableBotName != enableJianghuBotName)
            {
                enableJianghuBotName = newEnableBotName;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            if (GUILayout.Button("Languages    多国语言"))
            {
                showBotNameGUI = true;
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);


            // Quest Generator Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Quest Generator    任务生成器", GUIStyle.none);
            GUILayout.Space(5);

            bool newQuestGen = GUILayout.Toggle(enableQuestGenerator, " Enable (Need ‘Disable Vanilla Quests’)    开启。需先禁用原版任务");
            if (newQuestGen != enableQuestGenerator)
            {
                enableQuestGenerator = newQuestGen;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Game Rules Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Game Rules    游戏规则", GUIStyle.none);
            GUILayout.Space(5);
            bool newUsePreset = GUILayout.Toggle(usePreset, " Use Preset Configuration    使用预设");
            if (newUsePreset != usePreset) { usePreset = newUsePreset; SaveSettingsToJson(); }
            GUILayout.Space(5);
            if (GUILayout.Button("Rule Settings    规则设置"))
            {
                showRuleSettingsGUI = true;
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
            GUILayout.Label("Basic movement (default on)    基础身法（默认开启）", GUIStyle.none);
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
            bool newCashWipe = GUILayout.Toggle(enableCashWipeAfterDeath, " Cash Wipe on Death  死亡清空现金");
            if (newCashWipe != enableCashWipeAfterDeath)
            {
                enableCashWipeAfterDeath = newCashWipe;
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
            bool newOneLife = GUILayout.Toggle(enableReplaceOneRaidWithOneLife, " Replace new quest 1 Raid requirement with 1 Life    新任务的单局完成改为一命完成");
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


            bool newUnlockItems = GUILayout.Toggle(unlockAllItemsByNewQuest, " Unlock Items by 1 New Quest  新任务解锁全部物品");
            if (newUnlockItems != unlockAllItemsByNewQuest)
            {
                unlockAllItemsByNewQuest = newUnlockItems;
                SaveSettingsToJson();
            }
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
    }
}