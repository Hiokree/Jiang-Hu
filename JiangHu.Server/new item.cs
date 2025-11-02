using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
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
    private readonly FixedCustomItemService _CustomItemService;
    private readonly DatabaseService _databaseService;
    private readonly ConfigServer _configServer;

    public NewItemModule(FixedCustomItemService CustomItemService, DatabaseService databaseService, ConfigServer configServer)
    {
        _CustomItemService = CustomItemService;
        _databaseService = databaseService;
        _configServer = configServer;
    }

    private Dictionary<string, LocaleDetails> GetLocales()
    {
        return new Dictionary<string, LocaleDetails>
        {
            ["en"] = new LocaleDetails
            {
                Name = "1",
                ShortName = "1",
                Description = "1"
            }
        };
    }

    public async Task OnLoad()
    {
        var existingItems = _databaseService.GetTables().Templates.Items;
        if (existingItems.ContainsKey("e983002c4ab4d99999889000"))  // Mixue
        {
            Console.WriteLine("[JiangHu] Mixue exists - skipping ALL item creation");
            return;
        }

        DefineCustomBuffs();
        CreateMixueItem();
        CreateHaidilaoItem();
        CreateBaiyaoItem();
        CreateRepairKitItem();
        CreateMaotaiItem();
        AddItemsToFleaBlacklist();

        await Task.CompletedTask;
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
            Locales = GetLocales(),
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
            Locales = GetLocales(),
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
            Locales = GetLocales(),
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
            Locales = GetLocales(),
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
            Locales = GetLocales(),
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
            "e983002c4ab4d99999889004"
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
