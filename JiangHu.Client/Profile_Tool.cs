using BepInEx.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JiangHu
{
    public class DebugTool : MonoBehaviour
    {
        private bool enableDebugTool = true;
        private int finalQueststatus = 0;
        private int playerXP = 0;
        private int addRouble = 0;
        private bool allHideoutLevel = false;

        private ConfigEntry<bool> showDebugTool;

        private Rect windowRect = new Rect(350, 100, 400, 500);
        private bool showGUI = false;
        private Vector2 scrollPosition = Vector2.zero;

        public void SetConfig(ConfigEntry<bool> showDebugTool)
        {
            this.showDebugTool = showDebugTool;
            LoadSettingsFromJson();
        }

        void Update()
        {
            if (showDebugTool.Value && !showGUI)
            {
                showGUI = true;
            }
            else if (!showDebugTool.Value && showGUI)
            {
                showGUI = false;
            }
        }

        private void LoadSettingsFromJson()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "profile_setting.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (configDict != null)
                    {
                        if (configDict.ContainsKey("enable_debug_tool"))
                            enableDebugTool = Convert.ToBoolean(configDict["enable_debug_tool"]);
                        if (configDict.ContainsKey("finalQueststatus"))
                            finalQueststatus = Convert.ToInt32(configDict["finalQueststatus"]);
                        if (configDict.ContainsKey("playerXP"))
                            playerXP = Convert.ToInt32(configDict["playerXP"]);
                        if (configDict.ContainsKey("addrouble"))
                            addRouble = Convert.ToInt32(configDict["addrouble"]);
                        if (configDict.ContainsKey("all_hideout_level"))
                            allHideoutLevel = Convert.ToBoolean(configDict["all_hideout_level"]);

                        Debug.Log("✅ [DebugTool] Settings loaded from JSON");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [DebugTool] Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettingsToJson()
        {
            try
            {
                var configDict = new Dictionary<string, object>
                {
                    { "enable_debug_tool", enableDebugTool },
                    { "finalQueststatus", finalQueststatus },
                    { "playerXP", playerXP },
                    { "addrouble", addRouble },
                    { "all_hideout_level", allHideoutLevel }
                };

                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "profile_setting.json");

                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                string json = JsonConvert.SerializeObject(configDict, Formatting.Indented);
                File.WriteAllText(configPath, json);

                Debug.Log("✅ [DebugTool] Settings saved to JSON");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [DebugTool] Error saving settings: {ex.Message}");
            }
        }

        void OnGUI()
        {
            if (!showGUI) return;

            windowRect = GUI.Window(12351, windowRect, DrawDebugWindow, "Profile Tool");
        }

        void DrawDebugWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUI.Button(new Rect(windowRect.width - 25, 5, 20, 20), "X"))
            {
                showGUI = false;
                showDebugTool.Value = false;
                return;
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, windowRect.width - 25, 20));
            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(450));

            // Enable Debug Tool
            GUILayout.BeginVertical("box");
            bool newEnableDebug = GUILayout.Toggle(enableDebugTool, " Enable Profile Tool");
            if (newEnableDebug != enableDebugTool)
            {
                enableDebugTool = newEnableDebug;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Quest Status
            GUILayout.BeginVertical("box");
            GUILayout.Label("Set Final Quest Status");
            GUILayout.Label("(0=Locked, 2=Started, 3=AvailableForFinish, 4=Success)");
            string questStatusStr = GUILayout.TextField(finalQueststatus.ToString());
            if (int.TryParse(questStatusStr, out int newQuestStatus) && newQuestStatus != finalQueststatus)
            {
                finalQueststatus = newQuestStatus;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Player XP
            GUILayout.BeginVertical("box");
            GUILayout.Label("Player XP:");
            string xpStr = GUILayout.TextField(playerXP.ToString());
            if (int.TryParse(xpStr, out int newXP) && newXP != playerXP)
            {
                playerXP = newXP;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Add Rouble
            GUILayout.BeginVertical("box");
            GUILayout.Label("Auto-adds Roubles at startup:");
            string rubStr = GUILayout.TextField(addRouble.ToString());
            if (int.TryParse(rubStr, out int newRub) && newRub != addRouble)
            {
                addRouble = newRub;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Hideout Level
            GUILayout.BeginVertical("box");
            bool newHideout = GUILayout.Toggle(allHideoutLevel, " All Hideout Level 1");
            if (newHideout != allHideoutLevel)
            {
                allHideoutLevel = newHideout;
                SaveSettingsToJson();
            }
            GUILayout.EndVertical();

            GUILayout.Label("Restart server to apply changes");

            GUILayout.EndScrollView();
        }
    }
}