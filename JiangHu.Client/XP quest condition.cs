using Comfort.Common;
using EFT;
using EFT.Counters;
using EFT.Quests;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


namespace JiangHu
{
    public class ConditionGainExperience : ConditionStatistic
    {
        public ConditionGainExperience()
        {
            this.byTag = true;
            this.target = "Exp";
        }

        public override string FormattedDescription => base.FormattedDescription;
    }

    public class XPConditionManager : MonoBehaviour
    {
        private Harmony harmony;
        private static bool EnableNewRaidMode;

        private void LoadConfig()
        {
            string configPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");
            if (!File.Exists(configPath)) return;

            var config = JObject.Parse(File.ReadAllText(configPath));
            EnableNewRaidMode = config["Enable_New_RaidMode"]?.Value<bool>() ?? true;
        }

        void Start()
        {
            LoadConfig();

            if (!EnableNewRaidMode) return;

            harmony = new Harmony("jianghu.xp");
            PatchConditionRegistry();
            PatchExperienceSystem();
            PatchCounterCreatorLocalization();
            PatchCounterInitialization();
        }

        public void PatchConditionRegistry()
        {
            try
            {
                var converter = new GClass1871();
                var conditionType = typeof(ConditionGainExperience);

                if (!converter.List_0.Contains(conditionType))
                {
                    converter.List_0.Add(conditionType);
                }

                var keyToTypeMethod = AccessTools.Method(typeof(GClass1871), "KeyToType");
                harmony.Patch(keyToTypeMethod,
                    prefix: new HarmonyMethod(typeof(ConditionRegistryPatch), "KeyToTypePrefix"));
            }
            catch { }
        }

        public static class ConditionRegistryPatch
        {
            public static bool KeyToTypePrefix(string serializedType, ref Type __result)
            {
                if (serializedType == "GainExperience")
                {
                    __result = typeof(ConditionGainExperience);
                    return false;
                }
                return true;
            }
        }

        public void PatchExperienceSystem()
        {
            var xpMethods = new[]
            {
                AccessTools.Method(typeof(LocationStatisticsCollectorAbstractClass), "method_13"),
                AccessTools.Method(typeof(LocationStatisticsCollectorAbstractClass), "OnEnemyKill"),
                AccessTools.Method(typeof(LocationStatisticsCollectorAbstractClass), "OnEnemyDamage"),
                AccessTools.Method(typeof(LocationStatisticsCollectorAbstractClass), "AddDoorExperience"),
                AccessTools.Method(typeof(LocationStatisticsCollectorAbstractClass), "method_16"),
                AccessTools.Method(typeof(LocationStatisticsCollectorAbstractClass), "OnInteractWithLootContainer")
            };

            foreach (var method in xpMethods)
            {
                if (method != null)
                {
                    harmony.Patch(method,
                        postfix: new HarmonyMethod(typeof(XPExperiencePatch), "XPAdditionPostfix"));
                }
            }

            var beginStatsMethod = AccessTools.Method(typeof(LocationStatisticsCollectorAbstractClass), "BeginStatisticsSession");
            if (beginStatsMethod != null)
            {
                harmony.Patch(beginStatsMethod,
                    postfix: new HarmonyMethod(typeof(XPExperiencePatch), "BeginStatisticsSessionPostfix"));
            }
        }

        public static class XPExperiencePatch
        {
            private static int _lastTotalXP;

            public static void BeginStatisticsSessionPostfix(LocationStatisticsCollectorAbstractClass __instance)
            {
                ResetTracking();
            }

            public static void XPAdditionPostfix(LocationStatisticsCollectorAbstractClass __instance)
            {
                try
                {
                    var sessionCounters = __instance.Profile_0?.EftStats?.SessionCounters;
                    if (sessionCounters != null)
                    {
                        int currentTotalXP = sessionCounters.GetAllInt(new object[] { "Exp" });
                        int xpGained = currentTotalXP - _lastTotalXP;


                        if (xpGained > 0)
                        {
                            UpdateQuestCounters(__instance.Profile_0, xpGained);
                            _lastTotalXP = currentTotalXP;
                        }
                    }
                }
                catch { }
            }

            private static void UpdateQuestCounters(Profile profile, int xpGained)
            {
                try
                {
                    var gameWorld = Singleton<GameWorld>.Instance;
                    var player = gameWorld?.MainPlayer;
                    if (player == null) return;

                    var questControllerField = player.GetType().GetField("_questController",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (questControllerField == null) return;

                    var questController = questControllerField.GetValue(player);
                    if (questController == null) return;

                    var questBookField = questController.GetType().GetField("QuestBookClass",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (questBookField == null) return;

                    var questBook = questBookField.GetValue(questController) as QuestBookClass;
                    if (questBook == null) return;

                    foreach (var quest in questBook)
                    {
                        foreach (var conditionGroup in quest.Conditions)
                        {
                            foreach (var condition in conditionGroup.Value)
                            {
                                if (condition is ConditionCounterCreator counterCreator)
                                {
                                    foreach (var subCondition in counterCreator.Conditions)
                                    {
                                        if (subCondition is ConditionStatistic statistic && statistic.target == "Exp")
                                        {
                                            var questCounter = quest.ConditionCountersManager?.GetCounter(counterCreator.id);
                                            if (questCounter == null) continue;

                                            questCounter.Value += xpGained;

                                            var progressChecker = quest.ProgressCheckers?[counterCreator];
                                            if (progressChecker == null) continue;

                                            progressChecker.CallConditionChanged();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            public static void ResetTracking()
            {
                _lastTotalXP = 0;
            }
        }

        public void PatchCounterCreatorLocalization()
        {
            var gainExperienceType = typeof(ConditionGainExperience);
            if (!ConditionCounterCreator.LocalizationTypes.ContainsKey(gainExperienceType))
            {
                ConditionCounterCreator.LocalizationTypes.Add(gainExperienceType, "{experience}");
            }
        }

        public void PatchCounterInitialization()
        {
            var method0 = AccessTools.Method(typeof(ConditionCounterManager), "method_0");
            if (method0 != null)
            {
                harmony.Patch(method0,
                    postfix: new HarmonyMethod(typeof(CounterInitPatch), "Method0Postfix"));
            }
        }

        public static class CounterInitPatch
        {
            public static void Method0Postfix(ConditionCounterManager __instance, IEnumerable<ConditionCounterCreator> templates)
            {
                var quest = __instance.Conditional as QuestClass;
                if (quest?.QuestStatus == EQuestStatus.Started)
                {
                    foreach (var counter in __instance.Counters)
                    {
                        if (counter.Template is ConditionCounterCreator counterCreator)
                        {
                            foreach (var subCondition in counterCreator.Conditions)
                            {
                                if (subCondition is ConditionStatistic statistic && statistic.target == "Exp")
                                {
                                    counter.Value = 0; 
                                }
                            }
                        }
                    }
                }
            }
        }


        void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}