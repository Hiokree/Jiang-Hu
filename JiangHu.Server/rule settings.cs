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
        private string ConfigPath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "config", "config.json");

        // Config flags
        private bool _Disable_Vanilla_Quests = false;
        private bool _Increase_HeadHP = false;
        private bool _Lock_Flea = false;
        private bool _Enable_No_Insurance = false;
        private bool _Enable_Empty_Vanilla_Shop = false;
        private bool _Unlock_AllItems_By_NewQuest = false;
        private bool _Change_Prestige_Conditions = false;
        private bool _Add_HideoutProduction_DSP = false;
        private bool _Add_HideoutProduction_Labryskeycard = false;
        private bool _Unlock_All_Labrys_Quests = false;

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

                if (config.TryGetValue("Disable_Vanilla_Quests", out var questValue))
                    _Disable_Vanilla_Quests = questValue.GetBoolean();

                if (config.TryGetValue("Increase_HeadHP", out var headValue))
                    _Increase_HeadHP = headValue.GetBoolean();

                if (config.TryGetValue("Lock_Flea", out var fleaValue))
                    _Lock_Flea = fleaValue.GetBoolean();

                if (config.TryGetValue("Enable_No_Insurance", out var insuranceValue))
                    _Enable_No_Insurance = insuranceValue.GetBoolean();

                if (config.TryGetValue("Enable_empty_vanilla_shop", out var emptyShopValue))
                    _Enable_Empty_Vanilla_Shop = emptyShopValue.GetBoolean();

                if (config.TryGetValue("Unlock_AllItems_By_NewQuest", out var unlockValue))
                    _Unlock_AllItems_By_NewQuest = unlockValue.GetBoolean();

                if (config.TryGetValue("Change_Prestige_Conditions", out var prestigeValue))
                    _Change_Prestige_Conditions = prestigeValue.GetBoolean();

                if (config.TryGetValue("Add_HideoutProduction_DSP", out var dspValue))
                    _Add_HideoutProduction_DSP = dspValue.GetBoolean();

                if (config.TryGetValue("Add_HideoutProduction_Labryskeycard", out var labKeycardValue))
                    _Add_HideoutProduction_Labryskeycard = labKeycardValue.GetBoolean();

                if (config.TryGetValue("Unlock_All_Labrys_Quests", out var labrysQuestsValue))
                    _Unlock_All_Labrys_Quests = labrysQuestsValue.GetBoolean();

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

                if (_Disable_Vanilla_Quests)
                    DisableVanillaQuests(tables);

                if (_Lock_Flea)
                    LockFlea(tables);

                if (_Increase_HeadHP)
                    IncreaseHeadHP();

                if (_Enable_No_Insurance)
                    DisableInsurance(tables);

                if (_Enable_Empty_Vanilla_Shop)
                    DisableVanillaShops(tables);

                if (_Unlock_AllItems_By_NewQuest)
                    UnlockAllItemsByNewQuest(tables);

                if (_Change_Prestige_Conditions)
                    Change_Prestige_Conditions(tables);

                if (_Add_HideoutProduction_DSP)
                    AddHideoutProductionDSP(tables);

                if (_Add_HideoutProduction_Labryskeycard)
                    AddHideoutProductionLabKeycard(tables);

                if (_Unlock_All_Labrys_Quests)
                    UnlockLabrysQuests(tables);


                Console.WriteLine("\x1b[36m🎮 [Jiang Hu] All rules applied successfully\x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error applying settings: {ex.Message}  \x1b[0m");
            }

            return Task.CompletedTask;
        }

        // 🔹 Disable Vanilla Quests
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
            "656f0f98d80a697f855d34b1", // Lightkeeper
            "638f541a29ffd1183d187f57" // BTR Driver
        };

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

                quest.Conditions.AvailableForStart = new List<QuestCondition> { condition }; // REPLACES all conditions
                modifiedCount++;
            }

            Console.WriteLine($"\x1b[36m⚙️ [Jiang Hu] Disabled {modifiedCount} vanilla quests and changed {traderChangedCount} trader IDs \x1b[0m");
        }


        // 🔹 Lock Flea
        private void LockFlea(DatabaseTables tables)
        {
            var globals = tables.Globals;
            var ragfair = globals.Configuration.RagFair;

            if (_Lock_Flea)
            {
                ragfair.MinUserLevel = 99;
                Console.WriteLine("\x1b[36m🔒 [Jiang Hu] Flea Market locked \x1b[0m");

            }
            else
            {
                ragfair.MinUserLevel = 1;
                Console.WriteLine("\x1b[36m🟢 [Jiang Hu] Flea Market unlocked \x1b[0m");
            }
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
                    int bonusHp = Math.Min(exp / 30000, 65); 
                    int newMax = Math.Min(35 + bonusHp, 100);

                    if (pmc.Health?.BodyParts == null || !pmc.Health.BodyParts.ContainsKey("Head"))
                        continue;

                    var head = pmc.Health.BodyParts["Head"];

                    head.Health.Maximum = newMax;
                    if (head.Health.Current > newMax)
                        head.Health.Current = newMax;
                    Console.WriteLine($"\x1b[36m🧠 [Jiang Hu] Increased {pmc.Info.Nickname}'s Head HP to {newMax} (+{newMax - 35}) \x1b[0m");
                    modifiedCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error adjusting head HP: {ex.Message}  \x1b[0m");
            }
        }

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
                Console.WriteLine($"\x1b[36m🖐️ [Jiang Hu] Insurance disabled for traders: {string.Join(", ", disabledTraders)}  \x1b[0m");
            }
        }

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
                Console.WriteLine($"\x1b[36m🧩 [Jiang Hu] Vanilla shops cleared \x1b[0m");
        }

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

                Console.WriteLine($"\x1b[36m🛡️ [Jiang Hu] Fence disabled \x1b[0m");
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

        private void UnlockAllItemsByNewQuest(DatabaseTables tables)
        {
            try
            {
                int modifiedTraders = 0;

                foreach (var traderEntry in tables.Traders)
                {
                    var trader = traderEntry.Value;
                    if (trader.QuestAssort == null)
                        continue;

                    bool traderModified = false;

                    foreach (var status in new[] { "success", "started", "fail" })
                    {
                        if (!trader.QuestAssort.ContainsKey(status))
                            continue;

                        var questItems = trader.QuestAssort[status];
                        foreach (var key in questItems.Keys.ToList())
                        {
                            questItems[key] = "e983002c4ab4d229af8896a0"; 
                            traderModified = true;
                        }
                    }

                    if (traderModified)
                        modifiedTraders++;
                }
                Console.WriteLine($"\x1b[36m📌 [Jiang Hu] Items unlock requirement changed \x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error unlocking items: {ex.Message} \x1b[0m");
            }
        }

        // Add converter class
        public class MongoIdJsonConverter : JsonConverter<MongoId>
        {
            public override MongoId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string? idStr = reader.GetString();
                if (string.IsNullOrEmpty(idStr))
                    throw new JsonException("MongoId string is null or empty");
                return new MongoId(idStr);
            }

            public override void Write(Utf8JsonWriter writer, MongoId value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }
        public class ListOrTJsonConverter<T> : JsonConverter<ListOrT<T>>
        {
            public override ListOrT<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    var list = JsonSerializer.Deserialize<List<T>>(ref reader, options);
                    return new ListOrT<T>(list, default);
                }
                else
                {
                    var item = JsonSerializer.Deserialize<T>(ref reader, options);
                    return new ListOrT<T>(null, item);
                }
            }

            public override void Write(Utf8JsonWriter writer, ListOrT<T> value, JsonSerializerOptions options)
            {
                if (value.IsList)
                    JsonSerializer.Serialize(writer, value.List, options);
                else
                    JsonSerializer.Serialize(writer, value.Item, options);
            }
        }

        private void Change_Prestige_Conditions(DatabaseTables tables)
        {
            try
            {
                if (!_Change_Prestige_Conditions)
                {
                    Console.WriteLine("ℹ️ [Jiang Hu] Change_Prestige_Conditions is false. Skipping prestige conditions replacement.");
                    return;
                }

                var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var prestigeFile = System.IO.Path.Combine(modPath, "db", "prestige", "prestige.json");

                if (!File.Exists(prestigeFile))
                {
                    Console.WriteLine("⚠️ [Jiang Hu] prestige.json not found. Skipping prestige conditions replacement.");
                    return;
                }

                string prestigeJson = File.ReadAllText(prestigeFile);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new MongoIdJsonConverter());

                var customPrestige = JsonSerializer.Deserialize<Prestige>(prestigeJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters =
    {
        new MongoIdJsonConverter(),
        new ListOrTJsonConverter<string>()
    }
                });

                if (customPrestige?.Elements == null || customPrestige.Elements.Count == 0)
                {
                    Console.WriteLine("⚠️ [Jiang Hu] prestige.json is empty or invalid. Skipping prestige conditions replacement.");
                    return;
                }

                var originalPrestige = tables.Templates.Prestige;
                if (originalPrestige?.Elements == null || originalPrestige.Elements.Count == 0)
                {
                    Console.WriteLine("⚠️ [Jiang Hu] No original prestige elements found.");
                    return;
                }

                for (int i = 0; i < originalPrestige.Elements.Count; i++)
                {
                    var element = originalPrestige.Elements[i];

                    if (i < customPrestige.Elements.Count)
                    {
                        element.Conditions = customPrestige.Elements[i].Conditions;
                    }
                    else
                    {
                        element.Conditions.Clear();
                    }
                }
                Console.WriteLine($"\x1b[36m🛠️ [Jiang Hu] Prestige conditions replaced for {originalPrestige.Elements.Count} levels \x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error replacing prestige conditions: {ex.Message} \x1b[0m");
            }
        }
        private void AddHideoutProductionDSP(DatabaseTables tables)
        {
            try
            {
                if (!_Add_HideoutProduction_DSP)
                    return;

                var dspItem = tables.Templates.Items["62e910aaf957f2915e0a5e36"];
                if (dspItem == null)
                {
                    Console.WriteLine("⚠️ [Jiang Hu] DSP item not found in templates");
                    return;
                }

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
                }
            },
                    ProductionTime = 60,
                    EndProduct = new MongoId("62e910aaf957f2915e0a5e36"),
                    IsEncoded = true,
                    Locked = false,
                    NeedFuelForAllProductionTime = true,
                    Continuous = false,
                    Count = 1,
                    ProductionLimitCount = 0,
                    IsCodeProduction = false
                };

                tables.Hideout.Production.Recipes.Add(newRecipe);
                Console.WriteLine($"\x1b[36m🎁 [Jiang Hu] DSP production recipe added \x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error adding hideout production: {ex.Message} \x1b[0m");
            }
        }

        private void AddHideoutProductionLabKeycard(DatabaseTables tables)
        {
            try
            {
                if (!_Add_HideoutProduction_Labryskeycard)
                    return;

                var keycardItem = tables.Templates.Items["679b9819a2f2dd4da9023512"];
                if (keycardItem == null)
                {
                    Console.WriteLine("⚠️ [Jiang Hu] Lab keycard item not found in templates");
                    return;
                }

                var newRecipe = new HideoutProduction
                {
                    Id = new MongoId("e983002c4ab4d99999888201"),
                    AreaType = (HideoutAreas) 11,
                    Requirements = new List<Requirement>
            {
                new Requirement
                {
                    TemplateId = new MongoId("5734758f24597738025ee253"), // Golden neck chain
                    Type = "Item",
                    IsSpawnedInSession = true,
                    Count = 1
                },
                new Requirement
                {
                    TemplateId = new MongoId("5c12688486f77426843c7d32"), // Paracord
                    Type = "Item",
                    IsSpawnedInSession = true,
                    Count = 1
                },
                new Requirement
                {
                    TemplateId = new MongoId("61bf83814088ec1a363d7097"), // Sewing kit
                    Type = "Item",
                    IsSpawnedInSession = true,
                    Count = 1
                },
                new Requirement
                {
                    TemplateId = new MongoId("59e3556c86f7741776641ac2"), // Ox bleach
                    Type = "Item",
                    IsSpawnedInSession = true,
                    Count = 1
                },
                new Requirement
                {
                    TemplateId = new MongoId("59e361e886f774176c10a2a5"), // Bottle of hydrogen peroxide
                    Type = "Item",
                    IsSpawnedInSession = true,
                    Count = 1
                },
                new Requirement
                {
                    TemplateId = new MongoId("5b4335ba86f7744d2837a264"), // Medical bloodset
                    Type = "Item",
                    IsSpawnedInSession = true,
                    Count = 1
                },
                new Requirement
                {
                    TemplateId = new MongoId("590a3d9c86f774385926e510"), // Ultraviolet lamp
                    Type = "Item",
                    IsSpawnedInSession = true,
                    Count = 1
                },
                new Requirement
                {
                    TemplateId = new MongoId("5d1b3f2d86f774253763b735"), // Disposable syringe
                    Type = "Item",
                    IsSpawnedInSession = true,
                    Count = 1
                }
            },
                    ProductionTime = 600,
                    EndProduct = new MongoId("679b9819a2f2dd4da9023512"),
                    IsEncoded = false,
                    Locked = false,
                    NeedFuelForAllProductionTime = false,
                    Continuous = false,
                    Count = 1,
                    ProductionLimitCount = 0,
                    IsCodeProduction = false
                };

                tables.Hideout.Production.Recipes.Add(newRecipe);
                Console.WriteLine("\x1b[36m🎁 [Jiang Hu] Lab keycard production added \x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error adding Lab keycard production: {ex.Message} \x1b[0m");
            }
        }

        /// Unlock Labrys Quests
        private void UnlockLabrysQuests(DatabaseTables tables)
        {
            try
            {
                const string targetLocation = "6733700029c367a3d40b02af";
                const string newTraderId = "e983002c4ab4d99999888000";
                const string targetQuestId = "e983002c4ab4d229af888480"; // "Big Tarkov Family"

                var quests = tables.Templates.Quests;
                var matchingQuests = new List<(string QuestId, SPTarkov.Server.Core.Models.Eft.Common.Tables.Quest Quest)>();

                foreach (var kvp in quests)
                {
                    string questId = kvp.Key.ToString();
                    var quest = kvp.Value;

                    if (quest.Location == targetLocation)
                    {
                        matchingQuests.Add((questId, quest));
                    }
                }

                foreach (var (questId, quest) in matchingQuests)
                {
                    string questName = quest.Name ?? "Unnamed Quest";

                    string oldTraderId = quest.TraderId;
                    quest.TraderId = newTraderId;

                    quest.Conditions.AvailableForStart?.Clear();
                    quest.Conditions.AvailableForStart = new List<QuestCondition>();

                    var condition = new QuestCondition
                    {
                        Id = new MongoId(Guid.NewGuid().ToString("N").Substring(0, 24)),
                        Index = 0,
                        ConditionType = "Quest",
                        Status = new HashSet<SPTarkov.Server.Core.Models.Enums.QuestStatusEnum>
                {
                    SPTarkov.Server.Core.Models.Enums.QuestStatusEnum.Success
                },
                        Target = new ListOrT<string>(list: null, item: targetQuestId),
                        DynamicLocale = false,
                        GlobalQuestCounterId = string.Empty,
                        ParentId = string.Empty,
                        VisibilityConditions = new List<VisibilityCondition>()
                    };

                    quest.Conditions.AvailableForStart.Add(condition);
                }
                Console.WriteLine($"\x1b[36m🔓 [Jiang Hu] Unlocked {matchingQuests.Count} Labrys quests \x1b[0m");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error in UnlockLabrysQuests: {ex.Message}\x1b[0m");
            }
        }

    }
}
