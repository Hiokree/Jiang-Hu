using Comfort.Common; 
using EFT;
using EFT; 
using EFT.Communications;
using EFT.UI;
using EFT.UI.Matchmaker;
using Newtonsoft.Json;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Random = System.Random;

namespace JiangHu.Patches
{
    internal class MainMenuModifierPatch : ModulePatch
    {
        private const float LeftXPosition = -10f;
        private const float LeftStartY = 300f;
        private const float LeftButtonSpacing = -80f;
        private const float LeftButtonScale = 0.8f;

        private const float RightXPosition = -10f;  
        private const float RightYPosition = 200f;
        private const float RightButtonScale = 0.9f;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod("Show", new[] {
                typeof(Profile),
                typeof(MatchmakerPlayerControllerClass),
                typeof(ESessionMode)
            });
        }

        [PatchPostfix]
        private static void Postfix(MenuScreen __instance)
        {
            __instance.StartCoroutine(FinalFixDelayed(__instance));

        }

        private static System.Collections.IEnumerator FinalFixDelayed(MenuScreen menuScreen)
        {
            yield return new WaitForSeconds(0.1f);

            try
            {
                ProcessEscapeButton(menuScreen);

                ProcessLeftButtons(menuScreen);

                ProcessExitButtonGroup(menuScreen);

                AddJiangHuButton(menuScreen);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m❌ [Jiang Hu] FINAL ERROR: {ex.Message}\x1b[0m");
            }
        }

        private static void ProcessEscapeButton(MenuScreen menuScreen)
        {
            try
            {
                var playButtonField = typeof(MenuScreen).GetField("_playButton",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var playButton = (DefaultUIButton)playButtonField.GetValue(menuScreen);

                if (playButton == null) return;

                FixEscapeButtonPosition(playButton);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m❌ [Jiang Hu] Escape process error: {ex.Message}\x1b[0m");
            }
        }

        private static void ProcessLeftButtons(MenuScreen menuScreen)
        {
            string[] leftButtonFields = { "_playerButton", "_tradeButton", "_hideoutButton" };

            for (int i = 0; i < leftButtonFields.Length; i++)
            {
                try
                {
                    var field = typeof(MenuScreen).GetField(leftButtonFields[i],
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var button = (DefaultUIButton)field.GetValue(menuScreen);

                    if (button == null) continue;

                    FixLeftButtonPosition(button, leftButtonFields[i], i);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\x1b[31m❌ [Jiang Hu] {leftButtonFields[i]} error: {ex.Message}\x1b[0m");
                }
            }
        }

        private static void ProcessExitButtonGroup(MenuScreen menuScreen)
        {
            try
            {
                Transform exitGroupTransform = menuScreen.transform.Find("ExitButtonGroup");
                if (exitGroupTransform == null)
                {
                    return;
                }

                GameObject exitGroup = exitGroupTransform.gameObject;

                var layouts = exitGroup.GetComponentsInChildren<UnityEngine.UI.LayoutGroup>(true);
                foreach (var layout in layouts)
                {
                    layout.enabled = false;
                }

                RectTransform rt = exitGroup.GetComponent<RectTransform>();
                if (rt == null) return;

                Transform exitButtonTransform = exitGroup.transform.Find("ExitButton");
                if (exitButtonTransform != null)
                {
                    RectTransform exitRt = exitButtonTransform.GetComponent<RectTransform>();
                    if (exitRt != null)
                    {
                        exitRt.anchorMin = new Vector2(0f, 0f);
                        exitRt.anchorMax = new Vector2(0f, 0f);
                        exitRt.pivot = new Vector2(0f, 0f);
                        exitRt.anchoredPosition = Vector2.zero;
                    }
                    exitButtonTransform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                }
                Vector2 originalSize = rt.sizeDelta;

                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);

                float yPos = LeftStartY + (3 * LeftButtonSpacing);
                rt.anchoredPosition = new Vector2(LeftXPosition, yPos);
                rt.sizeDelta = originalSize;
                Transform exitButton = exitGroup.transform.Find("ExitButton");
                if (exitButton != null)
                {
                    RectTransform exitRt = exitButton.GetComponent<RectTransform>();
                    if (exitRt != null)
                    {
                        exitRt.anchorMin = new Vector2(0f, 0f);
                        exitRt.anchorMax = new Vector2(0f, 0f);
                        exitRt.pivot = new Vector2(0f, 0f);
                        exitRt.anchoredPosition = Vector2.zero;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m❌ [Jiang Hu] ExitGroup error: {ex.Message}\x1b[0m");
            }
        }

        private static void FixEscapeButtonPosition(DefaultUIButton button)
        {
            try
            {
                RectTransform rt = button.GetComponent<RectTransform>();
                if (rt == null) return;

                Vector2 originalSize = rt.sizeDelta;

                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);

                rt.anchoredPosition = new Vector2(RightXPosition, RightYPosition - 100f);

                rt.sizeDelta = originalSize;
                rt.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m❌ [Jiang Hu] Escape fix error: {ex.Message}\x1b[0m");
            }
        }

        private static void FixLeftButtonPosition(DefaultUIButton button, string fieldName, int index)
        {
            try
            {
                RectTransform rt = button.GetComponent<RectTransform>();
                if (rt == null) return;

                Vector2 originalSize = rt.sizeDelta;

                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);

                float yPos = LeftStartY + (index * LeftButtonSpacing);

                rt.anchoredPosition = new Vector2(LeftXPosition, yPos);

                rt.sizeDelta = originalSize;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 224f);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 60f);
                rt.localScale = new Vector3(0.85f, 0.85f, 0.85f);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m❌ [Jiang Hu] {fieldName} fix error: {ex.Message}\x1b[0m");
            }
        }

        private static void AddJiangHuButton(MenuScreen menuScreen)
        {
            try
            {
                var escapeField = typeof(MenuScreen).GetField("_playButton", BindingFlags.NonPublic | BindingFlags.Instance);
                DefaultUIButton escapeButton = (DefaultUIButton)escapeField.GetValue(menuScreen);
                if (escapeButton == null) return;

                GameObject jiangHuObj = GameObject.Instantiate(escapeButton.gameObject, escapeButton.transform.parent);
                jiangHuObj.name = "JiangHuButton";

                RectTransform rt = jiangHuObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(RightXPosition, RightYPosition + 100f);
                rt.localScale = new Vector3(0.75f, 0.75f, 0.75f);

                DefaultUIButton jiangHuButton = jiangHuObj.GetComponent<DefaultUIButton>();

                var rawTextField = typeof(DefaultUIButton).GetField("_rawText",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                rawTextField.SetValue(jiangHuButton, true);

                var textField = typeof(DefaultUIButton).GetField("_text",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                textField.SetValue(jiangHuButton, "<color=#a4cab6>江 湖</color>");

                var fontSizeField = typeof(DefaultUIButton).GetField("_fontSize",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                fontSizeField.SetValue(jiangHuButton, 42);

                jiangHuButton.method_9();

                var allTexts = jiangHuObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in allTexts)
                {
                    text.richText = true;
                }

                jiangHuButton.OnClick.RemoveAllListeners();
                jiangHuButton.OnClick.AddListener(() =>
                {

                    JiangHu.ExfilRandomizer.RandomExfilDestinationPatch.SetButtonClickedForThisRaid();

                    NotificationManagerClass.DisplayMessageNotification(
                        "Random Raid 随机战局",
                        ENotificationDurationType.Long,
                        ENotificationIconType.Default,
                        new Color(1f, 0.8f, 0f) // Gold color
                    );

                    try
                    {
                        if (!TarkovApplication.Exist(out var app))
                        {
                            return;
                        }

                        List<string> enabledMaps = GetEnabledMapsFromConfig();

                        Console.WriteLine($"\x1b[32m✅ [Jiang Hu] Enabled maps: {string.Join(", ", enabledMaps)}\x1b[0m");

                        string randomMap = enabledMaps[UnityEngine.Random.Range(0, enabledMaps.Count)];
                        app.InternalStartGame(randomMap, true, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\x1b[31m❌ [Jiang Hu] Error: {ex.Message}\x1b[0m");
                    }
                });

                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m❌ [Jiang Hu] AddJiangHuButton error: {ex.Message}\x1b[0m");
            }
        }

        private static List<string> GetEnabledMapsFromConfig()
        {
            List<string> enabledMaps = new List<string>();

            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (configDict != null)
                    {
                        Dictionary<string, string> mapConfigKeys = new Dictionary<string, string>
                        {
                            { "Woods", "Enable_Woods" },
                            { "factory4_day", "Enable_factoryday" },
                            { "factory4_night", "Enable_factorynight" },
                            { "bigmap", "Enable_bigmap" },
                            { "Shoreline", "Enable_Shoreline" },
                            { "Interchange", "Enable_Interchange" },
                            { "RezervBase", "Enable_RezervBase" },
                            { "laboratory", "Enable_laboratory" },
                            { "Lighthouse", "Enable_Lighthouse" },
                            { "TarkovStreets", "Enable_TarkovStreets" },
                            { "Sandbox", "Enable_Sandbox" },
                            { "Sandbox_high", "Enable_Sandboxhigh" },
                            { "labyrinth", "Enable_labyrinth" }
                        };

                        foreach (var kvp in mapConfigKeys)
                        {
                            string mapName = kvp.Key;
                            string configKey = kvp.Value;

                            if (configDict.ContainsKey(configKey) && configDict[configKey] is bool && (bool)configDict[configKey])
                            {
                                enabledMaps.Add(mapName);
                            }
                        }

                        if (enabledMaps.Count == 0)
                        {
                            enabledMaps.AddRange(mapConfigKeys.Keys);
                        }
                    }
                }
                else
                {
                    enabledMaps.AddRange(new[] {
                        "Woods", "factory4_day", "factory4_night", "bigmap", "Shoreline",
                        "Interchange", "RezervBase", "laboratory", "Lighthouse", "TarkovStreets",
                        "Sandbox", "Sandbox_high", "labyrinth"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m❌ [Jiang Hu] Config read error: {ex.Message}\x1b[0m");
                enabledMaps.AddRange(new[] {
                    "Woods", "factory4_day", "factory4_night", "bigmap", "Shoreline",
                    "Interchange", "RezervBase", "laboratory", "Lighthouse", "TarkovStreets",
                    "Sandbox", "Sandbox_high", "labyrinth"
                });
            }

            return enabledMaps;
        }
    }

    internal class HideProgressCounterUIPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MatchmakerTimeHasCome)
                .GetMethod("Show", new[] { typeof(MatchmakerTimeHasCome.TimeHasComeScreenClass) });
        }

        [PatchPostfix]
        private static void Postfix(MatchmakerTimeHasCome __instance)
        {
            __instance.StartCoroutine(HideProgressTextDelayed(__instance));
        }

        private static IEnumerator HideProgressTextDelayed(MatchmakerTimeHasCome instance)
        {
            yield return null;

            try
            {
                var deployingTextField = typeof(MatchmakerTimeHasCome).GetField("_deployingText",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (deployingTextField != null)
                {
                    var textComponent = (TextMeshProUGUI)deployingTextField.GetValue(instance);
                    if (textComponent != null)
                    {
                        textComponent.color = new Color(0, 0, 0, 0); 
                        textComponent.GetComponent<RectTransform>().anchoredPosition = new Vector2(9999f, 9999f);
                    }
                }
            }
            catch { }
        }
    }

    internal class RaidEndDetectionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalGame).GetMethod("Stop",
                BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void Postfix(string profileId, ExitStatus exitStatus, string exitName, float delay)
        {
            Console.WriteLine($"\x1b[35m🏁 [Jiang Hu] LocalGame.Stop() called - Status: {exitStatus}\x1b[0m");

            if (exitStatus != ExitStatus.Transit)
            {
                Console.WriteLine($"\x1b[35m🏁 [Jiang Hu] RAID ENDED (not transit) - Resetting flag\x1b[0m");
                JiangHu.ExfilRandomizer.RandomExfilDestinationPatch.ResetAfterRaid();
            }
            else
            {
                Console.WriteLine($"\x1b[35m🏁 [Jiang Hu] Skipping reset - Player is transiting\x1b[0m");
            }
        }
    }
}