using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils.Cloners;

namespace JiangHu.Server;

[Injectable]
public class NewItemModule
{
    private readonly CustomItemService _CustomItemService;
    private readonly DatabaseService _databaseService;
    private readonly ConfigServer _configServer;
    private bool _Enable_New_Item = false;
    private bool _Enable_Dogtag_Collection = false;

    public NewItemModule(CustomItemService CustomItemService, DatabaseService databaseService, ConfigServer configServer)
    {
        _CustomItemService = CustomItemService;
        _databaseService = databaseService;
        _configServer = configServer;
        LoadConfig();
    }

    public void OnLoad()
    {
        var existingItems = _databaseService.GetTables().Templates.Items;
        if (existingItems.ContainsKey("e983002c4ab4d99999889000"))  // Mixue
        {
            Console.WriteLine("[JiangHu] Mixue exists - skipping ALL item creation");
            return;
        }

        DefineCustomBuffs();


        if (_Enable_New_Item)
        {
            CreateMixueItem();
            CreateHaidilaoItem();
            CreateBaiyaoItem();
            CreateRepairKitItem();
            CreateMaotaiItem();
            CreateArmorRepairCat();
            CreateWeaponRepairCat();

            Console.WriteLine($"\x1b[90m♻️ [Jiang Hu] Core Modules New Items Loaded    基础构件：新物品 \x1b[0m");
        }

        if (_Enable_Dogtag_Collection) 
        {
            CreateCosmosCasket01();
            CreateCosmosCasket02();
            CreateCosmosCasket03();
            Console.WriteLine($"\x1b[33m🎁 [Jiang Hu] Three Body Mode enabled    三体模式 \u001b[0m");
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
                Console.WriteLine("⚠️ [New Item] config.json not found!");
                return;
            }

            var json = System.IO.File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (config != null)
            {
                if (config.TryGetValue("Enable_New_Item", out var itemValue))
                    _Enable_New_Item = itemValue.GetBoolean();
                if (config.TryGetValue("Enable_Dogtag_Collection", out var dogtagValue)) // Add this
                    _Enable_Dogtag_Collection = dogtagValue.GetBoolean();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [New Item] Error loading config: {ex.Message}");
        }
    }

    private void DefineCustomBuffs()
    {
        var tables = _databaseService.GetTables();
        if (tables?.Globals?.Configuration?.Health?.Effects?.Stimulator?.Buffs == null)
            return; 

        var buffs = tables.Globals.Configuration.Health.Effects.Stimulator.Buffs;

        buffs["mixue_buffs"] = new List<Buff>
        {
            new() { AbsoluteValue = true, BuffType = "HydrationRate", Chance = 1, Delay = 0, Duration = 300, SkillName = "", Value = 0.2 },
            new() { AbsoluteValue = true, BuffType = "EnergyRate", Chance = 1, Delay = 0, Duration = 300, SkillName = "", Value = 0.1 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Endurance.ToString(), Value = 1 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Strength.ToString(), Value = 1 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Vitality.ToString(), Value = 1 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Health.ToString(), Value = 1 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.StressResistance.ToString(), Value = 1 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Immunity.ToString(), Value = 1 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Intellect.ToString(), Value = 1 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Perception.ToString(), Value = 1 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Attention.ToString(), Value = 1 }
        };

        buffs["haidilao_buffs"] = new List<Buff>
        {
            new() { AbsoluteValue = false, BuffType = "WeightLimit", Chance = 1, Delay = 0, Duration = 900, SkillName = "", Value = 0.5 },
            new() { AbsoluteValue = true, BuffType = "MaxStamina", Chance = 1, Delay = 0, Duration = 900, SkillName = "", Value = 30 },
            new() { AbsoluteValue = true, BuffType = "StaminaRate", Chance = 1, Delay = 0, Duration = 900, SkillName = "", Value = 2 }
        };

        buffs["baiyao_buffs"] = new List<Buff>
        {
            new() { AbsoluteValue = true, BuffType = "RemoveAllBloodLosses", Chance = 1, Delay = 1, Duration = 180, SkillName = "", Value = 0 },
            new() { AbsoluteValue = true, BuffType = "HealthRate", Chance = 1, Delay = 1, Duration = 60, SkillName = "", Value = 6 },
            new() { AbsoluteValue = true, BuffType = "Antidote", Chance = 1, Delay = 1, Duration = 180, SkillName = "", Value = 0 },
            new() { AbsoluteValue = true, BuffType = "BodyTemperature", Chance = 1, Delay = 1, Duration = 180, SkillName = "", Value = -6 }
        };

        buffs["maotai_buffs"] = new List<Buff>
        {
            new() { AbsoluteValue = false, BuffType = "DamageModifier", Chance = 1, Delay = 0, Duration = 300, SkillName = "", Value = -0.3 },
            new() { AbsoluteValue = true, BuffType = "MaxStamina", Chance = 1, Delay = 1, Duration = 300, SkillName = "", Value = 15 },
            new() { AbsoluteValue = true, BuffType = "StaminaRate", Chance = 1, Delay = 1, Duration = 300, SkillName = "", Value = 1 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Endurance.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Strength.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Vitality.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Health.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.StressResistance.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Perception.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Throwing.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Assault.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.SMG.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.DMR.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Sniper.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.AimDrills.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.MagDrills.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.CovertMovement.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "SkillRate", Chance = 1, Delay = 1, Duration = 300, SkillName = SkillTypes.Surgery.ToString(), Value = 20 },
            new() { AbsoluteValue = true, BuffType = "HydrationRate", Chance = 1, Delay = 300, Duration = 60, SkillName = "", Value = -0.5 },
            new() { AbsoluteValue = true, BuffType = "EnergyRate", Chance = 1, Delay = 300, Duration = 60, SkillName = "", Value = -0.5 },
            new() { AbsoluteValue = true, BuffType = "HandsTremor", Chance = 1, Delay = 300, Duration = 60, SkillName = "", Value = 0 },
            new() { AbsoluteValue = true, BuffType = "QuantumTunnelling", Chance = 1, Delay = 300, Duration = 60, SkillName = "", Value = 0 },
            new() { AbsoluteValue = true, BuffType = "BodyTemperature", Chance = 1, Delay = 300, Duration = 60, SkillName = "", Value = 6 },
            new() { AbsoluteValue = false, BuffType = "DamageModifier", Chance = 1, Delay = 300, Duration = 60, SkillName = "", Value = 1 },
            new() { AbsoluteValue = false, BuffType = "WeightLimit", Chance = 1, Delay = 300, Duration = 60, SkillName = "", Value = -0.5 }
        };
    }

