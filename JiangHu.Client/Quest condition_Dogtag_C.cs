using Comfort.Common;
using EFT;
using EFT.Counters;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace JiangHu
{
    public class ConditionHandoverDogtagWithNickname : ConditionHandoverItem
    {
        public string dogtagnickname;
        public override string FormattedDescription => $"{dogtagnickname}";
    }

    public class DogtagConditionManager : MonoBehaviour
    {
        private Harmony harmony;
        private static bool EnableDogtagCollection;

        private void LoadConfig()
        {
            string configPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");
            if (!File.Exists(configPath)) return;

            var config = JObject.Parse(File.ReadAllText(configPath));
            EnableDogtagCollection = config["Enable_Dogtag_Collection"]?.Value<bool>() ?? true;
        }

        void Start()
        {
            LoadConfig();

            if (!EnableDogtagCollection) return;

            harmony = new Harmony("jianghu.dogtag");

            PatchConditionRegistry();

            PatchQuestObjectiveView();

            PatchQuestObjectiveShow();
        }

        public void PatchConditionRegistry()
        {
            try
            {
                var converter = new GClass1871();
                var conditionType = typeof(ConditionHandoverDogtagWithNickname);

                if (!converter.List_0.Contains(conditionType))
                {
                    converter.List_0.Add(conditionType);
                }

                var keyToTypeMethod = AccessTools.Method(typeof(GClass1871), "KeyToType");
                harmony.Patch(keyToTypeMethod, prefix: new HarmonyMethod(typeof(ConditionRegistryPatch), "KeyToTypePrefix"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"\x1b[31m🎮 [Jiang Hu DEBUG] Registry error: {e}\x1b[0m");
            }
        }

        public static class ConditionRegistryPatch
        {
            public static bool KeyToTypePrefix(string serializedType, ref Type __result)
            {
                if (serializedType == "HandoverDogtagWithNickname")
                {
                    __result = typeof(ConditionHandoverDogtagWithNickname);
                    return false;
                }
                return true;
            }
        }

        public void PatchQuestObjectiveView()
        {
            try
            {
                var onConditionChangedMethod = AccessTools.Method(typeof(QuestObjectiveView), "OnConditionChanged");
                if (onConditionChangedMethod != null)
                {
                    harmony.Patch(onConditionChangedMethod,
                        prefix: new HarmonyMethod(typeof(QuestObjectiveViewPatch), "OnConditionChangedPrefix"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"\x1b[31m🎮 [Jiang Hu] OnConditionChanged patch error: {e}\x1b[0m");
            }
        }

        public void PatchQuestObjectiveShow()
        {
            try
            {
                var showMethod = AccessTools.Method(typeof(QuestObjectiveView), "Show");
                if (showMethod != null)
                {
                    harmony.Patch(showMethod,
                        postfix: new HarmonyMethod(typeof(QuestObjectiveViewPatch), "ShowPostfix"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"\x1b[31m🎮 [Jiang Hu] Show patch error: {e}\x1b[0m");
            }
        }

        public static class QuestObjectiveViewPatch
        {
            public static void ShowPostfix(QuestObjectiveView __instance, QuestClass quest, Condition condition)
            {
                if (condition is ConditionHandoverDogtagWithNickname dogtagCondition)
                {
                    CustomHandoverSystem.AddCustomHandoverButton(__instance, dogtagCondition);
                }
            }

            public static bool OnConditionChangedPrefix(QuestObjectiveView __instance, ConditionProgressChecker conditionProgressChecker)
            {
                if (!(conditionProgressChecker.Condition is ConditionHandoverDogtagWithNickname))
                    return true;

                if (conditionProgressChecker.Condition is ConditionHandoverDogtagWithNickname dogtagCondition)
                {
                    CustomHandoverSystem.UpdateButtonVisibility(__instance, dogtagCondition);
                }
                return true;
            }
        }

        public static class CustomHandoverSystem
        {
            private static Dictionary<string, DefaultUIButton> _customButtons = new Dictionary<string, DefaultUIButton>();

            public static void AddCustomHandoverButton(QuestObjectiveView objectiveView, ConditionHandoverDogtagWithNickname condition)
            {
                try
                {
                    string buttonKey = $"{GetQuestClass(objectiveView)?.Id}_{condition.id}";

                    if (_customButtons.ContainsKey(buttonKey))
                        return;

                    var originalButtonField = typeof(QuestObjectiveView).GetField("_handoverButton",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var originalButton = originalButtonField?.GetValue(objectiveView) as DefaultUIButton;

                    if (originalButton == null) return;

                    var customButton = UnityEngine.Object.Instantiate(originalButton, originalButton.transform.parent);
                    customButton.gameObject.name = "CustomDogtagHandoverButton";
                    customButton.OnClick.RemoveAllListeners();
                    customButton.OnClick.AddListener(() => OnCustomHandoverClick(objectiveView, condition));

                    var rectTransform = customButton.GetComponent<RectTransform>();
                    var originalRect = originalButton.GetComponent<RectTransform>();
                    rectTransform.anchoredPosition = new Vector2(originalRect.anchoredPosition.x + 120f, originalRect.anchoredPosition.y);

                    var textComponent = customButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.text = "Handover Dogtag";
                    }
                    customButton.gameObject.SetActive(true);

                    _customButtons[buttonKey] = customButton;
                    UpdateCustomButtonVisibility(objectiveView, condition, customButton);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\x1b[31m🎮 [Jiang Hu] Error creating custom button: {e}\x1b[0m");
                }
            }

            private static void UpdateCustomButtonVisibility(QuestObjectiveView objectiveView, ConditionHandoverDogtagWithNickname condition, DefaultUIButton button)
            {
                try
                {
                    if (button == null || button.gameObject == null)
                    {
                        string buttonKey = $"{GetQuestClass(objectiveView)?.Id}_{condition.id}";
                        if (_customButtons.ContainsKey(buttonKey))
                        {
                            _customButtons.Remove(buttonKey);
                        }
                        return;
                    }

                    var profileField = typeof(ObjectiveView<QuestObjectiveView>).GetField("Profile",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var profile = profileField?.GetValue(objectiveView) as Profile;

                    if (profile == null) return;

                    int dogtagCount = 0;
                    foreach (var item in profile.Inventory.GetPlayerItems(EPlayerItems.All))
                    {
                        var dogtag = item.GetItemComponent<DogtagComponent>();
                        if (dogtag != null && dogtag.Nickname == condition.dogtagnickname)
                        {
                            dogtagCount += item.StackObjectsCount;
                        }
                    }

                    bool shouldShow = GetQuestClass(objectiveView)?.QuestStatus == EQuestStatus.Started &&
                                      !GetQuestClass(objectiveView)?.IsConditionDone(condition) == true &&
                                      dogtagCount > 0;

                    if (button != null && button.gameObject != null)
                    {
                        button.gameObject.SetActive(shouldShow);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\x1b[31m🎮 [Jiang Hu] Error updating button visibility: {e}\x1b[0m");
                }
            }

            private static void OnCustomHandoverClick(QuestObjectiveView objectiveView, ConditionHandoverDogtagWithNickname condition)
            {
                Singleton<GUISounds>.Instance?.PlayUISound(EUISoundType.ButtonBottomBarClick);

                var profileField = typeof(ObjectiveView<QuestObjectiveView>).GetField("Profile",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var profile = profileField?.GetValue(objectiveView) as Profile;

                if (profile == null) return;

                var matchingDogtags = new List<Item>();
                foreach (var item in profile.Inventory.GetPlayerItems(EPlayerItems.All))
                {
                    var dogtag = item.GetItemComponent<DogtagComponent>();
                    if (dogtag != null && dogtag.Nickname == condition.dogtagnickname)
                    {
                        matchingDogtags.Add(item);
                        if (matchingDogtags.Count >= condition.value) break;
                    }
                }

                if (matchingDogtags.Count > 0)
                {
                    var questControllerField = typeof(QuestObjectiveView).GetField("abstractQuestControllerClass",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var questController = questControllerField?.GetValue(objectiveView) as AbstractQuestControllerClass;

                    var quest = GetQuestClass(objectiveView);

                    if (questController != null)
                    {
                        var actualCondition = quest.GetCondition(condition.id);
                        questController.HandoverItem(quest, actualCondition as ConditionItem, matchingDogtags.ToArray(), true);
                    }
                }
            }

            public static void UpdateButtonVisibility(QuestObjectiveView objectiveView, ConditionHandoverDogtagWithNickname condition)
            {
                string buttonKey = $"{GetQuestClass(objectiveView)?.Id}_{condition.id}";
                if (_customButtons.TryGetValue(buttonKey, out var button))
                {
                    if (button != null && button.gameObject != null)
                    {
                        UpdateCustomButtonVisibility(objectiveView, condition, button);
                    }
                    else
                    {
                        _customButtons.Remove(buttonKey);
                    }
                }
            }
            private static QuestClass GetQuestClass(QuestObjectiveView objectiveView)
            {
                var questField = typeof(QuestObjectiveView).GetField("questClass",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return questField?.GetValue(objectiveView) as QuestClass;
            }

        }

        void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}