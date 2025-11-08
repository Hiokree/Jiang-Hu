using System;
using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace JiangHu.Server;

[Injectable]
public class JianghuBotName
{
    private readonly DatabaseService _databaseService;
    private readonly bool _enabled;

    public JianghuBotName(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _enabled = LoadConfig();
    }

    public void SetupJianghuBotNames()
    {
        if (!_enabled) return;

        try
        {
            var customNames = LoadCustomNames();
            if (!customNames.Any())
            {
                return;
            }

            var botTypes = _databaseService.GetBots().Types;
            var bossTypes = GetBossTypes();
            var replacedCount = 0;

            foreach (var (botTypeId, botType) in botTypes)
            {
                if (bossTypes.Contains(botTypeId)) continue;

                botType.FirstNames = customNames;
                replacedCount++;
            }

            Console.WriteLine($"\x1b[36m🤖 [Jiang Hu] {customNames.Count} Jianghu Bot names applied for {replacedCount} bot types \x1b[0m");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JiangHu] Error replacing bot names: {ex.Message}");
        }
    }

    private bool LoadConfig()
    {
        try
        {
            var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var configPath = System.IO.Path.Combine(modPath, "config", "config.json");
            if (System.IO.File.Exists(configPath))
            {
                var jsonContent = System.IO.File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);
                return config != null && config.TryGetValue("Enable_Jianghu_BotName", out var enableValue) && enableValue.GetBoolean();
            }
        }
        catch { }
        return false;
    }

    private List<string> LoadCustomNames()
    {
        var allNames = new List<string>();

        try
        {
            var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dbPath = System.IO.Path.Combine(modPath, "db", "bot name");
            var config = LoadConfigDictionary();

            var languageFiles = new Dictionary<string, string>
        {
            { "BotName_ch.json", "Enable_Jianghu_BotName_ch" },
            { "BotName_en.json", "Enable_Jianghu_BotName_en" },
            { "BotName_es.json", "Enable_Jianghu_BotName_es" },
            { "BotName_fr.json", "Enable_Jianghu_BotName_fr" },
            { "BotName_jp.json", "Enable_Jianghu_BotName_jp" },
            { "BotName_po.json", "Enable_Jianghu_BotName_po" },
            { "BotName_ru.json", "Enable_Jianghu_BotName_ru" }
        };

            foreach (var (fileName, configKey) in languageFiles)
            {
                if (config.TryGetValue(configKey, out var enabled) && enabled.GetBoolean())
                {
                    var filePath = System.IO.Path.Combine(dbPath, fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        var jsonContent = System.IO.File.ReadAllText(filePath);
                        var names = JsonSerializer.Deserialize<List<string>>(jsonContent) ?? new List<string>();
                        allNames.AddRange(names);
                    }
                }
            }

            if (allNames.Any())
            {
                allNames = allNames.Distinct().ToList();
                allNames = allNames.OrderBy(x => Random.Shared.Next()).ToList();
            }
            else
            {
                Console.WriteLine($"[JiangHu] No names loaded - check config settings and file paths");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JiangHu] Error loading name files: {ex.Message}");
        }

        return allNames;
    }

    private Dictionary<string, JsonElement> LoadConfigDictionary()
    {
        try
        {
            var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var configPath = System.IO.Path.Combine(modPath, "config", "config.json");
            if (System.IO.File.Exists(configPath))
            {
                var jsonContent = System.IO.File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent) ?? new Dictionary<string, JsonElement>();
            }
        }
        catch { }
        return new Dictionary<string, JsonElement>();
    }

    private HashSet<string> GetBossTypes()
    {
        return new HashSet<string>
        {
            "bosskilla", "bossbully", "bossknight", "bosskojaniy", "bosssanitar",
            "bosstagilla", "bossgluhar", "bosszryachiy", "bossboar", "bossboarsniper",
            "bosskolontay", "bosspartisan", "followerbigpipe", "followerbirdeye",
            "bosstagillaagro", "bosskillaagro", "tagillahelperagro"
        };
    }
}
