using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Fence;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Json;


namespace JiangHu.Server
{
    [Injectable]
    public class RuleSettings
    {
        private readonly SaveServer _saveServer;
        private readonly DatabaseServer _databaseServer;
        private readonly FenceService _fenceService;

        // Config flags
        private bool _Disable_Vanilla_Quests = false;
        private bool _Increase_HeadHP = false;
        private bool _Lock_Flea = false;
        private bool _Enable_No_Insurance = false;
        private bool _Enable_Empty_Vanilla_Shop = false;
        private bool _Disable_Secure_Container = false;

        private bool _Unlock_VanillaLocked_Items = false;
        private bool _Unlock_VanilaTrader_TraderStanding = false;
        private bool _Unlock_VanillaLocked_recipe = false;
        private bool _Unlock_VanillaLocked_Customization = false;


        private bool _Change_Prestige_Conditions = false;
        private bool _Add_HideoutProduction_DSP = false;
        private bool _Enable_Replace_OneRaid_with_OneLife = false;
        private bool _Remove_VanillaQuest_XP_reward = false;

        private bool _Default_Match_Time = true;
        private int _DeathMatch_Match_Time = 0;

        private readonly ConfigServer _configServer;

        public RuleSettings(SaveServer saveServer, DatabaseServer databaseServer, ConfigServer configServer, FenceService fenceService)
        {
            _saveServer = saveServer;
            _databaseServer = databaseServer;
            _configServer = configServer; 
            _fenceService = fenceService;

            LoadConfig();
        }
        private FenceService GetFenceService()
        {
            return _fenceService;
        }

        private void LoadConfig()
        {
            try
            {
                var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configPath = System.IO.Path.Combine(modPath, "config", "config.json");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine("⚠️ [Jiang Hu] config.json not found!");
                    return;
                }

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (config == null)
                    return;

                if (config.TryGetValue("Remove_VanillaQuest_XP_reward", out var xpRewardValue))
                    _Remove_VanillaQuest_XP_reward = xpRewardValue.GetBoolean();

                if (config.TryGetValue("Unlock_VanillaLocked_Items", out var itemsValue))
                    _Unlock_VanillaLocked_Items = itemsValue.GetBoolean();

                if (config.TryGetValue("Unlock_VanilaTrader_TraderStanding", out var standingValue))
                    _Unlock_VanilaTrader_TraderStanding = standingValue.GetBoolean();

                if (config.TryGetValue("Unlock_VanillaLocked_recipe", out var recipeValue))
                    _Unlock_VanillaLocked_recipe = recipeValue.GetBoolean();

                if (config.TryGetValue("Unlock_VanillaLocked_Customization", out var customizationValue))
                    _Unlock_VanillaLocked_Customization = customizationValue.GetBoolean();

                if (config.TryGetValue("Disable_Vanilla_Quests", out var questValue))
                    _Disable_Vanilla_Quests = questValue.GetBoolean();

                if (config.TryGetValue("Lock_Flea", out var fleaValue))
                    _Lock_Flea = fleaValue.GetBoolean();

                if (config.TryGetValue("Add_HideoutProduction_DSP", out var dspValue))
                    _Add_HideoutProduction_DSP = dspValue.GetBoolean();

                if (config.TryGetValue("Change_Prestige_Conditions", out var prestigeValue))
                    _Change_Prestige_Conditions = prestigeValue.GetBoolean();

                if (config.TryGetValue("Enable_No_Insurance", out var insuranceValue))
                    _Enable_No_Insurance = insuranceValue.GetBoolean();

                if (config.TryGetValue("Enable_empty_vanilla_shop", out var emptyShopValue))
                    _Enable_Empty_Vanilla_Shop = emptyShopValue.GetBoolean();

                if (config.TryGetValue("Increase_HeadHP", out var headValue))
                    _Increase_HeadHP = headValue.GetBoolean();

                if (config.TryGetValue("Enable_Replace_OneRaid_with_OneLife", out var oneLifeValue))
                    _Enable_Replace_OneRaid_with_OneLife = oneLifeValue.GetBoolean();

                if (config.TryGetValue("Disable_Secure_Container", out var ContainerValue))
                    _Disable_Secure_Container = ContainerValue.GetBoolean();

                if (config.TryGetValue("Default_Match_Time", out var defaultMatchTimeValue))
                    _Default_Match_Time = defaultMatchTimeValue.GetBoolean();

                if (config.TryGetValue("DeathMatch_Match_Time", out var matchTimeValue))
                    _DeathMatch_Match_Time = matchTimeValue.GetInt32();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error loading config: {ex.Message} \x1b[0m");
            }
        }

