using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Services;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace JiangHu.Server;

[Injectable]
public class NewTrader
{
    private readonly DatabaseService _databaseService;
    private readonly ConfigServer _configServer;
    private readonly ModHelper _modHelper;
    private readonly string _modPath;
    private bool _Enable_New_Trader = false;
    private const string TraderId = "e983002c4ab4d99999888000";

    public NewTrader(DatabaseService databaseService, ConfigServer configServer, ModHelper modHelper)
    {
        _databaseService = databaseService;
        _configServer = configServer;
        _modHelper = modHelper;
        _modPath = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        LoadConfig();
    }

    public void SetupJiangHuTrader()
    {
        if (!_Enable_New_Trader)
        {
            return;
        }

        var traderBase = LoadTraderBase();
        var traderAssort = LoadTraderAssort();
        InitializeTraderData(traderBase, traderAssort);
        AddTraderToDatabase(traderBase, traderAssort);
        SetTraderRefreshTime(TraderId, 72000, 72000);
        Console.WriteLine($"\x1b[90m♻️ [Jiang Hu] Core Modules New Trader Loaded    基础构件：新商人 \x1b[0m");
    }

    private void LoadConfig()
    {
        try
        {
            var modPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var configPath = System.IO.Path.Combine(modPath, "config", "config.json");

            if (!System.IO.File.Exists(configPath))
            {
                Console.WriteLine("⚠️ [New Trader] config.json not found!");
                return;
            }

            var json = System.IO.File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (config != null && config.TryGetValue("Enable_New_Trader", out var traderValue))
            {
                _Enable_New_Trader = traderValue.GetBoolean();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [New Trader] Error loading config: {ex.Message}");
        }
    }


    private void InitializeTraderData(TraderBase traderBase, TraderAssort traderAssort)
    {
        traderBase.LoyaltyLevels ??= new List<TraderLoyaltyLevel>();
        traderBase.SellCategory ??= new List<string>();
        traderAssort.Items ??= new List<Item>();
        traderAssort.BarterScheme ??= new Dictionary<MongoId, List<List<BarterScheme>>>();
    }

    private TraderBase? LoadTraderBase()
    {
        return _modHelper.GetJsonDataFromFile<TraderBase>(_modPath, "db/trader/base.json");
    }

    private TraderAssort? LoadTraderAssort()
    {
        return _modHelper.GetJsonDataFromFile<TraderAssort>(_modPath, "db/trader/assort.json");
    }

    private void AddTraderToDatabase(TraderBase traderBase, TraderAssort traderAssort)
    {
        var trader = new Trader
        {
            Base = traderBase,
            Assort = traderAssort,
            Dialogue = new Dictionary<string, List<string>?>(),
            QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>(),
            Suits = new List<Suit>(),
            Services = new List<TraderServiceModel>()
        };

        var traders = _databaseService.GetTraders();

        if (traders.ContainsKey(traderBase.Id))
        {
            traders[traderBase.Id] = trader;
        }
        else
        {
            traders.Add(traderBase.Id, trader);
        }
    }

    private void SetTraderRefreshTime(string traderId, int minSeconds, int maxSeconds)
    {
        var traderConfig = _configServer.GetConfig<TraderConfig>();
        traderConfig.UpdateTime.RemoveAll(x => x.TraderId == traderId);

        traderConfig.UpdateTime.Add(new UpdateTime
        {
            Name = "JiangHu",
            TraderId = traderId,
            Seconds = new MinMax<int>
            {
                Min = minSeconds,
                Max = maxSeconds
            }
        });
    }
}
