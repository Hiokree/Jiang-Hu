using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace JiangHu
{
    public class BattleScreenPlugin : MonoBehaviour
    {
        private static ManualLogSource Logger;
        private static BattleScreenPlugin Instance;

        private static List<DamageLogEntry> guiLog = new List<DamageLogEntry>();


        private static List<DamageLogEntry> fileBuffer = new List<DamageLogEntry>();
        private static System.Threading.Timer fileWriteTimer;
        private static int fileWriteInterval = 3000;
        private static bool isTimerRunning = false;

        private static string currentRaidFile;
        private static RaidStats raidStats = new RaidStats();
        private static bool inRaid = false;
        private static float raidStartTime;

        private static bool showGUI = false;

        private static CursorState cursorState = CursorState.Hidden;
        private static Vector2 scrollPosition = Vector2.zero;

        private static GUIStyle guiStyle;
        private static GUIStyle titleStyle;
        private static GUIStyle entryStyle;
        private static GUIStyle detailStyle;
        private static GUIStyle entryStyle2;
        private static GUIStyle entryStyle3;
        private static GUIStyle entryStyle4;
        private static GUIStyle bodyStyle;
        private static GUIStyle armorStyle;
        private static GUIStyle absorbedStyle;


        private static Dictionary<string, float> lastHitTimes = new Dictionary<string, float>();
        private static Dictionary<string, DamageInfoCache> damageInfoCache = new Dictionary<string, DamageInfoCache>();
        private static List<string> killedBots = new List<string>();

        private static Vector2 mainPanelDragOffset = Vector2.zero;
        private static Vector2 killPanelDragOffset = Vector2.zero;
        private static bool draggingMainPanel = false;
        private static bool draggingKillPanel = false;

        private static float mainPanelX = 20f;
        private static float mainPanelY = 100f;
        private static float killPanelX = 0f;
        private static float killPanelY = 0f;

        private enum CursorState
        {
            Hidden,    
            Visible,     
            Unlocked     
        }

        private void UpdateCursorState()
        {
            if (cursorState == CursorState.Hidden)
            {
            }
            else if (cursorState == CursorState.Visible)
            {
            }
            else 
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        void Awake()
        {
            Instance = this;

            try
            {
                var harmony = new Harmony("com.jianghu.battlescreen");
                harmony.PatchAll();

                fileWriteTimer = new System.Threading.Timer(WriteBufferToFile, null,
                    System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load Battle Screen: {ex}");
            }
        }

        void Update()
        {
            if (F12Manager.BattleScreenHotkey.Value.IsDown())
            {
                if (cursorState == CursorState.Hidden)
                {
                    cursorState = CursorState.Visible;
                    showGUI = true;
                }
                else if (cursorState == CursorState.Visible)
                {
                    cursorState = CursorState.Unlocked;
                    showGUI = true;
                }
                else 
                {
                    cursorState = CursorState.Hidden;
                    showGUI = false;
                }
            }

            CleanupLastHitTimes();
            CleanupDamageCache();
            UpdateCursorState();

            if (cursorState == CursorState.Visible && guiLog.Count > 0)
            {
                scrollPosition.y = Mathf.Infinity;
            }
        }

        void OnGUI()
        {
            if (!showGUI || !inRaid) return;

            if (guiStyle == null) InitializeGUIStyles();

            float panelWidth = 320f;
            float panelHeight = Mathf.Min(Screen.height * 0.5f, guiLog.Count * 70f + 170f);

            Rect panelRect = new Rect(mainPanelX, mainPanelY, panelWidth, panelHeight);

            if (cursorState == CursorState.Unlocked)
            {
                Rect dragHandle = new Rect(panelRect.x, panelRect.y, panelRect.width, 40f);
                Vector2 mousePos = Event.current.mousePosition;

                if (Event.current.type == EventType.MouseDown && dragHandle.Contains(mousePos))
                {
                    draggingMainPanel = true;
                    mainPanelDragOffset = new Vector2(mousePos.x - panelRect.x, mousePos.y - panelRect.y);
                }

                if (draggingMainPanel)
                {
                    if (Event.current.type == EventType.MouseDrag)
                    {
                        mainPanelX = mousePos.x - mainPanelDragOffset.x;
                        mainPanelY = mousePos.y - mainPanelDragOffset.y;
                        panelRect = new Rect(mainPanelX, mainPanelY, panelWidth, panelHeight);
                    }

                    if (Event.current.type == EventType.MouseUp)
                    {
                        draggingMainPanel = false;
                    }
                }
            }

            GUI.Box(panelRect, "", guiStyle);

            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 10f, panelWidth - 20f, 30f),
                     "BATTLE SCREEN     锋镝录", titleStyle);

            // Main panel content drawing
            float yPosEntry = panelRect.y + 50f;
            int displayCount = 0;
            int index = 0;
            DamageLogEntry lastDisplayedEntry = null;
            bool displayAllEntries = false;

            float contentHeight = guiLog.Count * 75f + 20f;
            float viewHeight = panelHeight - 100f;

            bool showScroll = (cursorState == CursorState.Unlocked && contentHeight > viewHeight);

            if (showScroll)
            {
                scrollPosition = GUI.BeginScrollView(
                    new Rect(panelRect.x + 5f, panelRect.y + 50f, panelWidth - 10f, viewHeight), 
                    scrollPosition,
                    new Rect(0, 0, 0, contentHeight),
                    false,
                    true);

                float scrollYPos = 0f;
                displayAllEntries = true;

                while ((displayAllEntries && index < guiLog.Count) ||
                       (!displayAllEntries && displayCount < 8 && index < guiLog.Count))
                {
                    var currentEntry = guiLog[index];

                    bool showName = true;
                    if (lastDisplayedEntry != null && currentEntry.targetName == lastDisplayedEntry.targetName)
                    {
                        float timeDiff = Mathf.Abs(currentEntry.timestamp - lastDisplayedEntry.timestamp);
                        showName = timeDiff > 5f;
                    }

                    if (showName)
                    {
                        DrawLogEntryHeaderScroll(currentEntry, 10f, ref scrollYPos, panelWidth - 25f);
                    }

                    lastDisplayedEntry = currentEntry;
                    DrawDamageLinesScroll(currentEntry, 10f, ref scrollYPos, panelWidth - 25f);
                    displayCount++;
                    index++;
                    scrollYPos += 5f;

                    while (index < guiLog.Count &&
                           Mathf.Abs(guiLog[index].timestamp - currentEntry.timestamp) < 0.01f &&
                           guiLog[index].targetName == currentEntry.targetName)
                    {
                        var subsequentEntry = guiLog[index];
                        DrawDamageLinesScroll(subsequentEntry, 10f, ref scrollYPos, panelWidth - 25f);
                        index++;
                        scrollYPos += 5f;
                    }
                }

                GUI.EndScrollView();
            }
            else
            {
                scrollPosition = Vector2.zero;
                yPosEntry = panelRect.y + 50f;
                displayAllEntries = (cursorState == CursorState.Unlocked);

                while ((displayAllEntries && index < guiLog.Count) ||
                       (!displayAllEntries && displayCount < 8 && index < guiLog.Count))
                {
                    var currentEntry = guiLog[index];

                    bool showName = true;
                    if (lastDisplayedEntry != null && currentEntry.targetName == lastDisplayedEntry.targetName)
                    {
                        float timeDiff = Mathf.Abs(currentEntry.timestamp - lastDisplayedEntry.timestamp);
                        showName = timeDiff > 5f;
                    }

                    if (showName)
                    {
                        DrawLogEntryHeader(currentEntry, panelRect.x + 10f, ref yPosEntry, panelWidth - 25f);
                    }

                    lastDisplayedEntry = currentEntry;
                    DrawDamageLines(currentEntry, panelRect.x + 10f, ref yPosEntry, panelWidth - 25f);
                    displayCount++;
                    index++;
                    yPosEntry += 5f;

                    while (index < guiLog.Count &&
                           Mathf.Abs(guiLog[index].timestamp - currentEntry.timestamp) < 0.01f &&
                           guiLog[index].targetName == currentEntry.targetName)
                    {
                        var subsequentEntry = guiLog[index];
                        DrawDamageLines(subsequentEntry, panelRect.x + 10f, ref yPosEntry, panelWidth - 25f);
                        index++;
                        yPosEntry += 5f;
                    }
                }
            }
            // Stats text
            float currentX = panelRect.x + 10f;
            float statsY = panelRect.y + panelHeight - 100f;

            string hitsText = $"Hits: {raidStats.totalHits} | ";
            GUI.Label(new Rect(currentX, statsY, panelWidth - 20f, 25f), hitsText, detailStyle);
            currentX += GetTextWidth(hitsText, detailStyle);

            string bodyText = $"Body: {raidStats.bodyDamage} | ";
            GUI.Label(new Rect(currentX, statsY, panelWidth - 20f, 25f), bodyText, bodyStyle);
            currentX += GetTextWidth(bodyText, bodyStyle);

            string armorText = $"Armor: {raidStats.armorDamage} | ";
            GUI.Label(new Rect(currentX, statsY, panelWidth - 20f, 25f), armorText, armorStyle);
            currentX += GetTextWidth(armorText, armorStyle);

            string absorbedText = $"Absorbed: {raidStats.armorAbsorbedDamage}";
            GUI.Label(new Rect(currentX, statsY, panelWidth - 20f, 25f), absorbedText, absorbedStyle);

            float cnStatsY = statsY + 22f; 
            currentX = panelRect.x + 10f;

            string hitsCN = $"击中: {raidStats.totalHits} | ";
            GUI.Label(new Rect(currentX, cnStatsY, panelWidth - 20f, 25f), hitsCN, detailStyle);
            currentX += GetTextWidth(hitsCN, detailStyle);

            string bodyCN = $"肉伤: {raidStats.bodyDamage} | ";
            GUI.Label(new Rect(currentX, cnStatsY, panelWidth - 20f, 25f), bodyCN, bodyStyle);
            currentX += GetTextWidth(bodyCN, bodyStyle);

            string armorCN = $"甲伤: {raidStats.armorDamage} | ";
            GUI.Label(new Rect(currentX, cnStatsY, panelWidth - 20f, 25f), armorCN, armorStyle);
            currentX += GetTextWidth(armorCN, armorStyle);

            string absorbedCN = $"减伤: {raidStats.armorAbsorbedDamage}";
            GUI.Label(new Rect(currentX, cnStatsY, panelWidth - 20f, 25f), absorbedCN, absorbedStyle);


            // Instruction text
            if (cursorState == CursorState.Visible)
            {
                GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + panelHeight - 45f, panelWidth - 20f, 25f),
                         "Press again to view history/change position", detailStyle);
                GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + panelHeight - 25f, panelWidth - 20f, 25f),
                         "再按一次显示完整战斗日志或调整位置", detailStyle);
            }
            else if (cursorState == CursorState.Unlocked)
            {
                GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + panelHeight - 45f, panelWidth - 20f, 25f),
                         "Drag to move screen, Press again to close", detailStyle);
                GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + panelHeight - 25f, panelWidth - 20f, 25f),
                         "拖拽移动界面，再按一次关闭界面", detailStyle);
            }

            // Kill panel 
            if (killedBots.Count > 0)
            {
                float killPanelWidth = 150f;
                float killPanelHeight = Mathf.Min(Screen.height * 0.5f, killedBots.Count * 25f + 40f);

                if (killPanelX == 0f && killPanelY == 0f)
                {
                    killPanelX = panelRect.x + panelWidth + 20f;
                    killPanelY = panelRect.y;
                }

                if (cursorState == CursorState.Unlocked)
                {
                    Rect killDragHandle = new Rect(killPanelX, killPanelY, killPanelWidth, 30f);
                    Vector2 mousePos = Event.current.mousePosition;

                    if (Event.current.type == EventType.MouseDown && killDragHandle.Contains(mousePos))
                    {
                        draggingKillPanel = true;
                        killPanelDragOffset = new Vector2(mousePos.x - killPanelX, mousePos.y - killPanelY);
                    }

                    if (draggingKillPanel)
                    {
                        if (Event.current.type == EventType.MouseDrag)
                        {
                            killPanelX = mousePos.x - killPanelDragOffset.x;
                            killPanelY = mousePos.y - killPanelDragOffset.y;
                        }

                        if (Event.current.type == EventType.MouseUp)
                        {
                            draggingKillPanel = false;
                        }
                    }
                }

                Rect killPanelRect = new Rect(killPanelX, killPanelY, killPanelWidth, killPanelHeight);
                GUI.Box(killPanelRect, "", guiStyle);

                GUI.Label(new Rect(killPanelRect.x + 10f, killPanelRect.y + 10f, killPanelWidth - 20f, 25f),
                          "KILLED 击杀", titleStyle);

                var groupedKills = killedBots.GroupBy(x => x)
                                            .Select(g => new { Type = g.Key, Count = g.Count() })
                                            .OrderByDescending(x => x.Count)
                                            .ThenBy(x => x.Type);

                float killY = killPanelRect.y + 40f;
                foreach (var kill in groupedKills)
                {
                    string killText = $"{kill.Type}: {kill.Count}";
                    GUI.Label(new Rect(killPanelRect.x + 10f, killY, killPanelWidth - 20f, 25f),
                              killText, detailStyle);
                    killY += 25f;
                }
            }
        }

        private void DrawLogEntryHeaderScroll(DamageLogEntry entry, float x, ref float y, float width)
        {
            float totalSeconds = entry.timeSinceRaidStart;
            int minutes = Mathf.FloorToInt(totalSeconds / 60);
            int seconds = Mathf.FloorToInt(totalSeconds % 60);

            string timeText = $"{minutes}m {seconds}s";
            string distanceText = $" {entry.distance:F0}m ";
            string botTypeText = entry.botType;
            string shortName = entry.targetName.Length > 6 ? entry.targetName.Substring(0, 6) : entry.targetName;
            string botNameText = $" {shortName}";

            float currentX = x; 

            GUI.Label(new Rect(currentX, y, width, 25f), timeText, entryStyle);
            currentX += GetTextWidth(timeText, entryStyle) + 5f;

            GUI.Label(new Rect(currentX, y, width, 25f), distanceText, entryStyle2);
            currentX += GetTextWidth(distanceText, entryStyle2) + 5f;

            GUI.Label(new Rect(currentX, y, width, 25f), botTypeText, entryStyle3);
            currentX += GetTextWidth(botTypeText, entryStyle3) + 5f;

            GUI.Label(new Rect(currentX, y, width, 25f), botNameText, entryStyle4);

            y += 25f;
        }

        private void DrawDamageLinesScroll(DamageLogEntry entry, float x, ref float y, float width)
        {
            if (!string.IsNullOrEmpty(entry.damageText))
            {
                GUI.Label(new Rect(x, y, width, 25f), entry.damageText, detailStyle);
                y += 25f;
            }

            if (!string.IsNullOrEmpty(entry.armorName) && entry.armorDamage > 0)
            {
                string armorText = $"{entry.armorName}: {entry.armorDamage}";
                if (entry.armorAbsorbedDamage > 0)
                {
                    armorText += $" ({entry.armorAbsorbedDamage} absorbed)";
                }
                GUI.Label(new Rect(x, y, width, 25f), armorText, detailStyle);
                y += 25f;
            }
        }


        private void DrawLogEntryHeader(DamageLogEntry entry, float x, ref float y, float width)
        {
            float totalSeconds = entry.timeSinceRaidStart;
            int minutes = Mathf.FloorToInt(totalSeconds / 60);
            int seconds = Mathf.FloorToInt(totalSeconds % 60);

            string timeText = $"{minutes}m {seconds}s";
            string distanceText = $" {entry.distance:F0}m ";
            string botTypeText = entry.botType;
            string shortName = entry.targetName.Length > 5 ? entry.targetName.Substring(0, 5) : entry.targetName;
            string botNameText = $" {shortName}";

            float currentX = x;

            GUI.Label(new Rect(currentX, y, width, 25f), timeText, entryStyle);
            currentX += GetTextWidth(timeText, entryStyle) + 5f;

            GUI.Label(new Rect(currentX, y, width, 25f), distanceText, entryStyle2);
            currentX += GetTextWidth(distanceText, entryStyle2) + 5f;

            GUI.Label(new Rect(currentX, y, width, 25f), botTypeText, entryStyle3);
            currentX += GetTextWidth(botTypeText, entryStyle3) + 5f;

            GUI.Label(new Rect(currentX, y, width, 25f), botNameText, entryStyle4);

            y += 25f;
        }

        private float GetTextWidth(string text, GUIStyle style)
        {
            return style.CalcSize(new GUIContent(text)).x;
        }

        private void DrawDamageLines(DamageLogEntry entry, float x, ref float y, float width)
        {
            if (!string.IsNullOrEmpty(entry.damageText))
            {
                GUI.Label(new Rect(x, y, width, 25f), entry.damageText, detailStyle);
                y += 25f;
            }

            if (!string.IsNullOrEmpty(entry.armorName) && entry.armorDamage > 0)
            {
                string armorText = $"{entry.armorName}: {entry.armorDamage}";
                if (entry.armorAbsorbedDamage > 0)
                {
                    armorText += $" ({entry.armorAbsorbedDamage} absorbed)";
                }
                GUI.Label(new Rect(x, y, width, 25f), armorText, detailStyle);
                y += 25f;
            }
        }

        private void InitializeGUIStyles()
        {
            Texture2D bgTexture = CreateTexture(2, 2, new Color(0f, 0f, 0f, 0.8f));
            guiStyle = new GUIStyle(GUI.skin.box);
            guiStyle.normal.background = bgTexture;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 16;
            titleStyle.normal.textColor = Color.white;

            detailStyle = new GUIStyle(GUI.skin.label);
            detailStyle.fontSize = 14;
            detailStyle.normal.textColor = HexColor("#cccccc");

            entryStyle = new GUIStyle(GUI.skin.label);
            entryStyle.fontSize = 16;
            entryStyle.normal.textColor = HexColor("#45b787");

            entryStyle2 = new GUIStyle(GUI.skin.label);
            entryStyle2.fontSize = 16;
            entryStyle2.normal.textColor = HexColor("#f8df70");

            entryStyle3 = new GUIStyle(GUI.skin.label);
            entryStyle3.fontSize = 16;
            entryStyle3.normal.textColor = HexColor("#fffefa");

            entryStyle4 = new GUIStyle(GUI.skin.label);
            entryStyle4.fontSize = 16;
            entryStyle4.normal.textColor = HexColor("#808080");

            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.fontSize = 14;
            bodyStyle.normal.textColor = HexColor("#f43e06"); 

            armorStyle = new GUIStyle(GUI.skin.label);
            armorStyle.fontSize = 14;
            armorStyle.normal.textColor = HexColor("#808080"); 

            absorbedStyle = new GUIStyle(GUI.skin.label);
            absorbedStyle.fontSize = 14;
            absorbedStyle.normal.textColor = HexColor("#0eb0c9"); 
        }

        private Color HexColor(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return color;
        }

        private Texture2D CreateTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }


        private static void StartNewRaid()
        {
            guiStyle = null;
            inRaid = true;
            raidStartTime = Time.time;
            raidStats = new RaidStats();
            guiLog.Clear();
            fileBuffer.Clear();
            killedBots.Clear();

            string logDir = Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "plugins", "JiangHu.Client", "fight_log");
            Directory.CreateDirectory(logDir);

            string prevFile = Path.Combine(logDir, "previous_raid.json");
            string currentFile = Path.Combine(logDir, "current_raid.json");

            if (File.Exists(currentFile))
            {
                File.Copy(currentFile, prevFile, true);
            }

            currentRaidFile = currentFile;

            var raidInfo = new
            {
                raid_start = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                map = GetCurrentMapName(),
                stats = raidStats,
                hits = new List<object>()
            };

            File.WriteAllText(currentRaidFile, JsonConvert.SerializeObject(raidInfo, Formatting.Indented));
        }

        private static void EndRaid()
        {
            inRaid = false;
            WriteBufferToFile(null);

            if (File.Exists(currentRaidFile))
            {
                try
                {
                    string json = File.ReadAllText(currentRaidFile);
                    var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (data != null)
                    {
                        data["raid_end"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        data["summary"] = new
                        {
                            total_hits = raidStats.totalHits,
                            body_damage = raidStats.bodyDamage,
                            armor_damage = raidStats.armorDamage,
                            armor_absorbed_damage = raidStats.armorAbsorbedDamage,
                            kills = raidStats.kills,
                            headshots = raidStats.headshots,
                            raid_duration = Time.time - raidStartTime
                        };

                        File.WriteAllText(currentRaidFile, JsonConvert.SerializeObject(data, Formatting.Indented));
                    }
                }
                catch { }
            }

            fileWriteTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            isTimerRunning = false;
        }

        private static void WriteBufferToFile(object state)
        {
            if (fileBuffer.Count == 0 || !File.Exists(currentRaidFile)) return;

            try
            {
                List<object> hitsToWrite = new List<object>();
                foreach (var entry in fileBuffer)
                {
                    hitsToWrite.Add(new
                    {
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        time_since_start = entry.timeSinceRaidStart,
                        target = entry.targetName,
                        bot_type = entry.botType,
                        body_part = entry.bodyPart,
                        damage_text = entry.damageText,
                        armor_name = entry.armorName,
                        armor_damage = entry.armorDamage,
                        armor_absorbed = entry.armorAbsorbedDamage,
                        distance = entry.distance,
                        kill = entry.wasKill
                    });
                }

                string json = File.ReadAllText(currentRaidFile);
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (data != null && data.ContainsKey("hits"))
                {
                    var existingHits = data["hits"] as List<object> ?? new List<object>();
                    existingHits.AddRange(hitsToWrite);
                    data["hits"] = existingHits;

                    File.WriteAllText(currentRaidFile, JsonConvert.SerializeObject(data, Formatting.Indented));
                    fileBuffer.Clear();
                }
            }
            catch { }
        }

        private static void AddHitToBuffer(DamageLogEntry entry)
        {
            fileBuffer.Add(entry);

            if (!isTimerRunning)
            {
                fileWriteTimer.Change(fileWriteInterval, System.Threading.Timeout.Infinite);
                isTimerRunning = true;
            }
        }

        private static string GetCurrentMapName()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
                return "unknown";

            return string.IsNullOrEmpty(gameWorld.LocationId) ? "unknown" : gameWorld.LocationId;
        }

        [HarmonyPatch(typeof(ArmorComponent), "ApplyDurabilityDamage")]
        private class ArmorComponent_ApplyDurabilityDamage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(ArmorComponent __instance, float armorDamage, List<ArmorComponent> armorComponents)
            {
                try
                {
                    if (armorDamage > 0)
                    {
                        ArmorHitTracker.RecordArmorHit(armorDamage, __instance.Item.ShortName.Localized());
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(Player), "ApplyDamageInfo")]
        private class Player_ApplyDamageInfo_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, DamageInfoStruct damageInfo,
                           EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
            {
                try
                {
                    string hitKey = $"{__instance.ProfileId}_{damageInfo.GetHashCode()}";
                    float currentTime = Time.time;

                    if (lastHitTimes.TryGetValue(hitKey, out float lastTime) &&
                        currentTime - lastTime < 0.1f)
                        return;

                    lastHitTimes[hitKey] = currentTime;

                    CleanupLastHitTimes();

                    var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                    if (mainPlayer == null || damageInfo.Player?.iPlayer?.ProfileId != mainPlayer.ProfileId)
                        return;

                    if (__instance.IsYourPlayer)
                    {
                        return;
                    }

                    if (!inRaid)
                    {
                        StartNewRaid();
                    }

                    var recentArmorHits = ArmorHitTracker.GetRecentHits(Time.time - 0.1f);
                    int armorDamage = 0;
                    string armorName = "";
                    int armorAbsorbedDamage = 0;

                    string cacheKey = null;
                    float closestTimeDiff = float.MaxValue;

                    foreach (var kvp in damageInfoCache)
                    {
                        if (kvp.Key.StartsWith(__instance.ProfileId + "_"))
                        {
                            float timeDiff = Mathf.Abs(Time.time - kvp.Value.Timestamp);
                            if (timeDiff < 0.1f && timeDiff < closestTimeDiff)
                            {
                                closestTimeDiff = timeDiff;
                                cacheKey = kvp.Key;
                            }
                        }
                    }

                    if (cacheKey != null && damageInfoCache.TryGetValue(cacheKey, out var damageCache))
                    {
                        float originalDamage = damageCache.OriginalDamage;
                        float finalDamage = damageInfo.Damage;
                        armorAbsorbedDamage = (int)Mathf.Max(0, originalDamage - finalDamage);
                        damageInfoCache.Remove(cacheKey);
                    }

                    if (recentArmorHits.Count > 0)
                    {
                        var lastArmorHit = recentArmorHits.Last();
                        armorDamage = (int)lastArmorHit.Damage;
                        armorName = lastArmorHit.ArmorName;
                        ArmorHitTracker.ClearRecentHits();
                    }

                    raidStats.armorAbsorbedDamage += armorAbsorbedDamage;

                    string targetName = __instance.Profile?.Nickname ?? "Unknown";
                    string botType = GetBotType(__instance);

                    int bodyDamage = (int)damageInfo.Damage;

                    string damageText = "";
                    if (bodyDamage > 0)
                    {
                        damageText = $"{bodyPartType}: {bodyDamage} dmg";
                    }

                    var entry = new DamageLogEntry
                    {
                        timestamp = Time.time,
                        timeSinceRaidStart = Time.time - raidStartTime,
                        targetName = targetName,
                        botType = botType,
                        bodyPart = bodyPartType.ToString(),
                        damageText = damageText,
                        armorName = armorName,
                        armorDamage = armorDamage,
                        armorAbsorbedDamage = armorAbsorbedDamage,
                        distance = Vector3.Distance(__instance.Position,
                                                   Singleton<GameWorld>.Instance.MainPlayer.Position),
                        wasKill = !__instance.HealthController.IsAlive
                    };

                    raidStats.totalHits++;
                    raidStats.bodyDamage += bodyDamage;
                    raidStats.armorDamage += armorDamage;

                    if (entry.wasKill)
                    {
                        raidStats.kills++;
                        killedBots.Add(entry.botType);
                    }
                    if (bodyPartType == EBodyPart.Head && bodyDamage > 0)
                        raidStats.headshots++;

                    guiLog.Insert(0, entry);
                    AddHitToBuffer(entry);
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(Player), "ApplyShot")]
        private class Player_ApplyShot_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance, DamageInfoStruct damageInfo,
                EBodyPart bodyPartType, EBodyPartColliderType colliderType,
                EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
            {
                try
                {
                    if (damageInfo.Player?.iPlayer == null) return;

                    string cacheKey = $"{__instance.ProfileId}_{Time.time:F3}";
                    damageInfoCache[cacheKey] = new DamageInfoCache
                    {
                        OriginalDamage = damageInfo.Damage,
                        Timestamp = Time.time,
                        TargetProfileId = __instance.ProfileId
                    };
                }
                catch { }
            }
        }

        private class DamageInfoCache
        {
            public float OriginalDamage;
            public float Timestamp;
            public string TargetProfileId;
        }

        private static void CleanupDamageCache()
        {
            float currentTime = Time.time;
            var toRemove = new List<string>();

            foreach (var kvp in damageInfoCache)
            {
                if (currentTime - kvp.Value.Timestamp > 1f)
                    toRemove.Add(kvp.Key);
            }

            foreach (var key in toRemove)
                damageInfoCache.Remove(key);
        }

        [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
        private class GameWorld_OnGameStarted_Patch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                StartNewRaid();
            }
        }

        [HarmonyPatch(typeof(GameWorld), "OnDestroy")]
        private class GameWorld_OnDestroy_Patch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                EndRaid();
            }
        }

        private static void CleanupLastHitTimes()
        {
            float currentTime = Time.time;
            var toRemove = new List<string>();

            foreach (var kvp in lastHitTimes)
            {
                if (currentTime - kvp.Value > 1f)
                    toRemove.Add(kvp.Key);
            }

            foreach (var key in toRemove)
                lastHitTimes.Remove(key);
        }

        private static class ArmorHitTracker
        {
            private static List<ArmorHitData> recentHits = new List<ArmorHitData>();

            public static void RecordArmorHit(float damage, string armorName)
            {
                recentHits.Add(new ArmorHitData
                {
                    Damage = damage,
                    ArmorName = armorName,
                    Timestamp = Time.time
                });

                if (recentHits.Count > 5) recentHits.RemoveAt(0);
            }

            public static List<ArmorHitData> GetRecentHits(float sinceTime)
            {
                return recentHits.Where(x => x.Timestamp >= sinceTime).ToList();
            }

            public static void ClearRecentHits()
            {
                recentHits.Clear();
            }

            public class ArmorHitData
            {
                public float Damage;
                public string ArmorName;
                public float Timestamp;
            }
        }

        private static string GetBotType(Player player)
        {
            var botOwner = player.AIData?.BotOwner;
            if (botOwner?.Profile?.Info?.Settings?.Role != null)
            {
                var role = botOwner.Profile.Info.Settings.Role;
                return UniversalBotSpawner.GetBotDisplayName(role);
            }
            return player.Side == EPlayerSide.Savage ? "SCAV" : "PMC";
        }

        private class DamageLogEntry
        {
            public float timestamp;
            public float timeSinceRaidStart;
            public string targetName;
            public string botType;
            public string bodyPart;
            public string damageText;
            public string armorName;
            public int armorDamage;
            public int armorAbsorbedDamage;
            public float distance;
            public bool wasKill;
        }

        private class RaidStats
        {
            public int totalHits = 0;
            public int bodyDamage = 0;
            public int armorDamage = 0;
            public int armorAbsorbedDamage = 0;
            public int kills = 0;
            public int headshots = 0;
        }

        void OnDestroy()
        {
            fileWriteTimer?.Dispose();
        }
    }
}