        public Task ApplySettings()
        {
            try
            {
                var tables = _databaseServer.GetTables();


                if (_Remove_VanillaQuest_XP_reward)
                    RemoveXPRewardsFromVanillaQuests(tables);

                if (_Unlock_VanillaLocked_Items)
                    UnlockVanillaLockedItems(tables);

                if (_Unlock_VanilaTrader_TraderStanding)
                    UnlockVanilaTraderTraderStanding(tables);            

                if (_Unlock_VanillaLocked_recipe)
                    UnlockVanillaLockedRecipe(tables);

                if (_Unlock_VanillaLocked_Customization)
                    RemoveCustomizationDirectRewards(tables);

                if (_Disable_Vanilla_Quests)
                    DisableVanillaQuests(tables);

                LockFlea(tables);

                if (_Add_HideoutProduction_DSP)
                    AddHideoutProductionDSP(tables);

                if (_Change_Prestige_Conditions)
                    Change_Prestige_Conditions(tables);

                if (_Enable_No_Insurance)
                    DisableInsurance(tables);

                if (_Enable_Empty_Vanilla_Shop)
                    DisableVanillaShops(tables);

                if (_Increase_HeadHP)
                    IncreaseHeadHP();

                if (_Enable_Replace_OneRaid_with_OneLife)
                    ReplaceOneRaidWithOneLife(tables);

                if (_Disable_Secure_Container)
                    DisableSecureContainer(tables);

                SetRaidTimeLength(tables);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error applying settings: {ex.Message}  \x1b[0m");
            }

            return Task.CompletedTask;
        }

        private readonly HashSet<string> _targetTraderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "54cb50c76803fa8b248b4571", // Prapor
            "54cb57776803fa99248b456e", // Therapist
            "58330581ace78e27b8b10cee", // Skier
            "5935c25fb3acc3127c3d8cd9", // Peacekeeper
            "5a7c2eca46aef81a7ca2145d", // Mechanic
            "5ac3b934156ae10c4430e83c", // Ragman
            "5c0647fdd443bc2504c2d371", // Jaeger
            "6617beeaa9cfa777ca915b7c", // Ref
            "579dc571d53a0658a154fbec",  // Fence
            "656f0f98d80a697f855d34b1", // BTR Driver
            "638f541a29ffd1183d187f57" // Lightkeeper
        };

