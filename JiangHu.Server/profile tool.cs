using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Servers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JiangHu.Server
{
    [Injectable]
    public class ProfileTool
    {
        private readonly SaveServer _saveServer;
        private readonly DatabaseServer _databaseServer;

        public ProfileTool(SaveServer saveServer, DatabaseServer databaseServer)
        {
            _saveServer = saveServer;
            _databaseServer = databaseServer;
        }

        public Task ApplyProfileSettings()
        {
            try
            {
                var modPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var profilePath = System.IO.Path.Combine(modPath, "config", "profile_setting.json");

                if (!File.Exists(profilePath))
                    return Task.CompletedTask;

                var json = File.ReadAllText(profilePath);
                var profileConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (profileConfig == null)
                    return Task.CompletedTask;

                if (profileConfig.TryGetValue("enable_debug_tool", out var debugValue))
                {
                    bool enableDebug = debugValue.GetBoolean();
                    if (!enableDebug)
                    {
                        return Task.CompletedTask;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProfileTool] Error reading profile config: {ex.Message}");
                return Task.CompletedTask;
            }

            try
            {
                var modPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var profilePath = System.IO.Path.Combine(modPath, "config", "profile_setting.json");

                if (!File.Exists(profilePath))
                    return Task.CompletedTask;

                var json = File.ReadAllText(profilePath);
                var profileConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (profileConfig == null)
                    return Task.CompletedTask;

                // --- Apply quest status ---
                if (profileConfig.TryGetValue("finalQueststatus", out var statusValue))
                {
                    int status = statusValue.GetInt32();
                    SetFinalQuestsStatus((QuestStatusEnum) status);
                    Console.WriteLine($"🧭 [ProfileTool] Final quests set to status ID: {status}");
                }

                // 🔹 Apply player XP
                if (profileConfig.TryGetValue("playerXP", out var xpValue))
                {
                    int targetXP = xpValue.GetInt32();
                    SetPlayerXP(targetXP);
                    Console.WriteLine($"💪 [ProfileTool] Player XP set to {targetXP}");
                }

                // 🔹 Apply added Rouble
                if (profileConfig.TryGetValue("addrouble", out var rubValue))
                {
                    int addRUB = rubValue.GetInt32();
                    AddRoubleToProfile(addRUB);
                }

                // 🔹 Max hideout levels
                if (profileConfig.TryGetValue("all_hideout_level", out var hideoutFlag) && hideoutFlag.GetBoolean())
                {
                    AllHideoutLevel();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [ProfileTool] Error applying profile settings: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private void SetFinalQuestsStatus(QuestStatusEnum status)
        {
            var profiles = _saveServer.GetProfiles();
            var questTemplates = _databaseServer.GetTables().Templates.Quests;

            foreach (var profile in profiles.Values)
            {
                if (profile.CharacterData?.PmcData?.Quests == null)
                {
                    profile.CharacterData.PmcData.Quests = new List<QuestStatus>();
                }
                var targetQuestId = "e983002c4ab4d229af8882b2";
                var existingQuest = profile.CharacterData.PmcData.Quests
                    .FirstOrDefault(q => q.QId == targetQuestId);

                if (existingQuest == null)
                {
                    profile.CharacterData.PmcData.Quests.Add(new QuestStatus
                    {
                        QId = targetQuestId,
                        Status = status,
                        StartTime = 0,
                        StatusTimers = new Dictionary<QuestStatusEnum, double>()
                    });
                }
                else
                {
                    existingQuest.Status = status;
                }
            }
        }

        private void SetPlayerXP(int xp)
        {
            var profiles = _saveServer.GetProfiles();

            foreach (var profile in profiles.Values)
            {
                var pmc = profile.CharacterData?.PmcData;
                if (pmc == null)
                    continue;

                pmc.Info.Experience = xp;
            }
        }

        private void AddRoubleToProfile(int amount)
        {
            if (amount <= 0) return;

            var profiles = _saveServer.GetProfiles();
            var RUB_TPL = new MongoId("5449016a4bdc2d6f028b456f");

            foreach (var profile in profiles.Values)
            {
                var pmc = profile.CharacterData?.PmcData;
                var inventory = pmc?.Inventory?.Items;
                if (inventory == null) continue;

                var newRoubleItem = new Item
                {
                    Id = new MongoId(),
                    Template = RUB_TPL,
                    ParentId = "664113fa049fd2369707c4eb",
                    SlotId = "hideout",
                    Upd = new Upd
                    {
                        StackObjectsCount = (double) amount,
                        SpawnedInSession = true
                    }
                };

                inventory.Add(newRoubleItem);

                Console.WriteLine($"💰 [ProfileTool] Added {amount} RUB");
            }
        }

        private void AllHideoutLevel()
        {
            try
            {
                var profiles = _saveServer.GetProfiles();

                foreach (var profile in profiles.Values)
                {
                    var pmc = profile.CharacterData?.PmcData;
                    if (pmc?.Hideout?.Areas == null)
                    {
                        Console.WriteLine($"⚪ [{pmc?.Info?.Nickname ?? "Unknown"}] No hideout data found.");
                        continue;
                    }

                    Console.WriteLine($"\n🏠 [Hideout Upgrade] {pmc.Info.Nickname}");
                    Console.WriteLine(new string('-', 45));

                    int index = 0;
                    foreach (var area in pmc.Hideout.Areas)
                    {
                        area.Level = 1;
                        area.Active = true;
                        area.PassiveBonusesEnabled = true;
                        area.CompleteTime = 0;
                        area.Constructing = false;

                        Console.WriteLine(
                            $"[{index}] Type: {area.Type}, Level: {area.Level}, Active: {area.Active}, " +
                            $"BonusesEnabled: {area.PassiveBonusesEnabled}, Constructing: {area.Constructing}"
                        );
                        index++;
                    }

                    Console.WriteLine(new string('-', 45));
                }

                Console.WriteLine("✅ [ProfileTool] All hideout areas upgraded to level 1.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [ProfileTool] Error upgrading hideout: {ex.Message}");
            }
        }
    }
}
