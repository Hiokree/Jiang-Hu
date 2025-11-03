using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils.Json;

namespace JiangHu.Server
{
    [Injectable] 
    public class QuestGenerator
    {
        private readonly DatabaseServer _databaseServer;
        private readonly SaveServer _saveServer;
        private bool _enableQuestGenerator = false;

        public QuestGenerator(DatabaseServer databaseServer, SaveServer saveServer)
        {
            _databaseServer = databaseServer;
            _saveServer = saveServer;
            LoadConfig();
            if (_enableQuestGenerator) 
            {
                _saveServer.AddBeforeSaveCallback("jianghu_reset_dummy", (profile) =>
                {
                    ResetDummyQuestForProfile(profile);
                    return profile;
                });
            }
        }

        private void LoadConfig()
        {
            try
            {
                var modPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var configPath = System.IO.Path.Combine(modPath, "config", "config.json");

                if (!System.IO.File.Exists(configPath))
                {
                    Console.WriteLine("⚠️ [Jiang Hu] config.json not found!");
                    return;
                }

                var json = System.IO.File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (config != null && config.TryGetValue("Enable_Quest_Generator", out var questGenValue))
                {
                    _enableQuestGenerator = questGenValue.GetBoolean();                
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Jiang Hu] Error loading quest generator config: {ex.Message}");
            }
        }

        public void GenerateQuestChain()
        {
            if (!_enableQuestGenerator) return;
            try
            {
                const string dummyQuestId = "e993002c4ab4d99999999999";

                var questPool = BuildQuestPool();
                if (questPool.Count == 0)
                {
                    Console.WriteLine("⚠️ [Jiang Hu] No quests available in pool");
                    return;
                }

                var random = new Random();
                var selectedQuests = questPool.OrderBy(x => random.Next()).Take(20).ToList();
                FisherYatesShuffle(selectedQuests, random);

                CreateQuestChain(selectedQuests, dummyQuestId);

                ResetDummyQuest();
                Console.WriteLine($"\x1b[36m🪄 [Jiang Hu] Quest Generator is On \x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Jiang Hu] Quest Generator failed: {ex.Message}");
            }
        }

        private List<string> BuildQuestPool()
        {
            var questPool = new List<string>();
            foreach (var kvp in _databaseServer.GetTables().Templates.Quests)
            {
                var levelCond = kvp.Value.Conditions.AvailableForStart?
                    .Find(c => c.ConditionType == "Level");
                if (levelCond?.Value == 99)
                    questPool.Add(kvp.Key);
            }

            if (_databaseServer.GetTables().Templates.Quests.TryGetValue("e993002c4ab4d99999999999", out var dummyQuest))
            {
                var levelCond = dummyQuest.Conditions.AvailableForStart?
                    .Find(c => c.ConditionType == "Level");
                if (levelCond != null)
                {
                    levelCond.Value = 1;
                }
            }
            return questPool;
        }

        private static void FisherYatesShuffle<T>(List<T> list, Random random)
        {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private void CreateQuestChain(List<string> questChain, string dummyQuestId)
        {
            for (int i = 0; i < questChain.Count; i++)
            {
                string currentQuestId = questChain[i];

                if (_databaseServer.GetTables().Templates.Quests.TryGetValue(currentQuestId, out var quest))
                {
                    quest.Conditions.AvailableForStart = new List<QuestCondition>();

                    quest.Conditions.AvailableForStart.Add(new QuestCondition
                    {
                        Id = new MongoId(),
                        ConditionType = "Level",
                        CompareMethod = ">=",
                        Value = 1,
                        DynamicLocale = true
                    });

                    string targetQuestId = i == 0 ? dummyQuestId : questChain[i - 1];

                    quest.Conditions.AvailableForStart.Add(new QuestCondition
                    {
                        Id = new MongoId(),
                        ConditionType = "Quest",
                        CompareMethod = ">=",
                        Target = new ListOrT<string>(list: null, item: targetQuestId),
                        Status = new HashSet<QuestStatusEnum> { QuestStatusEnum.Success, QuestStatusEnum.Fail },
                        DynamicLocale = true
                    });
                }
            }
        }
        private void ResetDummyQuest()
        {
            if (!_enableQuestGenerator) return;
            var profiles = _saveServer.GetProfiles();
            foreach (var profile in profiles.Values)
            {
                ResetDummyQuestForProfile(profile);
            }
        }
        private void ResetDummyQuestForProfile(SptProfile profile)
        {
            if (!_enableQuestGenerator) return;
            var dummyQuest = profile.CharacterData?.PmcData?.Quests?
                .FirstOrDefault(q => q.QId == "e993002c4ab4d99999999999");

            if (dummyQuest != null)
            {
                dummyQuest.Status = QuestStatusEnum.Locked;             
            }
        }
    }
}
