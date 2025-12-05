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
    private bool _Enable_JiangHu_quest = false;
    private bool _Enable_Arena_Quest = false;
    private bool _Enable_Dogtag_Collection = false;
    private bool _Enable_Quest_Generator = false;

    public NewQuestModule(DatabaseService databaseService, CustomQuestService customQuestService, ModHelper modHelper)
    {
        _databaseService = databaseService;
        _customQuestService = customQuestService;
        _modHelper = modHelper;
        _modPath = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        
    }

    public void SetupJiangHuQuests()
    {
        LoadConfig();

        var loadedQuests = new Dictionary<string, int>();

        if (_Enable_JiangHu_quest)
        {
            var count = LoadQuestFolder("Main_Quest");
            if (count > 0) loadedQuests["main"] = count;
        }

        if (_Enable_Arena_Quest)
        {
            var count = LoadQuestFile("Arena_Quest.json");
            if (count > 0) loadedQuests["arena"] = count;
        }

        if (_Enable_Dogtag_Collection)
        {
            var count = LoadQuestFile("Dogtag_Collection_Quest.json");
            if (count > 0) loadedQuests["Dogtag_Collection"] = count;
        }

        if (_Enable_Quest_Generator)
        {
            var count = LoadQuestFile("Random_Quest.json");
        }

        if (loadedQuests.Count > 0)
        {
            _questsLoaded = true;
            var logParts = new List<string>();
            foreach (var kvp in loadedQuests)
            {
                logParts.Add($"{kvp.Value} {kvp.Key} quests");
            }
            Console.WriteLine($"\x1b[90m♻️ [Jiang Hu] Core Modules New Quests: {string.Join(", ", logParts)} loaded    基础构件：新任务\x1b[0m");
        }
    }

    private void LoadConfig()
    {
        try
        {
            var configPath = System.IO.Path.Combine(_modPath, "config", "config.json");
            if (!System.IO.File.Exists(configPath))
            {
                Console.WriteLine("⚠️ [New Quest] config.json not found!");
                return;
            }

            var json = System.IO.File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (config != null)
            {
                if (config.TryGetValue("Enable_Main_Quest", out var mainQuestValue))
                    _Enable_JiangHu_quest = mainQuestValue.GetBoolean();

                if (config.TryGetValue("Enable_Arena_Quest", out var arenaValue))
                    _Enable_Arena_Quest = arenaValue.GetBoolean();

                if (config.TryGetValue("Enable_Dogtag_Collection", out var Dogtag_CollectionValue))
                    _Enable_Dogtag_Collection = Dogtag_CollectionValue.GetBoolean();

                if (config.TryGetValue("Enable_Quest_Generator", out var generatorValue))
                    _Enable_Quest_Generator = generatorValue.GetBoolean();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [New Quest] Error loading config: {ex.Message}");
        }
    }


    private int LoadQuestFile(string fileName)
    {
        var quests = LoadQuestsFromJson(fileName);
        foreach (var quest in quests)
            CreateQuest(quest);
        return quests.Count;
    }

    private int LoadQuestFolder(string folderName)
    {
        var folderPath = System.IO.Path.Combine(_modPath, "db", "quest", folderName);
        if (!System.IO.Directory.Exists(folderPath))
            return 0;

        var jsonFiles = System.IO.Directory.GetFiles(folderPath, "*.json");
        int totalQuests = 0;

        foreach (var jsonFile in jsonFiles)
        {
            var fileName = System.IO.Path.GetFileName(jsonFile);
            var count = LoadQuestFile(System.IO.Path.Combine(folderName, fileName));
            totalQuests += count;
        }

        return totalQuests;
    }

    private List<Quest> LoadQuestsFromJson(string fileName)
    {
        var questDict = _modHelper.GetJsonDataFromFile<Dictionary<string, Quest>>(_modPath, $"db/quest/{fileName}");
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
        try
        {
            var newQuestDetails = new NewQuestDetails
            {
                NewQuest = quest,
                Locales = LoadQuestLocales(quest.Id)
            };

            _customQuestService.CreateQuest(newQuestDetails);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Debug] Failed to create quest {quest.Id}: {ex.Message}");
        }
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
