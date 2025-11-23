using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

namespace JiangHu.Server
{
    [Injectable]
    public class EnableJianghuBot
    {
        private readonly ConfigServer _configServer;
        private bool _Enable_Jianghu_Bot = false;

        private readonly List<string> _bossBrains = new()
        {
                "bossBully",
                "bossGluhar",
                "bossKilla",
                "bossKojaniy",
                "bossSanitar",
                "bossTagilla",
                "bossKnight",
                "bossZryachiy",
                "bossBoar",
                "bossBoarSniper",
                "bossKolontay",
                "bossPartisan",
                "followerBigPipe",
                "followerBirdEye",
                "bossTagillaAgro",
                "bossKillaAgro",
                "tagillaHelperAgro"
        };

        public EnableJianghuBot(ConfigServer configServer)
        {
            _configServer = configServer;
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configPath = Path.Combine(modPath, "config", "config.json");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine("⚠️ [Jiang Hu] config.json not found for bot settings!");
                    return;
                }

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (config == null)
                    return;

                if (config.TryGetValue("Enable_Jianghu_Bot", out var botValue))
                    _Enable_Jianghu_Bot = botValue.GetBoolean();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error loading bot config: {ex.Message} \x1b[0m");
            }
        }

        public void ApplyBotSettings()
        {
            if (!_Enable_Jianghu_Bot)
                return;

            var botConfig = _configServer.GetConfig<BotConfig>();
            var pmcConfig = _configServer.GetConfig<PmcConfig>();

            if (botConfig == null || pmcConfig == null)
                return;

            if (botConfig.Bosses != null)
            {
                foreach (var pmcType in new[] { "pmcbot", "pmcbear", "pmcusec" })
                {
                    if (!botConfig.Bosses.Contains(pmcType))
                    {
                        botConfig.Bosses.Add(pmcType);
                    }
                }
            }

            if (botConfig.BotRolesWithDogTags != null)
            {
                foreach (var pmcType in new[] { "pmcbot", "pmcbear", "pmcusec" })
                {
                    if (!botConfig.BotRolesWithDogTags.Contains(pmcType))
                    {
                        botConfig.BotRolesWithDogTags.Add(pmcType);
                    }
                }
            }

            if (botConfig.Equipment != null)
            {
                var pmcEquipmentRole = "pmc";

                foreach (var bossType in _bossBrains)
                {
                    if (botConfig.Equipment.ContainsKey(bossType) && botConfig.Equipment[bossType] != null)
                    {
                        if (!botConfig.Equipment.ContainsKey(pmcEquipmentRole))
                        {
                            botConfig.Equipment[pmcEquipmentRole] = new EquipmentFilters();
                        }

                        var bossEquipment = botConfig.Equipment[bossType];
                        var pmcEquipment = botConfig.Equipment[pmcEquipmentRole];

                        if (bossEquipment != null && pmcEquipment != null)
                        {
                            pmcEquipment.WeaponModLimits = bossEquipment.WeaponModLimits;
                            pmcEquipment.Randomisation = bossEquipment.Randomisation;
                            pmcEquipment.ForceStock = bossEquipment.ForceStock;

                            if (bossEquipment.NvgIsActiveChanceDayPercent.HasValue)
                                pmcEquipment.NvgIsActiveChanceDayPercent = bossEquipment.NvgIsActiveChanceDayPercent;
                            if (bossEquipment.NvgIsActiveChanceNightPercent.HasValue)
                                pmcEquipment.NvgIsActiveChanceNightPercent = bossEquipment.NvgIsActiveChanceNightPercent;
                            if (bossEquipment.FaceShieldIsActiveChancePercent.HasValue)
                                pmcEquipment.FaceShieldIsActiveChancePercent = bossEquipment.FaceShieldIsActiveChancePercent;
                        }
                    }
                }
            }

            if (pmcConfig.PmcType != null)
            {
                var random = new Random();

                foreach (var mapEntry in pmcConfig.PmcType.ToList())
                {
                    var mapName = mapEntry.Key;
                    var sideBrains = mapEntry.Value;

                    foreach (var sideEntry in sideBrains.ToList())
                    {
                        var side = sideEntry.Key; 
                        var brainWeights = sideEntry.Value;

                        var newBrainWeights = new Dictionary<string, double>();

                        foreach (var bossBrain in _bossBrains)
                        {
                            newBrainWeights[bossBrain] = 1.0; 
                        }

                        pmcConfig.PmcType[mapName][side] = newBrainWeights;
                    }
                }

                Console.WriteLine($"\x1b[92m🤖 [Jiang Hu] Jianghu Bot activated, PMCs now use randomly selected brain from {_bossBrains.Count} bosses    使用boss大脑的新人机\x1b[0m");
            }
        }
    }
}
