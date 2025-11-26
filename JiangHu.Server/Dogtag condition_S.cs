using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Quests;

namespace JiangHu.Server
{
    public class QuestControllerHandoverPatch : AbstractPatch
    {
        private static readonly bool _enabled;

        static QuestControllerHandoverPatch()
        {
            _enabled = LoadConfig();
        }

        public QuestControllerHandoverPatch() : base("jianghu.quest.handover") { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(QuestController).GetMethod("HandoverQuest");
        }

        [PatchPrefix]
        public static bool Prefix(
            QuestController __instance,
            PmcData pmcData,
            HandoverQuestRequestData request,
            MongoId sessionID,
            ref ItemEventRouterResponse __result)
        {
            if (!_enabled) return true;

            try
            {
                var questHelperField = __instance.GetType().GetField("<questHelper>P", BindingFlags.NonPublic | BindingFlags.Instance);
                var questHelper = questHelperField?.GetValue(__instance);
                if (questHelper == null) return true;

                var getQuestFromDbMethod = questHelper.GetType().GetMethod("GetQuestFromDb");
                var quest = getQuestFromDbMethod?.Invoke(questHelper, new object[] { request.QuestId, pmcData });
                if (quest == null) return true;

                var availableForFinish = quest.GetType().GetProperty("Conditions")?.GetValue(quest)
                    ?.GetType().GetProperty("AvailableForFinish")?.GetValue(quest.GetType().GetProperty("Conditions")?.GetValue(quest)) as IEnumerable<object>;

                if (availableForFinish == null) return true;

                foreach (var condition in availableForFinish)
                {
                    if (condition.GetType().GetProperty("Id")?.GetValue(condition)?.ToString() == request.ConditionId.ToString() &&
                        condition.GetType().GetProperty("ConditionType")?.GetValue(condition) as string == "HandoverDogtagWithNickname")
                    {
                        condition.GetType().GetProperty("ConditionType")?.SetValue(condition, "HandoverItem");
                        break;
                    }
                }
            }
            catch
            {
                return true;
            }

            return true;
        }

        private static bool LoadConfig()
        {
            try
            {
                var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configPath = System.IO.Path.Combine(modPath, "config", "config.json");
                if (System.IO.File.Exists(configPath))
                {
                    var jsonContent = System.IO.File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);
                    return config != null && config.TryGetValue("Enable_Dogtag_Collection", out var enableValue) && enableValue.GetBoolean();
                }
            }
            catch { }
            return false;
        }
    }
}
