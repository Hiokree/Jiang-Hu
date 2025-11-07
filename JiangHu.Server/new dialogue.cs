using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

    public NewDialogueModule(DatabaseService databaseService, ModHelper modHelper)
    {
        _databaseService = databaseService;
        _modHelper = modHelper;
        _modPath = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    }

    public void SetupJiangHuDialogues()
    {
        var newDialogues = LoadDialoguesFromJson();
        AddDialoguesToTemplates(newDialogues);
        Console.WriteLine($"\x1b[36m🎭 [Jiang Hu] Loaded {newDialogues.Elements.Count} dialogues successfully \x1b[0m");
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
