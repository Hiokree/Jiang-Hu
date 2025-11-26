using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Servers;

namespace JiangHu.Server
{
    [Injectable]
    public class newbotXP
    {
        private readonly DatabaseServer _databaseServer;
        private readonly SaveServer _saveServer;
        private bool _Enable_New_RaidMode = false;
        private bool _Restart_New_RaidMode = false;
        private readonly string[] _targetQuestIds = {
            "e973002c4ab4d99999999000",
            "e973002c4ab4d99999999010",
            "e973002c4ab4d99999999020",
            "e973002c4ab4d99999999030",
            "e973002c4ab4d99999999040",
            "e973002c4ab4d99999999050"
        };


        public newbotXP(DatabaseServer databaseServer, SaveServer saveServer)
        {
            _databaseServer = databaseServer;
            _saveServer = saveServer;
            LoadConfig();
        }

        public void ApplyAllRaidModeSettings()
        {
            if (!_Enable_New_RaidMode)
            {
                ResetRaidModeQuestsInProfiles();
                return;
            }
            Console.WriteLine("\x1b[91m🍂 [Jiang Hu] Dance on the Razor's Edge enabled    惊鸿猎\x1b[0m");
            newBotXpValues();
            ApplyEnergyHydrationSettings();
            ModifyQuestCondition();
            LockRaidModeQuestsInProfiles();
        }

        private void LoadConfig()
        {
            try
            {
                var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configPath = System.IO.Path.Combine(modPath, "config", "config.json");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine("⚠️ [Bot XP] config.json not found!");
                    return;
                }

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (config == null)
                    return;

                if (config.TryGetValue("Enable_New_RaidMode", out var raidModeValue))
                    _Enable_New_RaidMode = raidModeValue.GetBoolean();

                if (config.TryGetValue("Restart_New_RaidMode", out var restartValue))
                    _Restart_New_RaidMode = restartValue.GetBoolean();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Bot XP] Error loading config: {ex.Message} \x1b[0m");
            }
        }

        public void newBotXpValues()
        {
            try
            {
                if (!_Enable_New_RaidMode)
                {
                    return;
                }

                var tables = _databaseServer.GetTables();
                var bots = tables.Bots;
                var botTypes = bots.Types;

                int modifiedCount = 0;
                foreach (var botType in botTypes)
                {
                    var botName = botType.Key;
                    var botData = botType.Value;

                    if (botData?.BotExperience?.Reward != null && botData.BotExperience.Reward.Any())
                    {
                        bool botModified = false;
                        foreach (var difficulty in botData.BotExperience.Reward)
                        {
                            var maxXp = difficulty.Value.Max;
                            if (maxXp >= 1000)
                            {
                                difficulty.Value.Min = 1000;
                                difficulty.Value.Max = 1000;
                                botModified = true;
                            }
                        }
                        if (botModified) modifiedCount++;
                    }
                }
                Console.WriteLine("\x1b[91m🍂 [Jiang Hu] Boss kill XP adjusted    调整击杀首领经验\x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[91m❌ [Bot XP Mod] Error processing bot XP values: {ex.Message}\x1b[0m");
            }
        }

        public void ApplyEnergyHydrationSettings()
        {
            try
            {
                if (!_Enable_New_RaidMode)
                {
                    return;
                }

                var tables = _databaseServer.GetTables();
                var existence = tables?.Globals?.Configuration?.Health?.Effects?.Existence;

                if (existence != null)
                {
                    existence.EnergyDamage = 1;
                    existence.EnergyLoopTime = 10;
                    existence.HydrationDamage = 1;
                    existence.HydrationLoopTime = 10;

                    Console.WriteLine($"\x1b[91m🍂 [Jiang Hu] Energy & Hydration consumption rates increased    能量水分消耗加快 \x1b[0m");
                }
                else
                {
                    Console.WriteLine($"\x1b[91m❌ [Jiang Hu] Could not find Existence settings in globals\x1b[0m");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[91m❌ [Energy/Hydration] Error modifying settings: {ex.Message}\x1b[0m");
            }
        }

        private void ModifyQuestCondition()
        {
            try
            {
                if (!_Enable_New_RaidMode)
                {
                    return;
                }

                var tables = _databaseServer.GetTables();
                var quests = tables.Templates.Quests;
                string targetQuestId = "e973002c4ab4d99999999000";

                if (quests.TryGetValue(targetQuestId, out var quest))
                {
                    quest.Conditions.AvailableForStart = new List<QuestCondition>();

                    var condition = new QuestCondition
                    {
                        Id = new MongoId(Guid.NewGuid().ToString("N").Substring(0, 24)),
                        Index = 0,
                        ConditionType = "Level",
                        CompareMethod = ">=",
                        DynamicLocale = false,
                        GlobalQuestCounterId = string.Empty,
                        ParentId = string.Empty,
                        Value = 1,
                        VisibilityConditions = new List<VisibilityCondition>()
                    };

                    quest.Conditions.AvailableForStart = new List<QuestCondition> { condition };
                    Console.WriteLine($"\x1b[91m🍂 [Jiang Hu] Arena Quest opened    竞技场任务开启\x1b[0m");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[91m❌ [Quest Mod] Error modifying quest condition: {ex.Message}\x1b[0m");
            }
        }

        private void ResetRaidModeQuestsInProfiles()
        {
            try
            {
                var profiles = _saveServer.GetProfiles();
                int resetCount = 0;

                foreach (var profile in profiles.Values)
                {
                    var pmcQuests = profile.CharacterData?.PmcData?.Quests;
                    if (pmcQuests != null)
                    {
                        foreach (var questId in _targetQuestIds)
                        {
                            var quest = pmcQuests.FirstOrDefault(q => q.QId == questId);
                            if (quest != null)
                            {
                                quest.Status = QuestStatusEnum.Locked;
                                resetCount++;
                            }
                        }
                    }
                }

                Console.WriteLine($"\x1b[93m⚔️ [Jiang Hu] Reset {resetCount} raid mode quests to locked\x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[91m❌ [Quest Reset] Error resetting quests: {ex.Message}\x1b[0m");
            }
        }

        private void SaveConfigRestartFlag()
        {
            try
            {
                var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configPath = System.IO.Path.Combine(modPath, "config", "config.json");

                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var configDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                    if (configDict != null && configDict.ContainsKey("Restart_New_RaidMode"))
                    {
                        configDict["Restart_New_RaidMode"] = JsonDocument.Parse("false").RootElement;

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        File.WriteAllText(configPath, JsonSerializer.Serialize(configDict, options));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[91m❌ [Config Save] Error saving config: {ex.Message}\x1b[0m");
            }
        }

        private void LockRaidModeQuestsInProfiles()
        {
            try
            {
                if (!_Restart_New_RaidMode)
                {
                    return;
                }

                var profiles = _saveServer.GetProfiles();
                int lockedCount = 0;

                foreach (var profile in profiles.Values)
                {
                    var pmcQuests = profile.CharacterData?.PmcData?.Quests;
                    if (pmcQuests != null)
                    {
                        foreach (var questId in _targetQuestIds)
                        {
                            var quest = pmcQuests.FirstOrDefault(q => q.QId == questId);
                            if (quest != null)
                            {
                                quest.Status = QuestStatusEnum.Locked;
                                lockedCount++;
                            }
                        }
                    }
                }

                SaveConfigRestartFlag();
                Console.WriteLine($"\x1b[92m⚔️ [Jiang Hu] Locked {lockedCount} raid mode quests in profiles\x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[91m❌ [Quest Lock] Error locking profile quests: {ex.Message}\x1b[0m");
            }
        }
    }
}
