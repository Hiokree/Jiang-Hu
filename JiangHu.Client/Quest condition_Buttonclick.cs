using EFT.Quests;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using UnityEngine;

namespace JiangHu
{
    public class ConditionRaidStatus : Condition
    {
        public string[] status;
        public override string FormattedDescription => $"Raid Status Check";
    }

    public class RaidStatusConditionManager : MonoBehaviour
    {
        private Harmony harmony;
        private static bool EnableArenaQuest;

        private void LoadConfig()
        {
            string configPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");
            if (!File.Exists(configPath)) return;

            var config = JObject.Parse(File.ReadAllText(configPath));
            EnableArenaQuest = config["Enable_Arena_Quest"]?.Value<bool>() ?? true;
        }

        void Start()
        {
            LoadConfig();
            if (!EnableArenaQuest) return;

            harmony = new Harmony("jianghu.raidstatus");
            PatchConditionRegistry();
        }

        public void PatchConditionRegistry()
        {
            try
            {
                var converter = new GClass1871();
                var conditionType = typeof(ConditionRaidStatus);

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
                if (serializedType == "RaidStatus")
                {
                    __result = typeof(ConditionRaidStatus);
                    return false;
                }
                return true;
            }
        }

        void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}