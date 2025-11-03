using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Servers;

namespace JiangHu.Server
{
    [Injectable]
    public class Preset
    {
        private readonly SaveServer _saveServer;
        private string ConfigPath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "config", "config.json");

        public Preset(SaveServer saveServer)
        {
            _saveServer = saveServer;
        }

        public void ApplyPreset()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;

                var configJson = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configJson);

                if (config == null || !config.ContainsKey("Use_Preset") || !config["Use_Preset"].GetBoolean())
                    return;

                var profiles = _saveServer.GetProfiles();

                foreach (var profile in profiles.Values)
                {
                    var pmc = profile.CharacterData?.PmcData;
                    if (pmc?.Info == null) continue;

                    int prestigeLevel = pmc.Info.PrestigeLevel ?? 0;

                    if (prestigeLevel == 0)
                    {
                        WriteConfigSetting("Disable_Vanilla_Quests", true);
                        WriteConfigSetting("Unlock_AllItems_By_NewQuest", true);
                        WriteConfigSetting("Change_Prestige_Conditions", true);
                        WriteConfigSetting("Increase_HeadHP", true);
                        WriteConfigSetting("Lock_Flea", true);
                        WriteConfigSetting("Enable_empty_vanilla_shop", false);
                        WriteConfigSetting("Enable_No_Insurance", false);
                        WriteConfigSetting("Enable_Cash_Wipe", false);
                        WriteConfigSetting("Add_HideoutProduction_DSP", true);
                        WriteConfigSetting("Add_HideoutProduction_Labryskeycard", true);
                        WriteConfigSetting("Unlock_All_Labrys_Quests", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af8896a0")) // "Home In Tarkov - Part 1"
                    {
                        WriteConfigSetting("Lock_Flea", false);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d99999888080")) // "Full Gear Preparation"
                    {
                        WriteConfigSetting("Lock_Flea", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d99999888061")) // "Reaching The Oasis"
                    {
                        WriteConfigSetting("Enable_empty_vanilla_shop", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af888700")) // "Long March"
                    {
                        WriteConfigSetting("Enable_No_Insurance", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d99999888080")) // "Full Gear Preparation"
                    {
                        WriteConfigSetting("Enable_Cash_Wipe", true);
                    }

                    break;
                }
                ShowAppliedRulesLog();
            }
            catch (Exception ex)
            {
            }
        }

        private bool CheckQuestCompleted(PmcData pmc, string questId)
        {
            if (pmc.Quests == null) return false;

            foreach (var quest in pmc.Quests)
            {
                if (quest.QId == questId && quest.Status == QuestStatusEnum.Success)
                {
                    return true;
                }
            }
            return false;
        }

        private void WriteConfigSetting(string settingName, bool value)
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;

                var configJson = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configJson);
                if (config == null) return;

                var updatedConfig = new Dictionary<string, object>();
                foreach (var kvp in config)
                {
                    updatedConfig[kvp.Key] = kvp.Key == settingName ? value : kvp.Value;
                }

                if (!config.ContainsKey(settingName))
                {
                    updatedConfig[settingName] = value;
                }

                string updatedJson = JsonSerializer.Serialize(updatedConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, updatedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Preset] Error updating {settingName}: {ex.Message}");
            }
        }

        private void ShowAppliedRulesLog()
        {
            try
            {
                Console.WriteLine("\x1b[36m🧩 [Jiang Hu] Preset applied successfully\x1b[0m");
            }
            catch (Exception)
            {
            }
        }
    }
}
