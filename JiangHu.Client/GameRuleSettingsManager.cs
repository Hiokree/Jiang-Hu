using UnityEngine;
using BepInEx.Configuration;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

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
        private Rect botNameWindowRect = new Rect(250, 150, 400, 300);
        private bool enableReplaceOneRaidWithOneLife = true;

        private ConfigEntry<bool> showSettingsManager;

        private Rect windowRect = new Rect(300, 100, 550, 700);
        private bool showGUI = false;
        private Vector2 scrollPosition = Vector2.zero;

        private bool showSettingsGuide = false;
        private Rect guideWindowRect = new Rect(200, 100, 600, 500);
        private Vector2 guideScrollPosition = Vector2.zero;
        private string settingsGuideContent = "";
        private string currentLanguage = "en";

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
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                if (File.Exists(configPath))
                {
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

                        Debug.Log("✅ [JiangHu] Settings loaded from JSON");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [JiangHu] Error loading settings: {ex.Message}");
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
                    { "Enable_Replace_OneRaid_with_OneLife", enableReplaceOneRaidWithOneLife }
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
            else
            {
                windowRect = GUI.Window(12349, windowRect, DrawSettingsWindow, "JiangHu Setting Manager  江湖设置管理器");
            }
        }

        void DrawSettingsWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUI.Button(new Rect(windowRect.width - 25, 5, 20, 20), "X"))
            {
                showGUI = false;
                showSettingsManager.Value = false;
                return;
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, windowRect.width - 25, 20));
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Quest Generator  任务生成器", GUIStyle.none);
            GUILayout.Space(5);

            bool newQuestGen = GUILayout.Toggle(enableQuestGenerator, " Enable Quest Generator   (requires Disable Vanilla Quests)");
            if (newQuestGen != enableQuestGenerator)
            {
                enableQuestGenerator = newQuestGen;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            // Jianghu Bot Settings
            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            GUILayout.Label("Jianghu Bot  江湖人机", GUIStyle.none);
            GUILayout.Space(5);

            bool newEnableBot = GUILayout.Toggle(enableJianghuBot, " Enable Jianghu Bot  使用江湖人机");
            if (newEnableBot != enableJianghuBot)
            {
                enableJianghuBot = newEnableBot;
                SaveSettingsToJson();
            }

            bool newEnableBotName = GUILayout.Toggle(enableJianghuBotName, " Enable Jianghu Bot Names  使用江湖人机名字");
            if (newEnableBotName != enableJianghuBotName)
            {
                enableJianghuBotName = newEnableBotName;
                SaveSettingsToJson();
            }
            GUILayout.Space(5);
            if (GUILayout.Button("Languages  多国语言"))
            {
                showBotNameGUI = true;
            }
            GUILayout.EndVertical();


            GUILayout.Space(10);

            GUILayout.Label("PRESET (toggle off to change rule settings)", GUIStyle.none);
            GUILayout.Space(5);

            // Preset section
            GUILayout.BeginVertical("box");

            bool newUsePreset = GUILayout.Toggle(usePreset, " Use Preset Configuration  使用预设");
            if (newUsePreset != usePreset)
            {
                usePreset = newUsePreset;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            GUILayout.Space(15);
            GUILayout.Label("RULE SETTINGS (applied at server start)", GUIStyle.none);
            GUILayout.Space(5);

            // Upper Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Gameplay", GUIStyle.none);
            GUILayout.Space(5);

            bool newDisableQuests = GUILayout.Toggle(disableVanillaQuests, " Disable Vanilla Quests  禁原版任务");
            if (newDisableQuests != disableVanillaQuests)
            {
                disableVanillaQuests = newDisableQuests;
                SaveSettingsToJson();
            }

            bool newLockFlea = GUILayout.Toggle(lockFlea, " Lock Flea Market  锁跳蚤市场");
            if (newLockFlea != lockFlea)
            {
                lockFlea = newLockFlea;
                SaveSettingsToJson();
            }

            bool newNoInsurance = GUILayout.Toggle(enableNoInsurance, " Disable Insurance  禁保险");
            if (newNoInsurance != enableNoInsurance)
            {
                enableNoInsurance = newNoInsurance;
                SaveSettingsToJson();
            }

            bool newEmptyShop = GUILayout.Toggle(enableEmptyVanillaShop, " Empty Trader Shops  禁商店");
            if (newEmptyShop != enableEmptyVanillaShop)
            {
                enableEmptyVanillaShop = newEmptyShop;
                SaveSettingsToJson();
            }

            bool newCashWipe = GUILayout.Toggle(enableCashWipeAfterDeath, " Cash Wipe on Death  死亡清空现金");
            if (newCashWipe != enableCashWipeAfterDeath)
            {
                enableCashWipeAfterDeath = newCashWipe;
                SaveSettingsToJson();
            }

            bool newHeadHP = GUILayout.Toggle(increaseHeadHP, " Increase Head HP  大头");
            if (newHeadHP != increaseHeadHP)
            {
                increaseHeadHP = newHeadHP;
                SaveSettingsToJson();
            }

            bool newUnlockLabrysQuests = GUILayout.Toggle(unlockAllLabrysQuests, " Unlock All Labrys Quests  解锁迷宫任务");
            if (newUnlockLabrysQuests != unlockAllLabrysQuests)
            {
                unlockAllLabrysQuests = newUnlockLabrysQuests;
                SaveSettingsToJson();
            }

            bool newLabKey = GUILayout.Toggle(addHideoutProductionLabryskeycard, " Hideout Recipe: Labrys Keycard  制造迷宫钥匙");
            if (newLabKey != addHideoutProductionLabryskeycard)
            {
                addHideoutProductionLabryskeycard = newLabKey;
                SaveSettingsToJson();
            }

            bool newOneLife = GUILayout.Toggle(enableReplaceOneRaidWithOneLife, " Replace new quest 1 Raid requirement with 1 Life    新任务的单局完成改为一命完成");
            if (newOneLife != enableReplaceOneRaidWithOneLife)
            {
                enableReplaceOneRaidWithOneLife = newOneLife;
                SaveSettingsToJson();
            }

            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Bottom Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Core", GUIStyle.none);
            GUILayout.Space(5);


            bool newUnlockItems = GUILayout.Toggle(unlockAllItemsByNewQuest, " Unlock Items by 1 New Quest  新任务解锁全部物品");
            if (newUnlockItems != unlockAllItemsByNewQuest)
            {
                unlockAllItemsByNewQuest = newUnlockItems;
                SaveSettingsToJson();
            }

            bool newPrestige = GUILayout.Toggle(changePrestigeCondition, " Change Prestige Conditions  改变升级荣誉条件");
            if (newPrestige != changePrestigeCondition)
            {
                changePrestigeCondition = newPrestige;
                SaveSettingsToJson();
            }

            bool newDSP = GUILayout.Toggle(addHideoutProductionDSP, " Hideout Recipe: encoded DSP  制造访问灯塔道具");
            if (newDSP != addHideoutProductionDSP)
            {
                addHideoutProductionDSP = newDSP;
                SaveSettingsToJson();
            }

            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Settings Guide  设置指南"))
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

            // Updated to 7 buttons for the new layout
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
    }
}