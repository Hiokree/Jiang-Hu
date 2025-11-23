using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace JiangHu.Server
{
    public static class Log
    {


        private static bool LoadGreetingConfig()
        {
            try
            {
                var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configPath = Path.Combine(modPath, "config", "config.json");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine("⚠️ [Greeting Log] config.json not found!");
                    return true; 
                }

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (config != null && config.TryGetValue("Enable_Greeting_Log", out var greetingValue))
                {
                    return greetingValue.GetBoolean();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Greeting Log] Error loading config: {ex.Message}");
            }

            return true; 
        }

        private static readonly string[] Colors =
        {
            "\x1b[38;2;0;255;0m",
            "\x1b[38;2;255;255;0m",
            "\x1b[38;2;255;0;255m",
            "\x1b[38;2;120;255;120m",
            "\x1b[38;2;255;255;120m",
            "\x1b[38;2;255;120;255m"
        };

        private static readonly string[] Bars = { "***", ":::" };
        private static readonly string[] Styles = { "\x1b[1m", "\x1b[5m" };
        private static readonly string Reset = "\x1b[0m";

        private static readonly Random _rand = new();

        private static string Rand(string[] arr) => arr.Length > 0 ? arr[_rand.Next(arr.Length)] : string.Empty;
        private static string Rand(string[] arr, string fallback) => arr.Length > 0 ? arr[_rand.Next(arr.Length)] : fallback;

        private static string GetGameLanguage()
        {
            var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            var sptSettingsPath = Path.GetFullPath(Path.Combine(modPath, "..", "..", "sptsettings", "Game.ini"));

            if (File.Exists(sptSettingsPath))
            {
                var content = File.ReadAllText(sptSettingsPath);
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.TryGetProperty("Language", out var languageElement))
                {
                    return languageElement.GetString()?.ToLowerInvariant() ?? "ch";
                }
            }

            return "ch";
        }

        private static string GetLocalizedFilePath(string fileName)
        {
            var modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            var gameLanguage = GetGameLanguage();

            if (gameLanguage != "ch")
            {
                var gameLanguagePath = Path.Combine(modPath, "db", "logs", gameLanguage, fileName);
                if (File.Exists(gameLanguagePath))
                    return gameLanguagePath;
            }

            return Path.Combine(modPath, "db", "logs", "ch", fileName);
        }

        public static void PrintBanner()
        {
            if (!LoadGreetingConfig())
            {
                return;
            }

            var greetingsFile = GetLocalizedFilePath("greeting.json");
            var loadingFile = GetLocalizedFilePath("loading.json");

            string[] greetings = JsonSerializer.Deserialize<string[]>(File.ReadAllText(greetingsFile));
            string[] loading = JsonSerializer.Deserialize<string[]>(File.ReadAllText(loadingFile));

            var msg = Rand(greetings);
            var msg1 = Rand(loading);
            var color = Rand(Colors, "\x1b[37m");
            var msgColor = Rand(Colors, "\x1b[37m");
            var style = Rand(Styles, "");
            var style2 = Rand(Styles, "");

            var top = new string(' ', 120);
            var middle = $"{color}{style}{msg1}{Reset}   {style2}{msgColor}...{msg}{Reset}";
            var bottom = new string(' ', 120);

            Console.WriteLine(top);
            Console.WriteLine(middle);
            Console.WriteLine(bottom);
        }
    }
}
