using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using JiangHu.Server;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.CodeWrapper;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Image;
using SPTarkov.Server.Core.Utils.Json;

namespace JiangHu.Server;

[Injectable(TypePriority = 400010)]
public class JiangHuMod : IOnLoad
{
    private readonly DatabaseService _databaseService;
    private readonly ImageRouterService _imageRouterService;
    private readonly DatabaseServer _databaseServer;
    private readonly SaveServer _saveServer;
    private readonly NewTrader _traderService;
    private readonly NewItemModule _newItemModule;
    private readonly NewQuestModule _newQuestModule;
    private readonly RuleSettings _RuleSettings;
    private readonly Preset _Preset;
    private readonly QuestGenerator _questGenerator;
    private readonly NewDialogueModule _newDialogueModule;
    private readonly EnableJianghuBot _enableJianghuBot;
    private readonly JianghuBotName _jianghuBotName;
    private readonly MovementServerSide _movementServerSide;
    private readonly newbotXP _newbotXP;


    public JiangHuMod(
        DatabaseService databaseService,
        ImageRouterService imageRouterService,
        DatabaseServer databaseServer,
        SaveServer saveServer,
        NewTrader traderService,
        NewItemModule newItemModule,
        NewQuestModule newQuestModule,
        NewDialogueModule newDialogueModule,
        RuleSettings RuleSettings,
        Preset Preset,
        QuestGenerator questGenerator,
        EnableJianghuBot enableJianghuBot,
        JianghuBotName jianghuBotName,
        MovementServerSide movementServerSide,
        newbotXP newbotXP)
    {
        _databaseService = databaseService;
        _imageRouterService = imageRouterService;
        _databaseServer = databaseServer;
        _saveServer = saveServer;
        _traderService = traderService;
        _newItemModule = newItemModule;
        _newQuestModule = newQuestModule;
        _newDialogueModule = newDialogueModule;

        _RuleSettings = RuleSettings;
        _Preset = Preset;
        _questGenerator = questGenerator;
        _enableJianghuBot = enableJianghuBot;
        _jianghuBotName = jianghuBotName;
        _movementServerSide = movementServerSide;
        _newbotXP = newbotXP;
    }

    public async Task OnLoad()
    {
        RouteImages();
        LoadLocales();

        _traderService.SetupJiangHuTrader();
        _newQuestModule.SetupJiangHuQuests();
        _newDialogueModule.SetupJiangHuDialogues();
        _newItemModule.OnLoad();
   
        _enableJianghuBot.ApplyBotSettings();
        _jianghuBotName.SetupJianghuBotNames();
        _movementServerSide.ApplyAllSettings();

        
        await _saveServer.LoadAsync();
        await _RuleSettings.ApplySettings();
        _Preset.ApplyPreset();
        _questGenerator.GenerateQuestChain();
        _newbotXP.ApplyAllRaidModeSettings();
        await _saveServer.SaveAsync();
        Log.PrintBanner();
        await Task.CompletedTask;
    }
    private void RouteImages()
    {
        var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(modPath)) return;

        void RegisterImages(string folderName, string routePrefix)
        {
            var path = System.IO.Path.Combine(modPath, "db", folderName);
            if (!Directory.Exists(path)) return;

            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".bmp", ".gif"
            };

            var imageFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var ext = System.IO.Path.GetExtension(file);
                    return !string.IsNullOrEmpty(ext) && extensions.Contains(ext);
                });

            foreach (var imageFile in imageFiles)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(imageFile);
                if (string.IsNullOrEmpty(fileName)) continue;

                var key = $"/files/{routePrefix}/{fileName}".ToLowerInvariant();
                _imageRouterService.AddRoute(key, imageFile);
            }
        }

        RegisterImages("trader", "trader/avatar");
        RegisterImages("quest", "quest/icon");
    }

    private void LoadLocales()
    {
        var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var localesPath = Path.Combine(modPath, "db", "locales");

        if (!Directory.Exists(localesPath))
            return;

        var databaseTables = _databaseServer.GetTables();
        var localeBase = databaseTables.Locales;
        var localeFiles = Directory.GetFiles(localesPath, "*.json");

        foreach (var localeFile in localeFiles)
        {
            var languageCode = Path.GetFileNameWithoutExtension(localeFile);
            if (!localeBase.Global.ContainsKey(languageCode))
                continue;

            try
            {
                var localeContent = File.ReadAllText(localeFile);
                var localeData = JsonSerializer.Deserialize<Dictionary<string, string>>(localeContent);

                localeBase.Global[languageCode].AddTransformer(existingDict =>
                {
                    var mergedDict = new Dictionary<string, string>(existingDict);
                    foreach (var kvp in localeData)
                    {
                        mergedDict[kvp.Key] = kvp.Value; 
                    }
                    return mergedDict;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load locale file {localeFile}: {ex.Message}");
            }
        }
    }
}
