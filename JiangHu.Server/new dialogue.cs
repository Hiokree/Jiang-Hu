using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Templates;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;

namespace JiangHu.Server;

[Injectable]
public class NewDialogueModule
{
    private readonly DatabaseService _databaseService;
    private readonly ModHelper _modHelper;
    private readonly string _modPath;
    private bool _Enable_New_Quest = false;

    public NewDialogueModule(DatabaseService databaseService, ModHelper modHelper)
    {
        _databaseService = databaseService;
        _modHelper = modHelper;
        _modPath = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        LoadConfig();
    }

    public void SetupJiangHuDialogues()
    {
        if (!_Enable_New_Quest)
        {
            return;
        }

        var newDialogues = LoadDialoguesFromJson();
        AddDialoguesToTemplates(newDialogues);
        Console.WriteLine($"\x1b[90m♻️ [Jiang Hu] Core Modules New Dialogue Loaded    基础构件：新任务对话\x1b[0m");
    }

    private void LoadConfig()
    {
        try
        {
            var modPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var configPath = System.IO.Path.Combine(modPath, "config", "config.json");

            if (!System.IO.File.Exists(configPath))
            {
                Console.WriteLine("⚠️ [New Dialogue] config.json not found!");
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
            Console.WriteLine($"❌ [New Dialogue] Error loading config: {ex.Message}");
        }
    }


    private TraderDialogs LoadDialoguesFromJson()
    {
        return _modHelper.GetJsonDataFromFile<TraderDialogs>(_modPath, "db/dialogue/dialogue.json");
    }

    private void AddDialoguesToTemplates(TraderDialogs newDialogues)
    {
        var templates = _databaseService.GetTemplates();

        if (templates.Dialogue?.Elements != null && newDialogues?.Elements != null)
        {
            templates.Dialogue.Elements.AddRange(newDialogues.Elements);
        }
    }


}