        // 🔹 Remove Vanilla Quest XP reward (all vanilla quest modifications run before DisableVanillaQuests)
        private void RemoveXPRewardsFromVanillaQuests(DatabaseTables tables)
        {
            var quests = tables.Templates.Quests;
            int modifiedCount = 0;

            foreach (var kvp in quests)
            {
                string questId = kvp.Key.ToString();

                if (questId.StartsWith("e983", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrEmpty(kvp.Value.TraderId) || !_targetTraderIds.Contains(kvp.Value.TraderId))
                    continue;

                var quest = kvp.Value;

                if (quest.Rewards != null && quest.Rewards.ContainsKey("Success"))
                {
                    var successRewards = quest.Rewards["Success"];
                    var originalCount = successRewards.Count;

                    successRewards.RemoveAll(reward => reward.Type == RewardType.Experience);

                    if (successRewards.Count != originalCount)
                    {
                        modifiedCount++;
                    }

                    if (!successRewards.Any())
                    {
                        successRewards.Clear();
                    }
                }
            }

            Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Removed XP rewards from {modifiedCount} vanilla quests    移除原版任务经验奖励\x1b[0m");
        }

        // 🔹 Unlock Vanilla Locked Items
        private void UnlockVanillaLockedItems(DatabaseTables tables)
        {
            var quests = tables.Templates.Quests;
            int modifiedCount = 0;

            foreach (var kvp in quests)
            {
                string questId = kvp.Key.ToString();
                if (questId.StartsWith("e983", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(kvp.Value.TraderId) || !_targetTraderIds.Contains(kvp.Value.TraderId)) continue;

                var quest = kvp.Value;
                if (quest.Rewards != null && quest.Rewards.ContainsKey("Success"))
                {
                    var successRewards = quest.Rewards["Success"];
                    var originalCount = successRewards.Count;
                    successRewards.RemoveAll(reward => reward.Type == RewardType.AssortmentUnlock);
                    if (successRewards.Count != originalCount) modifiedCount++;
                    if (!successRewards.Any()) successRewards.Clear();
                }
            }

            foreach (var traderEntry in tables.Traders)
            {
                var trader = traderEntry.Value;
                if (trader.QuestAssort != null && _targetTraderIds.Contains(trader.Base.Id))
                {
                    trader.QuestAssort.Clear();
                    modifiedCount++;
                }
            }

            Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Removed AssortmentUnlock from {modifiedCount} sources    移除商人解锁限制\x1b[0m");
        }

        // 🔹 Unlock Vanila traders, max all Vanila trader standings, and clear traderstanding rewards
        private void UnlockVanilaTraderTraderStanding(DatabaseTables tables)
        {
            var quests = tables.Templates.Quests;
            int modifiedCount = 0;

            // Unlock Jaeger and Ref
            var jaegerId = new MongoId("5c0647fdd443bc2504c2d371"); // Jaeger
            var refId = new MongoId("6617beeaa9cfa777ca915b7c"); // Ref

            if (tables.Traders.TryGetValue(jaegerId, out var jaeger))
            {
                jaeger.Base.UnlockedByDefault = true;
                modifiedCount++;
            }

            if (tables.Traders.TryGetValue(refId, out var refTrader))
            {
                refTrader.Base.UnlockedByDefault = true;
                modifiedCount++;
            }

            // Set all vanilla traders' standing to 6 in all player profiles
            var profiles = _saveServer.GetProfiles();
            foreach (var profile in profiles.Values)
            {
                if (profile.CharacterData?.PmcData?.TradersInfo != null)
                {
                    foreach (var traderId in _targetTraderIds)
                    {
                        if (profile.CharacterData.PmcData.TradersInfo.TryGetValue(traderId, out var traderInfo))
                        {
                            traderInfo.Standing = 6.0;
                            modifiedCount++;
                        }
                    }
                }
            }

            // Remove TraderStanding from quest rewards
            foreach (var kvp in quests)
            {
                string questId = kvp.Key.ToString();
                if (questId.StartsWith("e983", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(kvp.Value.TraderId) || !_targetTraderIds.Contains(kvp.Value.TraderId)) continue;

                var quest = kvp.Value;
                if (quest.Rewards != null && quest.Rewards.ContainsKey("Success"))
                {
                    var successRewards = quest.Rewards["Success"];
                    var originalCount = successRewards.Count;
                    successRewards.RemoveAll(reward => reward.Type == RewardType.TraderStanding);
                    if (successRewards.Count != originalCount) modifiedCount++;
                    if (!successRewards.Any()) successRewards.Clear();
                }
            }

            Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Unlock traders, max standing, and clear standing rewards   解锁原版任务商人，满声望，清除声望奖励\x1b[0m");
        }

        // 🔹 Unlock Vanilla Locked Recipe
        private void UnlockVanillaLockedRecipe(DatabaseTables tables)
        {
            var quests = tables.Templates.Quests;

            foreach (var kvp in quests)
            {
                string questId = kvp.Key.ToString();
                if (questId.StartsWith("e983", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(kvp.Value.TraderId) || !_targetTraderIds.Contains(kvp.Value.TraderId)) continue;

                var quest = kvp.Value;
                if (quest.Rewards != null && quest.Rewards.ContainsKey("Success"))
                {
                    var successRewards = quest.Rewards["Success"];
                    successRewards.RemoveAll(reward => reward.Type == RewardType.ProductionScheme);
                    if (!successRewards.Any()) successRewards.Clear();
                }
            }

            if (tables.Hideout?.Production?.Recipes != null)
            {
                foreach (var recipe in tables.Hideout.Production.Recipes)
                {
                    if (recipe.Locked == true)
                    {
                        recipe.Locked = false;
                    }

                    if (recipe.Requirements != null)
                    {
                        recipe.Requirements.RemoveAll(req => req.QuestId != null);
                    }
                }
            }
            Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Unlock All Vanilla Production Recipes    解锁全部原版制作配方\x1b[0m");
        }

        // 🔹 Unlock Vanilla Customization
        private void RemoveCustomizationDirectRewards(DatabaseTables tables)
        {
            var quests = tables.Templates.Quests;
            int modifiedCount = 0;

            foreach (var kvp in quests)
            {
                string questId = kvp.Key.ToString();
                if (questId.StartsWith("e983", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(kvp.Value.TraderId) || !_targetTraderIds.Contains(kvp.Value.TraderId)) continue;

                var quest = kvp.Value;
                if (quest.Rewards != null && quest.Rewards.ContainsKey("Success"))
                {
                    var successRewards = quest.Rewards["Success"];
                    var originalCount = successRewards.Count;
                    successRewards.RemoveAll(reward => reward.Type == RewardType.CustomizationDirect);
                    if (successRewards.Count != originalCount) modifiedCount++;
                    if (!successRewards.Any()) successRewards.Clear();
                }
            }

            if (tables.Hideout?.Customisation != null)
            {
                if (tables.Hideout.Customisation.Globals != null)
                {
                    foreach (var global in tables.Hideout.Customisation.Globals)
                    {
                        global.Conditions?.Clear();
                        modifiedCount++;
                    }
                }

                if (tables.Hideout.Customisation.Slots != null)
                {
                    foreach (var slot in tables.Hideout.Customisation.Slots)
                    {
                        slot.Conditions?.Clear();
                        modifiedCount++;
                    }
                }
            }

            Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Unlock all hideout Customization    解锁全部藏身处装饰\x1b[0m");
        }

        // 🔹 Disable Vanilla Quests (change start condition to lvl 99 and move them to new trader)
        private void DisableVanillaQuests(DatabaseTables tables)
        {
            var quests = tables.Templates.Quests;
            int modifiedCount = 0;
            int traderChangedCount = 0;
            string customTraderId = "e983002c4ab4d99999888000";

            foreach (var kvp in quests)
            {
                string questId = kvp.Key.ToString();

                if (questId.StartsWith("e983", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrEmpty(kvp.Value.TraderId) || !_targetTraderIds.Contains(kvp.Value.TraderId))
                    continue;

                kvp.Value.TraderId = customTraderId;
                traderChangedCount++;

                var quest = kvp.Value;
                quest.Conditions.AvailableForStart = new List<QuestCondition>();

                var condition = new QuestCondition
                {
                    Id = new MongoId(Guid.NewGuid().ToString("N").Substring(0, 24)),
                    Index = quest.Conditions.AvailableForStart.Count + 1,
                    ConditionType = "Level",
                    CompareMethod = ">=",
                    DynamicLocale = false,
                    GlobalQuestCounterId = string.Empty,
                    ParentId = string.Empty,
                    Value = 99,
                    VisibilityConditions = new List<VisibilityCondition>()
                };

                quest.Conditions.AvailableForStart = new List<QuestCondition> { condition };
                modifiedCount++;
            }

            Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Disabled and moved {traderChangedCount} vanilla quests to Loong Gate Inn    锁定原版任务并移至龙门客栈\x1b[0m");
        }

        // 🔹 Lock Flea
        private void LockFlea(DatabaseTables tables)
        {
            var globals = tables.Globals;
            var ragfair = globals.Configuration.RagFair;

            if (_Lock_Flea)
            {
                ragfair.MinUserLevel = 99;
                Console.WriteLine("\x1b[36m🎮 [Jiang Hu] Flea Market locked    锁定跳蚤市场\x1b[0m");

            }
            else
            {
                ragfair.MinUserLevel = 1;
                Console.WriteLine("\x1b[36m🎮 [Jiang Hu] Flea Market unlocked    解锁跳蚤市场\x1b[0m");
            }
        }

        // 🔹 Add DSP Recipe
        private void AddHideoutProductionDSP(DatabaseTables tables)
        {
            try
            {
                var newRecipe = new HideoutProduction
                {
                    Id = new MongoId("e983002c4ab4d99999888200"),
                    AreaType = (HideoutAreas) 11,
                    Requirements = new List<Requirement>
                    {
                        new Requirement
                        {
                            TemplateId = new MongoId("590c2e1186f77425357b6124"),
                            Type = "Tool",
                            Count = 1
                        },
                        new Requirement
                        {
                            QuestId = new MongoId("e983002c4ab4d229af880700"),
                            Type = "QuestComplete"
                        }
                    },
                    ProductionTime = 60,
                    EndProduct = new MongoId("62e910aaf957f2915e0a5e36"),
                    IsEncoded = true,
                    Locked = true,
                    NeedFuelForAllProductionTime = true,
                    Continuous = false,
                    Count = 1,
                    ProductionLimitCount = 0,
                    IsCodeProduction = false
                };

                tables.Hideout.Production.Recipes.Add(newRecipe);

                Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Lighthouse DSP production recipe added   灯塔通行道具制作配方\x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error adding hideout production: {ex.Message} \x1b[0m");
            }
        }

        // 🔹 Change Prestige Conditions
        private void Change_Prestige_Conditions(DatabaseTables tables)
        {
            try
            {
                if (!_Change_Prestige_Conditions)
                {
                    Console.WriteLine("ℹ️ [Jiang Hu] Change_Prestige_Conditions is false. Skipping prestige conditions replacement.");
                    return;
                }

                var originalPrestige = tables.Templates.Prestige;
                if (originalPrestige?.Elements == null || originalPrestige.Elements.Count == 0)
                {
                    Console.WriteLine("⚠️ [Jiang Hu] No original prestige elements found.");
                    return;
                }

                var conditionIds = new[]
                {
                    "e983002c4ab4d99999aaaa01", // Level 1
                    "e983002c4ab4d99999aaaa11", // Level 2  
                    "e983002c4ab4d99999aaaa21", // Level 3
                    "e983002c4ab4d99999aaaa31"  // Level 4
                };

                for (int i = 0; i < originalPrestige.Elements.Count; i++)
                {
                    var element = originalPrestige.Elements[i];

                    element.Conditions.Clear();

                    element.Conditions.Add(new QuestCondition
                    {
                        Id = new MongoId(conditionIds[i]),
                        Index = 1,
                        DynamicLocale = false,
                        VisibilityConditions = new List<VisibilityCondition>(),
                        GlobalQuestCounterId = "",
                        ParentId = "",
                        Target = new ListOrT<string>(null, "e983002c4ab4d229af880000"),
                        Status = new HashSet<QuestStatusEnum> { QuestStatusEnum.Success },
                        AvailableAfter = 0,
                        Dispersion = 0,
                        ConditionType = "Quest"
                    });

                    element.TransferConfigs = new TransferConfigs
                    {
                        StashConfig = new StashPrestigeConfig
                        {
                            XCellCount = 0,
                            YCellCount = 0,
                            Filters = new StashPrestigeFilters
                            {
                                IncludedItems = new List<MongoId>(),
                                ExcludedItems = new List<MongoId>()
                            }
                        },
                        SkillConfig = new PrestigeSkillConfig { TransferMultiplier = element.TransferConfigs?.SkillConfig?.TransferMultiplier ?? 0 },
                        MasteringConfig = new PrestigeMasteringConfig { TransferMultiplier = element.TransferConfigs?.MasteringConfig?.TransferMultiplier ?? 0 }
                    };
                }

                Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Prestige conditions and transfer configs replaced for {originalPrestige.Elements.Count} levels    新转生条件\x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error replacing prestige conditions: {ex.Message} \x1b[0m");
            }
        }

        // 🔹 Disable Insurance
        private void DisableInsurance(DatabaseTables tables)
        {
            if (!_Enable_No_Insurance)
                return;

            var disabledTraders = new List<string>();

            foreach (var traderEntry in tables.Traders)
            {
                var trader = traderEntry.Value?.Base;
                if (trader?.Insurance?.Availability != true)
                    continue;

                if (trader.LoyaltyLevels == null)
                    continue;

                foreach (var level in trader.LoyaltyLevels)
                {
                    level.InsurancePriceCoefficient = 99999999999;
                }

                disabledTraders.Add(trader.Nickname ?? trader.Name);
            }
            if (disabledTraders.Count > 0)
            {
                Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Insurance disabled for traders: {string.Join(", ", disabledTraders)}    禁保险\x1b[0m");
            }
        }

        // 🔹 Disable Vanilla Shops
        private void DisableVanillaShops(DatabaseTables tables)
        {
            var lockedTraderIds = new[]
            {
                "54cb50c76803fa8b248b4571", // Prapor
                "54cb57776803fa99248b456e", // Therapist
                "58330581ace78e27b8b10cee", // Skier
                "5935c25fb3acc3127c3d8cd9", // Peacekeeper
                "5a7c2eca46aef81a7ca2145d", // Mechanic
                "5ac3b934156ae10c4430e83c", // Ragman
                "5c0647fdd443bc2504c2d371", // Jaeger
                "6617beeaa9cfa777ca915b7c"  // Fence
            };

            var clearedTraders = new List<string>();

            foreach (var traderId in lockedTraderIds)
            {
                if (!tables.Traders.TryGetValue(traderId, out var trader))
                    continue;

                if (trader?.Assort != null)
                {
                    trader.Assort.Items.Clear();
                    trader.Assort.BarterScheme.Clear();
                    trader.Assort.LoyalLevelItems.Clear();
                    clearedTraders.Add(trader.Base.Name);
                }
            }

            DisableFenceAssort(tables, true);
            if (clearedTraders.Count > 0)
                Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Vanilla shops cleared    禁商店\x1b[0m");
        }

        // 🔹 Disable Fence Shop
        public void DisableFenceAssort(DatabaseTables tables, bool enableEmptyVanillaShop)
        {
            if (!enableEmptyVanillaShop)
                return;

            try
            {
                var traderConfig = _configServer.GetConfig<TraderConfig>();
                var fence = traderConfig.Fence;

                fence.AssortSize = 0;
                fence.DiscountOptions.AssortSize = 0;
                fence.EquipmentPresetMinMax.Min = 0;
                fence.EquipmentPresetMinMax.Max = 0;
                fence.DiscountOptions.EquipmentPresetMinMax.Min = 0;
                fence.DiscountOptions.EquipmentPresetMinMax.Max = 0;
                fence.WeaponPresetMinMax.Min = 0;
                fence.WeaponPresetMinMax.Max = 0;
                fence.DiscountOptions.WeaponPresetMinMax.Min = 0;
                fence.DiscountOptions.WeaponPresetMinMax.Max = 0;

                fence.RegenerateAssortsOnRefresh = false;
                fence.PartialRefreshTimeSeconds = 0;
                fence.PartialRefreshChangePercent = 0;

                _fenceService.GenerateFenceAssorts();

                _fenceService.SetFenceAssort(CreateEmptyAssort());
                _fenceService.SetFenceDiscountAssort(CreateEmptyAssort());

                Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Fence disabled    禁倒爷\x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m⚠️ [Jiang Hu] Fence disable failed: {ex.Message} \x1b[0m");
            }
        }

        private TraderAssort CreateEmptyAssort()
        {
            return new TraderAssort
            {
                Items = new List<Item>(),
                BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                LoyalLevelItems = new Dictionary<MongoId, int>(),
                NextResupply = long.MaxValue
            };
        }

        private void DisableSecureContainer(DatabaseTables tables)
        {
            var items = tables.Templates.Items; 

            MongoId[] secureContainerIds =
            [
                new("544a11ac4bdc2d470e8b456a"), // Alpha
                new("5857a8b324597729ab0a0e7d"), // Beta
                new("5857a8bc2459772bad15db29"), // Gamma
                new("665ee77ccf2d642e98220bca"), // Gamma Unheard
                new("59db794186f77448bc595262"), // Epsilon
                new("664a55d84a90fc2c8a6305c9"), // Theta
                new("5c093ca986f7740a1867ab12"), // Kappa
                new("676008db84e242067d0dc4c9"), // Kappa (Desecrated)
                new("5732ee6a24597719ae0c0281")  // Waist Pouch
            ];

            foreach (var containerId in secureContainerIds)
            {
                if (!items.TryGetValue(containerId, out var containerObj) || containerObj is not TemplateItem container)
                    continue;

                var grids = container.Properties.Grids?.ToList() ?? [];
                if (grids.Count == 0) continue;

                var gridFilters = grids[0].Properties.Filters;
                if (gridFilters == null) continue;

                var filterList = gridFilters.ToList();
                if (filterList.Count == 0) continue;

                // Clear the filter - set to empty HashSet so nothing can be put in
                filterList[0].Filter = new HashSet<MongoId>();
                grids[0].Properties.Filters = filterList;

                container.Properties.Grids = grids;
            }

            Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Secure container Disabled    安全箱已禁用\x1b[0m");
        }

        // 🔹 Increase Head HP
        private void IncreaseHeadHP()
        {
            try
            {
                var profiles = _saveServer.GetProfiles();
                int modifiedCount = 0;

                foreach (var kvp in profiles)
                {
                    var profile = kvp.Value;
                    var pmc = profile?.CharacterData?.PmcData;
                    if (pmc == null)
                        continue;

                    var exp = pmc?.Info?.Experience ?? 0;
                    int bonusHp = Math.Min(exp / 20000, 65);
                    int newMax = Math.Min(35 + bonusHp, 100);

                    if (pmc.Health?.BodyParts == null || !pmc.Health.BodyParts.ContainsKey("Head"))
                        continue;

                    var head = pmc.Health.BodyParts["Head"];

                    head.Health.Maximum = newMax;
                    if (head.Health.Current > newMax)
                        head.Health.Current = newMax;
                    Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Increased {pmc.Info.Nickname}'s Head HP to {newMax} (+{newMax - 35})    头变大啦\x1b[0m");
                    modifiedCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error adjusting head HP: {ex.Message}  \x1b[0m");
            }
        }

        // 🔹 Replace One-Raid with One-Life for specific quests
        private void ReplaceOneRaidWithOneLife(DatabaseTables tables)
        {
            try
            {
                if (!_Enable_Replace_OneRaid_with_OneLife)
                    return;

                var quests = tables.Templates.Quests;
                int modifiedCount = 0;

                // Long March
                if (quests.TryGetValue("e983002c4ab4d229af888000", out var quest08))
                {
                    if (quest08.Conditions?.AvailableForFinish != null)
                    {
                        foreach (var condition in quest08.Conditions.AvailableForFinish)
                        {
                            if (condition.OneSessionOnly == true)
                            {
                                condition.OneSessionOnly = false;
                            }
                        }
                    }

                    if (quest08.Conditions?.AvailableForFinish != null)
                    {
                        quest08.Conditions.AvailableForFinish.RemoveAll(condition =>
                            condition.Counter?.Conditions?.Any(counterCondition =>
                                counterCondition.ConditionType == "ExitStatus") == true
                        );
                    }

                    var failCondition700 = new QuestCondition
                    {
                        Id = new MongoId("e983002c4ab4d229af888090"),
                        Index = 0,
                        ConditionType = "CounterCreator",
                        DynamicLocale = false,
                        GlobalQuestCounterId = string.Empty,
                        ParentId = string.Empty,
                        OneSessionOnly = false,
                        Value = 1,
                        Type = "Exploration",
                        VisibilityConditions = new List<VisibilityCondition>(),
                        Counter = new QuestConditionCounter
                        {
                            Id = "e983002c4ab4d229af888091",
                            Conditions = new List<QuestConditionCounterCondition>
                    {
                        new QuestConditionCounterCondition
                        {
                            Id = new MongoId("e983002c4ab4d229af888092"),
                            ConditionType = "ExitStatus",
                            DynamicLocale = false,
                            Status = new List<string> { "Killed", "MissingInAction", "Left" }
                        }
                    }
                        }
                    };

                    if (quest08.Conditions.Fail == null)
                        quest08.Conditions.Fail = new List<QuestCondition>();

                    quest08.Conditions.Fail.Insert(0, failCondition700);

                    quest08.Restartable = true;

                    modifiedCount++;
                }

                // Jiang Hu Bounty Edict
                if (quests.TryGetValue("e983002c4ab4d229af880000", out var quest00))
                {
                    if (quest00.Conditions?.AvailableForFinish != null)
                    {
                        foreach (var condition in quest00.Conditions.AvailableForFinish)
                        {
                            if (condition.OneSessionOnly == true)
                            {
                                condition.OneSessionOnly = false;
                            }
                        }
                    }

                    if (quest00.Conditions?.AvailableForFinish != null)
                    {
                        quest00.Conditions.AvailableForFinish.RemoveAll(condition =>
                            condition.Counter?.Conditions?.Any(counterCondition =>
                                counterCondition.ConditionType == "ExitStatus") == true
                        );
                    }

                    var failCondition2b2 = new QuestCondition
                    {
                        Id = new MongoId("e983002c4ab4d229af880030"),
                        Index = 0,
                        ConditionType = "CounterCreator",
                        DynamicLocale = false,
                        GlobalQuestCounterId = string.Empty,
                        ParentId = string.Empty,
                        OneSessionOnly = false,
                        Value = 1,
                        Type = "Exploration",
                        VisibilityConditions = new List<VisibilityCondition>(),
                        Counter = new QuestConditionCounter
                        {
                            Id = "e983002c4ab4d229af880031",
                            Conditions = new List<QuestConditionCounterCondition>
                    {
                        new QuestConditionCounterCondition
                        {
                            Id = new MongoId("e983002c4ab4d229af880032"),
                            ConditionType = "ExitStatus",
                            DynamicLocale = false,
                            Status = new List<string> { "Killed", "MissingInAction", "Left" }
                        }
                    }
                        }
                    };

                    if (quest00.Conditions.Fail == null)
                        quest00.Conditions.Fail = new List<QuestCondition>();

                    quest00.Conditions.Fail.Insert(0, failCondition2b2);
                    
                    quest00.Restartable = true;

                    modifiedCount++;
                }

                if (modifiedCount > 0)
                    Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Replaced one-raid requirement with one-life for {modifiedCount} new quests    一次战局完成的新任务降为一命完成\x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error replacing one-raid with one-life: {ex.Message} \x1b[0m");
            }
        }

        // Raid Time
        private void SetRaidTimeLength(DatabaseTables tables)
        {
            try
            {
                if (_Default_Match_Time)
                {
                    return;
                }

                if (_DeathMatch_Match_Time <= 0)
                {
                    return;
                }

                var locations = tables.Locations;
                int modifiedCount = 0;

                var locationsDict = locations.GetDictionary();

                foreach (var kvp in locationsDict)
                {
                    var location = kvp.Value;

                    location.Base.EscapeTimeLimit = (double) _DeathMatch_Match_Time;

                    if (location.Base.EscapeTimeLimitCoop.HasValue)
                        location.Base.EscapeTimeLimitCoop = _DeathMatch_Match_Time;

                    if (location.Base.EscapeTimeLimitPVE.HasValue)
                        location.Base.EscapeTimeLimitPVE = _DeathMatch_Match_Time;

                    modifiedCount++;
                }

                Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Set all map raid time to {_DeathMatch_Match_Time} minutes for {modifiedCount} locations    设置对局时间\x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error setting raid time: {ex.Message}\x1b[0m");
            }
        }
    }
}
