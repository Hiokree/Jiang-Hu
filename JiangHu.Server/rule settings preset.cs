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
                {
                    Console.WriteLine("\x1b[93m🎮 [Jiang Hu] Free Mode On    自由模式开启\x1b[0m");
                    return;
                }

                var profiles = _saveServer.GetProfiles();

                foreach (var profile in profiles.Values)
                {
                    var pmc = profile.CharacterData?.PmcData;
                    if (pmc?.Info == null) continue;

                    // fixed settings
                    WriteConfigSetting("Enable_New_Trader", true);
                    WriteConfigSetting("Enable_New_Item", true);
                    WriteConfigSetting("Enable_Main_Quest", true);
                    WriteConfigSetting("Enable_Arena_Mode", true);
                    WriteConfigSetting("Remove_VanillaQuest_XP_reward", true);
                    WriteConfigSetting("Increase_HeadHP", true);
                    WriteConfigSetting("Enable_Dogtag_Collection", true);
                    WriteConfigSetting("Change_Prestige_Conditions", true);
                    WriteConfigSetting("Add_HideoutProduction_DSP", true);
                    WriteConfigSetting("Disable_Vanilla_Quests", true);
                    WriteConfigSetting("Lock_Flea", false);

                    // adjustable settings reset: Rewards

                    //// unlocks
                    WriteConfigSetting("Unlock_VanillaLocked_Items", false);
                    WriteConfigSetting("Unlock_VanilaTrader_TraderStanding", false);
                    WriteConfigSetting("Unlock_VanillaLocked_recipe", false);
                    WriteConfigSetting("Unlock_VanillaLocked_Customization", false);

                    //// skills
                    WriteConfigSetting("Enable_New_Movement", false);
                    WriteConfigSetting("Enable_Fast_Movement", false);
                    WriteConfigSetting("Enable_Fast_Leaning", false);
                    WriteConfigSetting("Enable_Fast_Pose_Transition", false);
                    WriteConfigSetting("Enable_Jump_Higher", false);
                    WriteConfigSetting("Enable_Slide", false);
                    WriteConfigSetting("Enable_Fast_Weapon_Switching", false);
                    WriteConfigSetting("Enable_Minimal_Aimpunch", false);
                    WriteConfigSetting("Enable_Fast_Aiming", false);
                    WriteConfigSetting("Enable_Wider_Freelook_Angle", false);

                    // adjustable settings reset: Challenges

                    //// disable flea first
                    WriteConfigSetting("Enable_Cash_Wipe", false);
                    WriteConfigSetting("Cash_Wipe_Coefficiency", 0.1);
                    WriteConfigSetting("Enable_No_Insurance", false);
                    WriteConfigSetting("Enable_empty_vanilla_shop", false);

                    // unlocks
                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af881000")) // "Home In Tarkov - Part 1"
                    {
                        WriteConfigSetting("Unlock_VanillaLocked_recipe", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af880100")) // "Prologue - Ground Zero"
                    {
                        WriteConfigSetting("Unlock_VanillaLocked_Customization", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af880300")) // "Prologue - Interchange"
                    {
                        WriteConfigSetting("Unlock_VanilaTrader_TraderStanding", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af880600")) // "Prologue - Reserve"
                    {
                        WriteConfigSetting("Unlock_VanillaLocked_Items", true);
                    }

                    // skills
                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af880200")) // "Prologue - Customs"
                    {
                        WriteConfigSetting("Enable_Wider_Freelook_Angle", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af880400")) // "Prologue - Factory"
                    {
                        WriteConfigSetting("Enable_New_Movement", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af880500")) // "Prologue - Woods"
                    {
                        WriteConfigSetting("Enable_Slide", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af880700")) // "Prologue - Shoreline"
                    {
                        WriteConfigSetting("Enable_Minimal_Aimpunch", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af880800")) // "Prologue - Lighthouse"
                    {
                        WriteConfigSetting("Enable_Fast_Pose_Transition", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af880900")) // "Prologue - Streets of Tarkov"
                    {
                        WriteConfigSetting("Enable_Fast_Leaning", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af880a00")) // "Prologue - Lab"
                    {
                        WriteConfigSetting("Enable_Fast_Weapon_Switching", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af886000")) // "Big Tarkov Family"
                    {
                        WriteConfigSetting("Enable_Fast_Movement", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d999998880f0")) // "Oasis - Professional  Part 2"
                    {
                        WriteConfigSetting("Enable_Jump_Higher", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d999998880b0")) // "Oasis - Precise Identification  Part 2"
                    {
                        WriteConfigSetting("Enable_Fast_Aiming", true);
                    }

                    // Challenges
                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af887000")) // "The Survivalist"
                    {
                        WriteConfigSetting("Lock_Flea", true);
                    }
                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af884000")) // "Prologue"
                    {
                        WriteConfigSetting("Enable_Cash_Wipe", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af884000")) // "Prologue"
                    {
                        WriteConfigSetting("Cash_Wipe_Coefficiency", 0.1);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af885000")) // "Reaching The Oasis"
                    {
                        WriteConfigSetting("Enable_No_Insurance", true);
                    }

                    if (CheckQuestCompleted(pmc, "e983002c4ab4d229af888000")) // "Long March"
                    {
                        WriteConfigSetting("Enable_empty_vanilla_shop", true);
                    }

                    break;
                }
                Console.WriteLine("\x1b[93m🎮 [Jiang Hu] Story Mode On    剧情模式开启\x1b[0m");
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

        private void WriteConfigSetting(string settingName, object value)
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
    }
}
