using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Quests;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;

namespace JiangHu.Server;

[Injectable]
public class NewQuestModule
{
    private readonly DatabaseService _databaseService;
    private readonly CustomQuestService _customQuestService;
    private readonly ModHelper _modHelper;
    private readonly string _modPath;
    private bool _questsLoaded = false;
    private bool _Enable_New_Quest = false;

    public NewQuestModule(DatabaseService databaseService, CustomQuestService customQuestService, ModHelper modHelper)
    {
        _databaseService = databaseService;
        _customQuestService = customQuestService;
        _modHelper = modHelper;
        _modPath = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        LoadConfig();
    }

    public void SetupJiangHuQuests()
    {
        if (!_Enable_New_Quest)
        {
            return;
        }

        var quests = LoadQuestsFromJson();
        foreach (var quest in quests) CreateQuest(quest);
        _questsLoaded = true;
        Console.WriteLine($"\x1b[90m♻️ [Jiang Hu] Core Modules New Quest Loaded    基础构件：新任务\x1b[0m");
    }

    private void LoadConfig()
    {
        try
        {
            var modPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var configPath = System.IO.Path.Combine(modPath, "config", "config.json");

            if (!System.IO.File.Exists(configPath))
            {
                Console.WriteLine("⚠️ [New Quest] config.json not found!");
                return;
            }

            var json = System.IO.File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (config != null && config.TryGetValue("Enable_New_Quest", out var questValue))
            {
                _Enable_New_Quest = questValue.GetBoolean();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [New Quest] Error loading config: {ex.Message}");
        }
    }


    private List<Quest> LoadQuestsFromJson()
    {
        var questDict = _modHelper.GetJsonDataFromFile<Dictionary<string, Quest>>(_modPath, "db/quest/JiangHu_quest.json");
        var quests = new List<Quest>();
        foreach (var kvp in questDict)
        {
            var quest = kvp.Value;
            if (string.IsNullOrEmpty(quest.Id)) quest.Id = kvp.Key;
            if (string.IsNullOrEmpty(quest.TemplateId)) quest.TemplateId = quest.Id;
            quests.Add(quest);
        }
        return quests;
    }

    private void CreateQuest(Quest quest)
    {
        var newQuestDetails = new NewQuestDetails
        {
            NewQuest = quest,
            Locales = LoadQuestLocales(quest.Id)
        };
        _customQuestService.CreateQuest(newQuestDetails);
    }

    private Dictionary<string, Dictionary<string, string>> LoadQuestLocales(MongoId questId)
    {
        var locales = new Dictionary<string, Dictionary<string, string>>();
        var localesPath = System.IO.Path.Combine(_modPath, "db", "locales");
        var localeFiles = System.IO.Directory.GetFiles(localesPath, "*.json");
        foreach (var localeFile in localeFiles)
        {
            var languageCode = System.IO.Path.GetFileNameWithoutExtension(localeFile);
            var localeContent = System.IO.File.ReadAllText(localeFile);
            var localeData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(localeContent);
            var questLocales = new Dictionary<string, string>();
            foreach (var kvp in localeData)
                if (kvp.Key?.Contains(questId) == true)
                    questLocales[kvp.Key] = kvp.Value;
            if (questLocales.Count > 0)
                locales[languageCode] = questLocales;
        }
        return locales;
    }
}