    private void CreateMixueItem()
    {
        NewItemFromCloneDetails newItemFromCloneDetails = new NewItemFromCloneDetails
        {
            ItemTplToClone = "60098b1705871270cd5352a1",
            NewId = "e983002c4ab4d99999889000",
            ParentId = "5448e8d64bdc2dce718b4568",
            FleaPriceRoubles = 35000,
            HandbookPriceRoubles = 30000,
            HandbookParentId = "5b47574386f77428ca22b335",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "",
                    ShortName = "",
                    Description = ""
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab
                {
                    Path = "assets/content/weapons/usable_items/item_drink_waterration/item_drink_waterration_loot.bundle",
                    Rcid = ""
                },
                DiscardLimit = -1,
                Weight = 0.2,
                BackgroundColor = "tracerGreen",
                Height = 1,
                Width = 1,
                ItemSound = "food_bottle",
                FoodUseTime = 2,
                MaxResource = 1,
                CanRequireOnRagfair = false,
                StimulatorBuffs = "mixue_buffs"
            }
        };

        _CustomItemService.CreateItemFromClone(newItemFromCloneDetails);
    }

    private void CreateHaidilaoItem()
    {
        NewItemFromCloneDetails newItemFromCloneDetails = new NewItemFromCloneDetails
        {
            ItemTplToClone = "5bc9c29cd4351e003562b8a3",
            NewId = "e983002c4ab4d99999889001",
            ParentId = "5448e8d04bdc2ddf718b4569",
            FleaPriceRoubles = 60000,
            HandbookPriceRoubles = 50000,
            HandbookParentId = "5b47574386f77428ca22b336",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "",
                    ShortName = "",
                    Description = ""
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab
                {
                    Path = "assets/content/items/food/item_food_sprats/item_food_sprats.bundle",
                    Rcid = ""
                },
                DiscardLimit = -1,
                Weight = 0.2,
                BackgroundColor = "tracerGreen",
                Height = 1,
                Width = 1,
                ItemSound = "food_tin_can",
                FoodUseTime = 3,
                MaxResource = 1,
                CanRequireOnRagfair = false,
                StimulatorBuffs = "haidilao_buffs"
            }
        };

        _CustomItemService.CreateItemFromClone(newItemFromCloneDetails);
    }

    private void CreateBaiyaoItem()
    {
        NewItemFromCloneDetails newItemFromCloneDetails = new NewItemFromCloneDetails
        {
            ItemTplToClone = "5af0548586f7743a532b7e99",
            NewId = "e983002c4ab4d99999889002",
            ParentId = "5448f3a14bdc2d27728b4569",
            FleaPriceRoubles = 180000,
            HandbookPriceRoubles = 150000,
            HandbookParentId = "5b47574386f77428ca22b337",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "1",
                    ShortName = "1",
                    Description = "1"
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab
                {
                    Path = "assets/content/weapons/usable_items/item_ibuprofen/item_ibuprofen_loot.bundle",
                    Rcid = ""
                },
                DiscardLimit = -1,
                Weight = 0.05,
                BackgroundColor = "tracerGreen",
                Height = 1,
                Width = 1,
                ItemSound = "med_pills",
                MedUseTime = 3,
                MaxHpResource = 3,
                CanRequireOnRagfair = false,
                StimulatorBuffs = "baiyao_buffs"
            }
        };

        _CustomItemService.CreateItemFromClone(newItemFromCloneDetails);
    }

    private void CreateRepairKitItem()
    {
        NewItemFromCloneDetails newItemFromCloneDetails = new NewItemFromCloneDetails
        {
            ItemTplToClone = "60098af40accd37ef2175f27",
            NewId = "e983002c4ab4d99999889003",
            ParentId = "5448f3ac4bdc2dce718b4569",
            FleaPriceRoubles = 150000,
            HandbookPriceRoubles = 100000,
            HandbookParentId = "5b47574386f77428ca22b339",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "1",
                    ShortName = "1",
                    Description = "1"
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab
                {
                    Path = "assets/content/weapons/usable_items/item_meds_cat/item_meds_cat_loot.bundle",
                    Rcid = ""
                },
                DiscardLimit = -1,
                Weight = 0.2,
                BackgroundColor = "tracerGreen",
                Height = 1,
                Width = 1,
                ItemSound = "med_medkit",
                MedUseTime = 6,
                MaxHpResource = 10,
                HpResourceRate = 0,
                CanRequireOnRagfair = false
            }
        };

        _CustomItemService.CreateItemFromClone(newItemFromCloneDetails);
    }

    private void CreateMaotaiItem()
    {
        NewItemFromCloneDetails newItemFromCloneDetails = new NewItemFromCloneDetails
        {
            ItemTplToClone = "5ed515c8d380ab312177c0fa",
            NewId = "e983002c4ab4d99999889004",
            ParentId = "5448f3a64bdc2d60728b456a",
            FleaPriceRoubles = 100000,
            HandbookPriceRoubles = 80000,
            HandbookParentId = "5b47574386f77428ca22b33a",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "1",
                    ShortName = "1",
                    Description = "1"
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab
                {
                    Path = "assets/content/weapons/usable_items/item_syringe/item_stimulator_obdolbos2_loot.bundle",
                    Rcid = ""
                },
                DiscardLimit = -1,
                Weight = 0.05,
                BackgroundColor = "tracerGreen",
                Height = 1,
                Width = 1,
                ItemSound = "med_stimulator",
                MedUseTime = 2,
                MaxHpResource = 0,
                CanRequireOnRagfair = false,
                StimulatorBuffs = "maotai_buffs"
            }
        };

        _CustomItemService.CreateItemFromClone(newItemFromCloneDetails);
    }

    private void CreateArmorRepairCat()
    {
        NewItemFromCloneDetails armorRepairCat = new NewItemFromCloneDetails
        {
            ItemTplToClone = "591094e086f7747caa7bb2ef",
            NewId = "e983002c4ab4d99999889005",
            ParentId = "616eb7aea207f41933308f46",
            FleaPriceRoubles = 100000,
            HandbookPriceRoubles = 80000,
            HandbookParentId = "5b47574386f77428ca22b33a",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "1",
                    ShortName = "1",
                    Description = "1"
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab
                {
                    Path = "assets/content/items/barter/item_barter_mr_kerman/item_barter_mr_kerman.bundle",
                    Rcid = ""
                },
                DiscardLimit = -1,
                Weight = 2,
                BackgroundColor = "tracerGreen",
                Height = 2,
                Width = 1,
                ItemSound = "spec_armorrep",
                MaxRepairResource = 50,
                RepairCost = 1,
                RepairQuality = 1
            }
        };

        _CustomItemService.CreateItemFromClone(armorRepairCat);

        _databaseService.GetTables().Templates.Items["e983002c4ab4d99999889005"].Properties.RepairType = "Armor";
    }

    private void CreateWeaponRepairCat()
    {
        NewItemFromCloneDetails weaponRepairCat = new NewItemFromCloneDetails
        {
            ItemTplToClone = "5910968f86f77425cf569c32",
            NewId = "e983002c4ab4d99999889006",
            ParentId = "616eb7aea207f41933308f46",
            FleaPriceRoubles = 100000,
            HandbookPriceRoubles = 80000,
            HandbookParentId = "5b47574386f77428ca22b33a",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "1",
                    ShortName = "1",
                    Description = "1"
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab
                {
                    Path = "assets/content/items/barter/item_barter_mr_kerman/item_barter_mr_kerman.bundle",
                    Rcid = ""
                },
                DiscardLimit = -1,
                Weight = 2,
                BackgroundColor = "tracerYellow",
                Height = 2,
                Width = 1,
                ItemSound = "spec_weaprep",
                MaxRepairResource = 50,
                RepairCost = 1,
                RepairQuality = 1
            }
        };

        _CustomItemService.CreateItemFromClone(weaponRepairCat);
        _databaseService.GetTables().Templates.Items["e983002c4ab4d99999889006"].Properties.RepairType = "Firearms";
    }


    private void CreateCosmosCasket01()
    {
        NewItemFromCloneDetails cosmosCasket = new NewItemFromCloneDetails
        {
            ItemTplToClone = "5c093e3486f77430cb02e593",
            NewId = "e983002c4ab4d99999889007",
            ParentId = "5795f317245977243854e041",
            FleaPriceRoubles = 15000000,
            HandbookPriceRoubles = 12000000,
            HandbookParentId = "5b47574386f77428ca22b33a",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "1",
                    ShortName = "1",
                    Description = "1"
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab
                {
                    Path = "assets/content/items/barter/item_barter_valuable_nyball/item_barter_valuable_nyball_violet.bundle",
                    Rcid = ""
                },
                DiscardLimit = -1,
                Weight = 0.01,
                BackgroundColor = "tracerRed",
                Height = 1,
                Width = 1,
                ItemSound = "item_container_plastic",
                Grids = new List<Grid>
                {
                    new Grid
                    {
                        Name = "main",
                        Id = "e983002c4ab4d99999889007_main",
                        Parent = "e983002c4ab4d99999889007",
                        Properties = new GridProperties
                        {
                            CellsH = 3,
                            CellsV = 3,
                            Filters = new List<GridFilter>
                            {
                                new GridFilter
                                {
                                    Filter = new HashSet<MongoId>
                                    {
                                        new("54009119af1c881c07000029")
                                    }
                                }
                            },
                            MaxCount = 0,
                            MaxWeight = 0,
                            MinCount = 0,
                            IsSortingTable = false
                        }
                    }
                }
            }
        };

        _CustomItemService.CreateItemFromClone(cosmosCasket);

        AddCosmosCasketToSecureContainers();
        AddCosmosCasketToPocketSpecialSlots();
    }

    private void CreateCosmosCasket02()
    {
        NewItemFromCloneDetails cosmosCasket = new NewItemFromCloneDetails
        {
            ItemTplToClone = "5c093e3486f77430cb02e593",
            NewId = "e983002c4ab4d99999889008",
            ParentId = "5795f317245977243854e041",
            FleaPriceRoubles = 45000000,
            HandbookPriceRoubles = 36000000,
            HandbookParentId = "5b47574386f77428ca22b33a",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "1",
                    ShortName = "1",
                    Description = "1"
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab
                {
                    Path = "assets/content/items/barter/item_barter_valuable_nyball/item_barter_valuable_nyball_silver.bundle",
                    Rcid = ""
                },
                DiscardLimit = -1,
                Weight = 0.01,
                BackgroundColor = "tracerRed",
                Height = 1,
                Width = 1,
                ItemSound = "item_container_plastic",
                Grids = new List<Grid>
                {
                    new Grid
                    {
                        Name = "main",
                        Id = "e983002c4ab4d99999889007_main",
                        Parent = "e983002c4ab4d99999889007",
                        Properties = new GridProperties
                        {
                            CellsH = 5,
                            CellsV = 5,
                            Filters = new List<GridFilter>
                            {
                                new GridFilter
                                {
                                    Filter = new HashSet<MongoId>
                                    {
                                        new("54009119af1c881c07000029")
                                    }
                                }
                            },
                            MaxCount = 0,
                            MaxWeight = 0,
                            MinCount = 0,
                            IsSortingTable = false
                        }
                    }
                }
            }
        };

        _CustomItemService.CreateItemFromClone(cosmosCasket);

        AddCosmosCasketToSecureContainers();
        AddCosmosCasketToPocketSpecialSlots();
    }

    private void CreateCosmosCasket03()
    {
        NewItemFromCloneDetails cosmosCasket = new NewItemFromCloneDetails
        {
            ItemTplToClone = "5c093e3486f77430cb02e593",
            NewId = "e983002c4ab4d99999889009",
            ParentId = "5795f317245977243854e041",
            FleaPriceRoubles = 150000000,
            HandbookPriceRoubles = 120000000,
            HandbookParentId = "5b47574386f77428ca22b33a",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "1",
                    ShortName = "1",
                    Description = "1"
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                Prefab = new Prefab
                {
                    Path = "assets/content/items/barter/item_barter_valuable_nyball/item_barter_valuable_nyball_red.bundle",
                    Rcid = ""
                },
                DiscardLimit = -1,
                Weight = 0.01,
                BackgroundColor = "tracerRed",
                Height = 1,
                Width = 1,
                ItemSound = "item_container_plastic",
                Grids = new List<Grid>
                {
                    new Grid
                    {
                        Name = "main",
                        Id = "e983002c4ab4d99999889007_main",
                        Parent = "e983002c4ab4d99999889007",
                        Properties = new GridProperties
                        {
                            CellsH = 10,
                            CellsV = 10,
                            Filters = new List<GridFilter>
                            {
                                new GridFilter
                                {
                                    Filter = new HashSet<MongoId>
                                    {
                                        new("54009119af1c881c07000029")
                                    }
                                }
                            },
                            MaxCount = 0,
                            MaxWeight = 0,
                            MinCount = 0,
                            IsSortingTable = false
                        }
                    }
                }
            }
        };

        _CustomItemService.CreateItemFromClone(cosmosCasket);

        AddCosmosCasketToSecureContainers();
        AddCosmosCasketToPocketSpecialSlots();
    }

    private void AddCosmosCasketToSecureContainers()
    {
        var items = _databaseService.GetTables().Templates.Items;
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

        var cosmosCasketIds = new[]
        {
            new MongoId("e983002c4ab4d99999889007"),
            new MongoId("e983002c4ab4d99999889008"),
            new MongoId("e983002c4ab4d99999889009")
        };

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

            var allowed = filterList[0].Filter ?? new HashSet<MongoId>();
            foreach (var cosmosCasketId in cosmosCasketIds)
            {
                allowed.Add(cosmosCasketId);
            }
            filterList[0].Filter = allowed;
            grids[0].Properties.Filters = filterList;

            container.Properties.Grids = grids;
        }
    }

    private void AddCosmosCasketToPocketSpecialSlots()
    {
        var items = _databaseService.GetTables().Templates.Items;
        var cosmosCasketIds = new[]
        {
            new MongoId("e983002c4ab4d99999889007"),
            new MongoId("e983002c4ab4d99999889008"),
            new MongoId("e983002c4ab4d99999889009")
        };

        var specialPocket = items.GetValueOrDefault("627a4e6b255f7527fb05a0f6");
        var tuePocket = items.GetValueOrDefault("65e080be269cbd5c5005e529");

        if (specialPocket?.Properties?.Slots != null)
        {
            foreach (var slot in specialPocket.Properties.Slots)
            {
                if (slot?.Properties?.Filters != null)
                {
                    foreach (var filter in slot.Properties.Filters)
                    {
                        foreach (var cosmosCasketId in cosmosCasketIds)
                        {
                            filter.Filter?.Add(cosmosCasketId);
                        }
                    }
                }
            }
        }

        if (tuePocket?.Properties?.Slots != null)
        {
            foreach (var slot in tuePocket.Properties.Slots)
            {
                if (slot?.Properties?.Filters != null)
                {
                    foreach (var filter in slot.Properties.Filters)
                    {
                        foreach (var cosmosCasketId in cosmosCasketIds)
                        {
                            filter.Filter?.Add(cosmosCasketId);
                        }
                    }
                }
            }
        }
    }

    private void AddItemsToFleaBlacklist()
    {
        var ragfairConfig = _configServer.GetConfig<RagfairConfig>();
        var blacklist = ragfairConfig.Dynamic.Blacklist.Custom;

        var itemIds = new[]
        {
            "e983002c4ab4d99999889000",
            "e983002c4ab4d99999889001",
            "e983002c4ab4d99999889002",
            "e983002c4ab4d99999889003",
            "e983002c4ab4d99999889004",
            "e983002c4ab4d99999889005",
            "e983002c4ab4d99999889006",
            "e983002c4ab4d99999889007",
            "e983002c4ab4d99999889008",
            "e983002c4ab4d99999889009"
        };

        foreach (var itemId in itemIds)
        {
            if (!blacklist.Contains(itemId))
            {
                blacklist.Add(itemId);
            }
        }
    }
}
