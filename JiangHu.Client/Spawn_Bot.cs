using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.Game.Spawning;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static JiangHu.F12Manager;

namespace JiangHu
{
    public class JiangHuBotMarker : MonoBehaviour
    {
        public HostilityType HostilityType { get; set; }
    }

    public class UniversalBotSpawner : MonoBehaviour
    {
        private ConfigEntry<KeyboardShortcut> _spawnHotkey;
        private ConfigEntry<KeyboardShortcut> _killHotkey;
        private ConfigEntry<HostilityType> _hostilityConfig;
        private Dictionary<WildSpawnType, ConfigEntry<bool>> _botTypeConfigs;

        private bool _isSpawning = false;
        private float _lastSpawnTime = 0f;
        private float _spawnCooldown = 5f;

        private List<WildSpawnType> _botQueue = new List<WildSpawnType>();
        private int _botQueueIndex = 0;

        public static UniversalBotSpawner Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            LoadCooldownConfig();
        }

        private void LoadCooldownConfig()
        {
            try
            {
                string configPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Application.dataPath),
                    "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                if (System.IO.File.Exists(configPath))
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    var config = Newtonsoft.Json.Linq.JObject.Parse(json);
                    _spawnCooldown = config["Spawn_Cooldown"]?.Value<float>() ?? 5f;
                }
            }
            catch { _spawnCooldown = 5f; }
        }

        void Update()
        {
            if (!_isSpawning && Time.time - _lastSpawnTime >= _spawnCooldown)
            {
                if (_spawnHotkey?.Value.IsDown() == true)
                {
                    SpawnUniversalBot();
                }
                else if (_killHotkey?.Value.IsDown() == true)
                {
                    RemoveBotsBySettings();
                }
            }
        }

        public void Init(ConfigEntry<KeyboardShortcut> spawnHotkey,
                         ConfigEntry<KeyboardShortcut> killHotkey,
                         ConfigEntry<HostilityType> hostilityConfig,
                         Dictionary<WildSpawnType, ConfigEntry<bool>> botTypeConfigs)
        {
            _spawnHotkey = spawnHotkey;
            _killHotkey = killHotkey;
            _hostilityConfig = hostilityConfig;
            _botTypeConfigs = botTypeConfigs;
        }

        private void InitializeBotQueue()
        {
            _botQueue.Clear();

            foreach (var kvp in _botTypeConfigs)
            {
                if (kvp.Value.Value)
                {
                    _botQueue.Add(kvp.Key);
                }
            }

            if (_botQueue.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _botQueue.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, _botQueue.Count);
                var temp = _botQueue[i];
                _botQueue[i] = _botQueue[j];
                _botQueue[j] = temp;
            }

            _botQueueIndex = 0;
        }

        private async void SpawnUniversalBot()
        {
            _isSpawning = true;
            _lastSpawnTime = Time.time;

            var hostility = _hostilityConfig.Value;

            string startMessage = GetStartNotification(hostility);
            Color startColor = GetNotificationColor(hostility);

            NotificationManagerClass.DisplayMessageNotification(
                startMessage,
                ENotificationDurationType.Long,
                ENotificationIconType.Alert,
                startColor
            );

            try
            {
                var botGame = Singleton<IBotGame>.Instance;
                if (botGame?.BotsController == null) return;

                if (_botQueue == null || _botQueue.Count == 0 || _botQueueIndex >= _botQueue.Count)
                {
                    InitializeBotQueue();
                }

                InitializeBotQueue();

                if (_botQueue.Count == 0)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        "No bot types enabled in settings / 未选择任何人机类型",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Alert,
                        Color.yellow
                    );
                    return;
                }

                var nextBotType = _botQueue[_botQueueIndex];
                _botQueueIndex++;

                var spawnZone = botGame.BotsController.BotSpawner.GetRandomBotZone(false);
                if (spawnZone == null) throw new Exception("No spawn zones");

                var spawnParams = new BotSpawnParams()
                {
                    ShallBeGroup = new ShallBeGroupParams(false, true, 1),
                    Id_spawn = $"JiangHu_Bot|{Guid.NewGuid()}|{hostility}",
                };

                var profileData = new BotProfileDataClass(
                    EPlayerSide.Savage,
                    nextBotType,
                    BotDifficulty.hard,
                    0f,
                    spawnParams,
                    false
                );

                var creationData = await BotCreationDataClass.Create(
                    profileData,
                    botGame.BotsController.BotSpawner.BotCreator,
                    1,
                    botGame.BotsController.BotSpawner
                );

                if (creationData == null) throw new Exception("Creation data null");

                botGame.BotsController.BotSpawner.TryToSpawnInZoneAndDelay(
                    spawnZone,
                    creationData,
                    false,
                    true,
                    null,
                    true
                );

                string botName = GetBotDisplayName(nextBotType);
                string successMessage = GetSuccessNotification(hostility, botName);
                Color successColor = GetSuccessColor(hostility);

                NotificationManagerClass.DisplayMessageNotification(
                    successMessage,
                    ENotificationDurationType.Default,
                    GetSuccessIcon(hostility),
                    successColor
                );
            }
            catch (Exception ex)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    "Spawn failed, try again later / 召唤失败，请稍后重试",
                    ENotificationDurationType.Default,
                    ENotificationIconType.Alert,
                    Color.red
                );
            }
            finally
            {
                _isSpawning = false;
            }
        }



        private void RemoveBotsBySettings()
        {
            try
            {
                var botGame = Singleton<IBotGame>.Instance;
                if (botGame?.BotsController?.Bots?.BotOwners == null) return;

                var targetHostility = _hostilityConfig.Value;

                var enabledBotTypes = new List<WildSpawnType>();
                foreach (var kvp in _botTypeConfigs)
                {
                    if (kvp.Value.Value)
                    {
                        enabledBotTypes.Add(kvp.Key);
                    }
                }

                if (enabledBotTypes.Count == 0)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        "No bot types enabled in settings / 未选择任何人机类型",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Alert,
                        Color.yellow
                    );
                    return;
                }

                BotOwner targetBot = null;
                foreach (var bot in botGame.BotsController.Bots.BotOwners)
                {
                    if (bot == null || bot.IsDead) continue;

                    var botType = bot.Profile.Info.Settings.Role;
                    if (!enabledBotTypes.Contains(botType)) continue;

                    HostilityType botHostility;
                    var marker = bot.gameObject.GetComponent<JiangHuBotMarker>();
                    if (marker != null)
                    {
                        botHostility = marker.HostilityType;
                    }
                    else
                    {
                        var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                        bool isHostile = mainPlayer != null &&
                                         bot.BotsGroup?.Enemies?.ContainsKey(mainPlayer) == true;
                        botHostility = isHostile ? HostilityType.Enemy : HostilityType.Friendly;
                    }

                    if (botHostility != targetHostility) continue;

                    targetBot = bot;
                    break;
                }

                if (targetBot != null)
                {
                    targetBot.LeaveData.RemoveFromMap();
                    UnityEngine.Object.Destroy(targetBot.gameObject);
                    string botName = UniversalBotSpawner.GetBotDisplayName(targetBot.Profile.Info.Settings.Role);
                    NotificationManagerClass.DisplayMessageNotification(
                        $"Removed {botName} / 移除{botName}",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Default,
                        Color.red
                    );
                }
                else
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        "No matching bot found / 未找到匹配的机器人",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Alert,
                        Color.yellow
                    );
                }
            }
            catch (Exception ex)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    "Remove failed / 移除失败",
                    ENotificationDurationType.Default,
                    ENotificationIconType.Alert,
                    Color.red
                );
            }
        }

        private string GetStartNotification(HostilityType hostility)
        {
            switch (hostility)
            {
                case HostilityType.Friendly:
                    return "Start Spawning Teanmate / 开始召唤队友";
                case HostilityType.Enemy:
                    return "Start Spawning opponant / 开始召唤对手";
                case HostilityType.Neutral:
                    return "Start Spawning neutral / 开始召唤吃瓜群众";
                default:
                    return "Start Spawning vanilla bot / 开始召唤原生人机";
            }
        }

        private string GetSuccessNotification(HostilityType hostility, string botName)
        {
            switch (hostility)
            {
                case HostilityType.Friendly:
                    return $"Teanmate {botName} entering, try again if no show / 队友{botName}尝试入局，若未现身请重试";
                case HostilityType.Enemy:
                    return $"Opponant {botName} entering, try again if no show / 对手{botName}尝试入局，若未现身请重试";
                case HostilityType.Neutral:
                    return $"Neutral {botName} entering, try again if no show / 路人{botName}尝试入局，若未现身请重试";
                default:
                    return $"An vanilla {botName} entering / 一个{botName}尝试入局";
            }
        }

        private Color GetNotificationColor(HostilityType hostility)
        {
            Color notificationColor;
            switch (hostility)
            {
                case HostilityType.Friendly:
                    ColorUtility.TryParseHtmlString("#45b787", out notificationColor);
                    return notificationColor;
                case HostilityType.Enemy:
                    ColorUtility.TryParseHtmlString("#f43e06", out notificationColor);
                    return notificationColor;
                case HostilityType.Neutral:
                    ColorUtility.TryParseHtmlString("#808080", out notificationColor);
                    return notificationColor;
                default:
                    return Color.white;
            }
        }
        private Color GetSuccessColor(HostilityType hostility)
        {
            Color successColor;
            switch (hostility)
            {
                case HostilityType.Friendly:
                    ColorUtility.TryParseHtmlString("#45b787", out successColor);
                    return successColor;
                case HostilityType.Enemy:
                    ColorUtility.TryParseHtmlString("#f43e06", out successColor);
                    return successColor;
                case HostilityType.Neutral:
                    ColorUtility.TryParseHtmlString("#808080", out successColor);
                    return successColor;
                default:
                    return Color.white;
            }
        }

        private ENotificationIconType GetSuccessIcon(HostilityType hostility)
        {
            switch (hostility)
            {
                case HostilityType.Friendly: return ENotificationIconType.Friend;
                case HostilityType.Enemy: return ENotificationIconType.Alert;
                case HostilityType.Neutral: return ENotificationIconType.Default;
                default: return ENotificationIconType.Default;
            }
        }

        public static string GetBotDisplayName(WildSpawnType spawnType)
        {
            switch (spawnType)
            {
                // Regular bots
                case WildSpawnType.pmcBEAR: return "BEAR PMC";
                case WildSpawnType.pmcUSEC: return "USEC PMC";
                case WildSpawnType.marksman: return "Sniper Scav";
                case WildSpawnType.assault: return "Scav";
                case WildSpawnType.exUsec: return "Rogue";
                case WildSpawnType.pmcBot: return "Raider";
                case WildSpawnType.arenaFighterEvent: return "Smuggler";
                case WildSpawnType.sectantWarrior: return "Cultist Warrior";

                // Bosses
                case WildSpawnType.bossBully: return "Reshala";
                case WildSpawnType.bossGluhar: return "Gluhar";
                case WildSpawnType.bossKilla: return "Killa";
                case WildSpawnType.bossKojaniy: return "Shturman";
                case WildSpawnType.bossSanitar: return "Sanitar";
                case WildSpawnType.bossTagilla: return "Tagilla";
                case WildSpawnType.bossKnight: return "Knight";
                case WildSpawnType.bossZryachiy: return "Zryachiy";
                case WildSpawnType.bossBoar: return "Kaban";
                case WildSpawnType.bossKolontay: return "Kolontay";
                case WildSpawnType.bossPartisan: return "Partisan";
                case WildSpawnType.bossTagillaAgro: return "Tagilla (Aggro)";
                case WildSpawnType.bossKillaAgro: return "Killa (Aggro)";
                case WildSpawnType.followerBigPipe: return "Big Pipe";
                case WildSpawnType.followerBirdEye: return "Bird Eye";
                case WildSpawnType.sectantPriest: return "Cultist Priest";


                // Follower types
                case WildSpawnType.followerKojaniy: return "Shturman Follower";
                case WildSpawnType.followerSanitar: return "Sanitar Follower";
                case WildSpawnType.followerZryachiy: return "Zryachiy Follower";
                case WildSpawnType.followerBoar: return "Kaban Follower";
                case WildSpawnType.followerBoarClose1: return "Kaban Close Guard";
                case WildSpawnType.followerBoarClose2: return "Kaban Close Guard";
                case WildSpawnType.bossBoarSniper: return "Kaban Sniper Guard";
                case WildSpawnType.followerKolontayAssault: return "Kolontay Assault";
                case WildSpawnType.followerKolontaySecurity: return "Kolontay Security";
                case WildSpawnType.tagillaHelperAgro: return "Tagilla Helper";

                // Infected
                case WildSpawnType.infectedAssault: return "Infected Scav";
                case WildSpawnType.infectedPmc: return "Infected PMC";
                case WildSpawnType.infectedCivil: return "Infected Civilian";
                case WildSpawnType.infectedLaborant: return "Infected Lab Worker";
                case WildSpawnType.infectedTagilla: return "Infected Tagilla";

                // Special
                case WildSpawnType.crazyAssaultEvent: return "Crazy Scav";
                case WildSpawnType.peacefullZryachiyEvent: return "Peaceful Zryachiy";
                case WildSpawnType.sectactPriestEvent: return "Cultist Priest (Event)";
                case WildSpawnType.ravangeZryachiyEvent: return "Enraged Zryachiy";
                case WildSpawnType.shooterBTR: return "BTR Gunner";
                case WildSpawnType.gifter: return "Santa";
                case WildSpawnType.skier: return "Skier";
                case WildSpawnType.peacemaker: return "Peacekeeper";
                case WildSpawnType.cursedAssault: return "Cursed Scav";
                case WildSpawnType.sectantPredvestnik: return "Cultist Herald";
                case WildSpawnType.sectantPrizrak: return "Cultist Ghost";
                case WildSpawnType.sectantOni: return "Cultist Oni";

                default:
                    string name = spawnType.ToString();
                    name = name.Replace("boss", "").Replace("follower", "").Replace("sectant", "Cultist ");
                    return name;
            }
        }

        public static readonly WildSpawnType[] allBosses = new WildSpawnType[]
        {
            WildSpawnType.bossBully,
            WildSpawnType.bossGluhar,
            WildSpawnType.bossKilla,
            WildSpawnType.bossKojaniy,
            WildSpawnType.bossSanitar,
            WildSpawnType.bossTagilla,
            WildSpawnType.bossKnight,
            WildSpawnType.bossZryachiy,
            WildSpawnType.bossBoar,
            WildSpawnType.bossKolontay,
            WildSpawnType.bossPartisan,
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye,
            WildSpawnType.bossTagillaAgro,
            WildSpawnType.bossKillaAgro,
            WildSpawnType.sectantPriest
        };

        public static string GetBossDisplayName(WildSpawnType bossType)
        {
            switch (bossType)
            {
                case WildSpawnType.bossBully: return "Reshala";
                case WildSpawnType.bossGluhar: return "Gluhar";
                case WildSpawnType.bossKilla: return "Killa";
                case WildSpawnType.bossKojaniy: return "Shturman";
                case WildSpawnType.bossSanitar: return "Sanitar";
                case WildSpawnType.bossTagilla: return "Tagilla";
                case WildSpawnType.bossKnight: return "Knight";
                case WildSpawnType.bossZryachiy: return "Zryachiy";
                case WildSpawnType.bossBoar: return "Kaban";
                case WildSpawnType.bossKolontay: return "Kolontay";
                case WildSpawnType.bossPartisan: return "Partisan";
                case WildSpawnType.followerBigPipe: return "Big Pipe";
                case WildSpawnType.followerBirdEye: return "Bird Eye";
                case WildSpawnType.bossTagillaAgro: return "Tagilla (Aggro)";
                case WildSpawnType.bossKillaAgro: return "Killa (Aggro)";
                case WildSpawnType.sectantPriest: return "Cultist Priest";
                default: return bossType.ToString().Replace("boss", "").Replace("follower", "");
            }
        }
    }

    [HarmonyPatch(typeof(BotSpawner), "method_11")]
    class ProcessJiangHuBotPatch
    {
        static void Postfix(BotOwner bot, BotCreationDataClass data, Action<BotOwner> callback, bool shallBeGroup, Stopwatch stopWatch)
        {
            try
            {
                var spawnData = bot.SpawnProfileData as BotProfileDataClass;
                if (spawnData == null) return;

                // Handle UniversalSpawner bots
                if (spawnData.SpawnParams?.Id_spawn?.StartsWith("JiangHu_Bot|") == true)
                {
                    // Extract hostility from ID
                    string[] parts = spawnData.SpawnParams.Id_spawn.Split('|');
                    if (parts.Length >= 3 && Enum.TryParse(parts[2], out HostilityType hostility))
                    {
                        var marker = bot.gameObject.AddComponent<JiangHuBotMarker>();
                        marker.HostilityType = hostility;

                        // Teleport near player for friendly bots
                        if (hostility == HostilityType.Friendly)
                        {
                            var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                            if (mainPlayer != null)
                            {
                                var botGame = Singleton<IBotGame>.Instance;
                                float dist;
                                var playerZone = botGame?.BotsController?.GetClosestZone(mainPlayer.Position, out dist);
                                if (playerZone != null)
                                {
                                    var closestSpawn = playerZone.SpawnPointMarkers?
                                        .Where(m => m?.SpawnPoint != null)
                                        .OrderBy(m => (m.SpawnPoint.Position - mainPlayer.Position).sqrMagnitude)
                                        .FirstOrDefault()?.SpawnPoint;

                                    if (closestSpawn != null)
                                        bot.GetPlayer.Teleport(closestSpawn.Position, true);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(BotOwner), "method_10")]
    class ConfigureJiangHuBotHostility
    {
        static void Postfix(BotOwner __instance)
        {
            try
            {
                var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                if (mainPlayer == null) return;

                var botGame = Singleton<IBotGame>.Instance;
                if (botGame == null) return;

                // Get bot's spawn marker
                var spawnData = __instance.SpawnProfileData as BotProfileDataClass;
                string spawnId = spawnData?.SpawnParams?.Id_spawn;

                if (string.IsNullOrEmpty(spawnId)) return;

                // Check if DeathMatch bot
                bool isDeathMatchTeammate = spawnId.StartsWith("jianghu_deathmatch_teammate");
                bool isDeathMatchEnemy = spawnId.StartsWith("jianghu_deathmatch_enemy_team");

                // Check if UniversalSpawner bot
                var marker = __instance.gameObject.GetComponent<JiangHuBotMarker>();
                bool isUniversalBot = marker != null;

                if (isDeathMatchTeammate)
                {
                    // DEATHMATCH TEAMMATE LOGIC
                    // 1. Friendly to player
                    __instance.BotsGroup?.RemoveEnemy(mainPlayer);
                    if (mainPlayer.BotsGroup != null)
                        mainPlayer.BotsGroup.RemoveEnemy(__instance);
                    if (__instance.BotsGroup != null && !__instance.BotsGroup.Allies.Contains(mainPlayer))
                        __instance.BotsGroup.AddAlly(mainPlayer);

                    // 2. Friendly to other teammate bots
                    // 3. Friendly to JiangHu_Bot|friendly bots
                    // 4. Hostile to all other bots
                    foreach (var otherBot in botGame.BotsController.Bots.BotOwners)
                    {
                        if (otherBot == null || otherBot.Profile.Id == __instance.Profile.Id) continue;

                        var otherSpawnData = otherBot.SpawnProfileData as BotProfileDataClass;
                        string otherSpawnId = otherSpawnData?.SpawnParams?.Id_spawn;
                        var otherMarker = otherBot.gameObject.GetComponent<JiangHuBotMarker>();

                        bool isOtherTeammate = otherSpawnId?.StartsWith("jianghu_deathmatch_teammate") == true;
                        bool isOtherFriendly = otherMarker != null && otherMarker.HostilityType == HostilityType.Friendly;
                        bool isOtherUniversalFriendly = otherSpawnId?.StartsWith("JiangHu_Bot|") == true &&
                                                       otherSpawnId.Contains("|Friendly");

                        if (isOtherTeammate || isOtherFriendly || isOtherUniversalFriendly)
                        {
                            // Friendly to teammate, friendly bots, and JiangHu_Bot|friendly
                            __instance.BotsGroup?.RemoveEnemy(otherBot);
                            otherBot.BotsGroup?.RemoveEnemy(__instance);
                            if (__instance.BotsGroup != null && !__instance.BotsGroup.Allies.Contains(otherBot.GetPlayer))
                                __instance.BotsGroup.AddAlly(otherBot.GetPlayer);
                        }
                        else
                        {
                            // Hostile to everyone else
                            if (__instance.BotsGroup != null && !__instance.BotsGroup.Enemies.ContainsKey(otherBot))
                                __instance.BotsGroup.AddEnemy(otherBot, EBotEnemyCause.initial);
                        }
                    }
                    return;
                }

                if (isDeathMatchEnemy)
                {
                    // DEATHMATCH ENEMY TEAM LOGIC
                    string myTeamMarker = spawnId.Split('|')[0]; // Get "jianghu_deathmatch_enemy_teamX"

                    // 1. Hostile to player
                    if (__instance.BotsGroup != null && !__instance.BotsGroup.Enemies.ContainsKey(mainPlayer))
                        __instance.BotsGroup.AddEnemy(mainPlayer, EBotEnemyCause.initial);

                    // 2. Friendly to same team, hostile to other teams
                    foreach (var otherBot in botGame.BotsController.Bots.BotOwners)
                    {
                        if (otherBot == null || otherBot.Profile.Id == __instance.Profile.Id) continue;

                        var otherSpawnData = otherBot.SpawnProfileData as BotProfileDataClass;
                        string otherSpawnId = otherSpawnData?.SpawnParams?.Id_spawn;

                        if (string.IsNullOrEmpty(otherSpawnId)) continue;

                        if (otherSpawnId.StartsWith(myTeamMarker))
                        {
                            // Same team - friendly
                            __instance.BotsGroup?.RemoveEnemy(otherBot);
                            otherBot.BotsGroup?.RemoveEnemy(__instance);
                            if (__instance.BotsGroup != null && !__instance.BotsGroup.Allies.Contains(otherBot.GetPlayer))
                                __instance.BotsGroup.AddAlly(otherBot.GetPlayer);
                        }
                        else
                        {
                            // Different team or player bot - hostile
                            if (__instance.BotsGroup != null && !__instance.BotsGroup.Enemies.ContainsKey(otherBot))
                                __instance.BotsGroup.AddEnemy(otherBot, EBotEnemyCause.initial);
                        }
                    }
                    return;
                }

                if (isUniversalBot)
                {
                    // UNIVERSAL SPAWNER BOT LOGIC
                    switch (marker.HostilityType)
                    {
                        case HostilityType.Friendly:
                            __instance.BotsGroup?.RemoveEnemy(mainPlayer);
                            if (mainPlayer.BotsGroup != null)
                                mainPlayer.BotsGroup.RemoveEnemy(__instance);
                            if (__instance.BotsGroup != null && !__instance.BotsGroup.Allies.Contains(mainPlayer))
                                __instance.BotsGroup.AddAlly(mainPlayer);

                            foreach (var otherBot in botGame.BotsController.Bots.BotOwners)
                            {
                                if (otherBot == null || otherBot.Profile.Id == __instance.Profile.Id) continue;

                                var otherMarker = otherBot.gameObject.GetComponent<JiangHuBotMarker>();
                                var otherSpawnData = otherBot.SpawnProfileData as BotProfileDataClass;
                                string otherSpawnId = otherSpawnData?.SpawnParams?.Id_spawn;

                                bool isOtherFriendly = otherMarker != null && otherMarker.HostilityType == HostilityType.Friendly;
                                bool isOtherTeammate = otherSpawnId?.StartsWith("jianghu_deathmatch_teammate") == true;
                                bool isOtherUniversalFriendly = otherSpawnId?.StartsWith("JiangHu_Bot|") == true &&
                                                               otherSpawnId.Contains("|Friendly");

                                if (isOtherFriendly || isOtherTeammate || isOtherUniversalFriendly)
                                {
                                    __instance.BotsGroup?.RemoveEnemy(otherBot);
                                    otherBot.BotsGroup?.RemoveEnemy(__instance);
                                    if (__instance.BotsGroup != null && !__instance.BotsGroup.Allies.Contains(otherBot.GetPlayer))
                                        __instance.BotsGroup.AddAlly(otherBot.GetPlayer);
                                }
                                else
                                {
                                    if (__instance.BotsGroup != null && !__instance.BotsGroup.Enemies.ContainsKey(otherBot))
                                        __instance.BotsGroup.AddEnemy(otherBot, EBotEnemyCause.initial);
                                }
                            }
                            break;

                        case HostilityType.Enemy:
                            if (__instance.BotsGroup != null && !__instance.BotsGroup.Enemies.ContainsKey(mainPlayer))
                                __instance.BotsGroup.AddEnemy(mainPlayer, EBotEnemyCause.initial);

                            foreach (var otherBot in botGame.BotsController.Bots.BotOwners)
                            {
                                if (otherBot == null || otherBot.Profile.Id == __instance.Profile.Id) continue;

                                var otherMarker = otherBot.gameObject.GetComponent<JiangHuBotMarker>();
                                if (otherMarker != null && otherMarker.HostilityType == HostilityType.Enemy)
                                {
                                    __instance.BotsGroup?.RemoveEnemy(otherBot);
                                    otherBot.BotsGroup?.RemoveEnemy(__instance);
                                    if (__instance.BotsGroup != null && !__instance.BotsGroup.Allies.Contains(otherBot.GetPlayer))
                                        __instance.BotsGroup.AddAlly(otherBot.GetPlayer);
                                }
                                else
                                {
                                    if (__instance.BotsGroup != null && !__instance.BotsGroup.Enemies.ContainsKey(otherBot))
                                        __instance.BotsGroup.AddEnemy(otherBot, EBotEnemyCause.initial);
                                }
                            }
                            break;

                        case HostilityType.Neutral:
                            __instance.BotsGroup?.RemoveEnemy(mainPlayer, EBotEnemyCause.initial);

                            if (__instance.BotsGroup != null && !__instance.BotsGroup.Neutrals.ContainsKey(mainPlayer))
                            {
                                __instance.BotsGroup.AddNeutral(mainPlayer);
                            }
                            if (mainPlayer.BotsGroup != null)
                            {
                                mainPlayer.BotsGroup.RemoveEnemy(__instance, EBotEnemyCause.initial);

                                if (!mainPlayer.BotsGroup.Neutrals.ContainsKey(__instance))
                                {
                                    mainPlayer.BotsGroup.AddNeutral(__instance);
                                }
                            }
                            break;
                    }
                    return;
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(BotsGroup), "AddEnemy")]
    class BotsGroupAddEnemyPatch
    {
        static bool Prefix(IPlayer person, EBotEnemyCause cause, BotsGroup __instance)
        {
            try
            {
                bool isJiangHuFriendlyGroup = false;

                foreach (var member in __instance.Members)
                {
                    var marker = member.gameObject.GetComponent<JiangHuBotMarker>();
                    if (marker != null && marker.HostilityType == HostilityType.Friendly)
                    {
                        isJiangHuFriendlyGroup = true;
                        break;
                    }

                    var spawnData = member.SpawnProfileData as BotProfileDataClass;
                    if (spawnData?.SpawnParams?.Id_spawn?.StartsWith("jianghu_deathmatch_teammate") == true)
                    {
                        isJiangHuFriendlyGroup = true;
                        break;
                    }
                }

                if (isJiangHuFriendlyGroup)
                {
                    var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                    if (mainPlayer != null && person.ProfileId == mainPlayer.ProfileId)
                    {
                        return false;
                    }
                }
            }
            catch { }

            return true;
        }
    }

    public class BotMarkerController : MonoBehaviour
    {
        private const float UpdateInterval = 0.25f;
        private GameWorld _gameWorld;
        private Player _player;
        private float _nextUpdate;
        private Camera _camera;
        private readonly Dictionary<string, BotInfo> _bots = new Dictionary<string, BotInfo>();
        private GUIStyle _nameStyle;
        private GUIStyle _typeStyle;
        private Texture2D _hostileCircleTex;
        private Texture2D _friendlyCircleTex;
        private Texture2D _neutralCircleTex;


        private class BotInfo
        {
            public string Name;
            public Vector3 Position;
            public float Distance;
            public WildSpawnType? Role;
            public bool IsHostile;
            public bool IsFriendly;
            public bool IsJiangHuBot;
            public HostilityType? JiangHuHostility;
            public Rect GuiRect;
            public Vector3 LastScreenPos;
            public float LastSeen;
        }

        void Start()
        {
            if (!ShowBotIndicator.Value) { Destroy(this); return; }
            _gameWorld = Singleton<GameWorld>.Instance;
            _player = _gameWorld?.MainPlayer;
            _camera = Camera.main;
            if (_gameWorld == null || _player == null || _camera == null) Destroy(this);
        }

        void Update()
        {
            if (ShowBotIndicator.Value)
            {
                UpdateBots();
                CleanupOldBots();
            }
        }

        void OnGUI()
        {
            if (!ShowBotIndicator.Value) return;
            if (_camera == null) return;

            if (_nameStyle == null)
            {
                _nameStyle = new GUIStyle(GUI.skin.label);
                _nameStyle.alignment = TextAnchor.MiddleCenter;
                _nameStyle.fontSize = 16;
                _nameStyle.normal.textColor = Color.white;
            }

            if (_typeStyle == null)
            {
                _typeStyle = new GUIStyle(GUI.skin.label);
                _typeStyle.alignment = TextAnchor.MiddleCenter;
                _typeStyle.fontSize = 14;

                Color typeColor;
                if (ColorUtility.TryParseHtmlString("#2775b6", out typeColor))
                {
                    _typeStyle.normal.textColor = typeColor;
                }
            }

            if (_hostileCircleTex == null)
            {
                _hostileCircleTex = CreateCircleTexture(12, "#f43e06");
                _friendlyCircleTex = CreateCircleTexture(12, "#45b787");
                _neutralCircleTex = CreateCircleTexture(12, "#808080");
            }

            foreach (var kvp in _bots)
            {
                var bot = kvp.Value;
                Vector3 screenPos = _camera.WorldToScreenPoint(bot.Position + Vector3.up * 1.7f);

                if (screenPos.z > 0)
                {
                    float guiX = screenPos.x;
                    float guiY = Screen.height - screenPos.y;

                    // Check if DeathMatch enemy team bot
                    Texture2D circleTex = null;
                    Rect circleRect = new Rect(guiX - 6, guiY - 6, 12, 12);

                    // Check if DeathMatch enemy team bot
                    var botOwner = GetBotOwnerByProfileId(kvp.Key);
                    if (botOwner != null)
                    {
                        var spawnData = botOwner.SpawnProfileData as BotProfileDataClass;
                        string spawnId = spawnData?.SpawnParams?.Id_spawn;

                        if (!string.IsNullOrEmpty(spawnId) && spawnId.StartsWith("jianghu_deathmatch_enemy_team"))
                        {
                            // Extract team marker
                            string teamMarker = spawnId;
                            if (teamMarker.Contains("|"))
                            {
                                teamMarker = teamMarker.Split('|')[0];
                            }

                            if (teamMarker.StartsWith("jianghu_deathmatch_enemy_team"))
                            {
                                if (F12Manager.ShowTeam.Value)
                                {
                                    // Use team color
                                    Color teamColor = DeathMatchShared.GetTeamColor(teamMarker);
                                    string hexColor = ColorUtility.ToHtmlStringRGB(teamColor);
                                    if (!hexColor.StartsWith("#"))
                                    {
                                        hexColor = "#" + hexColor;
                                    }
                                    circleTex = CreateCircleTexture(12, hexColor);
                                }
                                else
                                {
                                    circleTex = _hostileCircleTex;
                                }

                                GUI.DrawTexture(circleRect, circleTex);
                            }
                        }
                    }

                    if (circleTex == null)
                    {
                        if (bot.IsHostile)
                            circleTex = _hostileCircleTex;
                        else if (bot.IsFriendly)
                            circleTex = _friendlyCircleTex;
                        else
                            circleTex = _neutralCircleTex;
                    }

                    // Draw circle
                    GUI.DrawTexture(circleRect, circleTex);

                    // Build display text
                    string displayText = "";
                    if (ShowBotName.Value)
                        displayText = bot.Name;

                    if (ShowDistance.Value)
                    {
                        if (!string.IsNullOrEmpty(displayText))
                            displayText += $" <color=yellow>{bot.Distance:F0}m</color>";
                        else
                            displayText = $"<color=yellow>{bot.Distance:F0}m</color>";
                    }

                    // Draw bot type
                    if (ShowBotType.Value && bot.Role.HasValue)
                    {
                        string typeText = UniversalBotSpawner.GetBotDisplayName(bot.Role.Value);
                        GUIContent typeContent = new GUIContent(typeText);
                        Vector2 typeSize = _typeStyle.CalcSize(typeContent);
                        Rect typeRect = new Rect(
                            guiX - typeSize.x / 2,
                            guiY - 50,
                            typeSize.x,
                            typeSize.y
                        );
                        GUI.Label(typeRect, typeText, _typeStyle);
                    }

                    // Draw name/distance
                    if (!string.IsNullOrEmpty(displayText))
                    {
                        GUIContent nameContent = new GUIContent(displayText);
                        Vector2 nameSize = _nameStyle.CalcSize(nameContent);
                        Rect nameRect = new Rect(
                            guiX - nameSize.x / 2,
                            guiY - 35,
                            nameSize.x,
                            nameSize.y
                        );
                        GUI.Label(nameRect, displayText, _nameStyle);
                    }
                }
            }
        }

        private BotOwner GetBotOwnerByProfileId(string profileId)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld?.AllAlivePlayersList != null)
            {
                foreach (Player player in gameWorld.AllAlivePlayersList)
                {
                    if (player.ProfileId == profileId)
                    {
                        return player.AIData?.BotOwner;
                    }
                }
            }
            return null;
        }



        private Texture2D CreateCircleTexture(int size, string hexColor)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hexColor, out color);

            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            Color[] pixels = new Color[size * size];

            float radius = size / 2f;
            float center = size / 2f - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (distance <= radius)
                    {
                        pixels[y * size + x] = color;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }




        private void UpdateBots()
        {
            if (_gameWorld.AllAlivePlayersList == null) return;
            float currentTime = Time.time;

            foreach (Player player in _gameWorld.AllAlivePlayersList)
            {
                if (player.ProfileId == _player.ProfileId) continue;
                ProcessBot(player, currentTime);
            }
        }

        private void ProcessBot(Player player, float currentTime)
        {
            string id = player.ProfileId;
            float distance = Vector3.Distance(player.Position, _player.Position);
            var botOwner = player.AIData?.BotOwner;
            if (botOwner == null) return;

            var role = botOwner.Profile?.Info?.Settings?.Role;

            // Check if DeathMatch enemy team bot - SKIP regular hostility checks
            var spawnData = botOwner.SpawnProfileData as BotProfileDataClass;
            bool isDeathMatchEnemy = spawnData?.SpawnParams?.Id_spawn?.StartsWith("jianghu_deathmatch_enemy_") == true;

            if (isDeathMatchEnemy)
            {
                // Handle DeathMatch enemy teams with custom colors
                if (!F12Manager.ShowTeam.Value && !F12Manager.ShowEnemyBots.Value) return;

                ProcessDeathMatchEnemyBot(botOwner, id, distance, currentTime);
                return;
            }

            // Check hostility
            bool isHostile = false;
            bool isFriendly = false;
            bool isNeutral = false;

            // Check if JiangHu bot (for reference only, not used for detection)
            var jiangHuMarker = botOwner.gameObject.GetComponent<JiangHuBotMarker>();
            bool isJiangHuBot = jiangHuMarker != null;
            HostilityType? jiangHuHostility = null;

            if (isJiangHuBot)
            {
                jiangHuHostility = jiangHuMarker.HostilityType;
            }

            // Check vanilla hostility dictionaries
            if (botOwner.BotsGroup != null && _player != null)
            {
                if (botOwner.BotsGroup.Enemies != null)
                {
                    isHostile = botOwner.BotsGroup.Enemies.ContainsKey(_player);
                }

                if (botOwner.BotsGroup.Allies != null)
                {
                    isFriendly = botOwner.BotsGroup.Allies.Contains(_player);
                }

                if (botOwner.BotsGroup.Neutrals != null)
                {
                    isNeutral = botOwner.BotsGroup.Neutrals.ContainsKey(_player);
                }
            }

            // Check visibility based on settings
            bool shouldShow = false;

            if (botOwner.BotsGroup != null && _player != null)
            {
                if (F12Manager.ShowFriendlyBots.Value && isFriendly)
                    shouldShow = true;
                else if (F12Manager.ShowEnemyBots.Value && isHostile)
                    shouldShow = true;
                else if (F12Manager.ShowNeutralBots.Value && isNeutral)
                    shouldShow = true;
            }


            if (!shouldShow) return;

            if (!_bots.TryGetValue(id, out var bot))
            {
                string botName = player.Profile?.Nickname ?? UniversalBotSpawner.GetBotDisplayName(role.Value);
                bot = new BotInfo
                {
                    Name = botName,
                    Role = role,
                    IsHostile = isHostile,
                    IsFriendly = isFriendly,
                    IsJiangHuBot = isJiangHuBot,
                    JiangHuHostility = jiangHuHostility
                };
                _bots[id] = bot;
            }

            bot.Position = player.Position;
            bot.Distance = distance;
            bot.Role = role;
            bot.IsHostile = isHostile;
            bot.IsFriendly = isFriendly;
            bot.IsJiangHuBot = isJiangHuBot;
            bot.JiangHuHostility = jiangHuHostility;
            bot.LastSeen = currentTime;
        }

        private void ProcessDeathMatchEnemyBot(BotOwner bot, string id, float distance, float currentTime)
        {
            string spawnId = (bot.SpawnProfileData as BotProfileDataClass)?.SpawnParams?.Id_spawn;
            if (string.IsNullOrEmpty(spawnId)) return;

            string teamMarker = spawnId;
            if (teamMarker.Contains("|"))
            {
                teamMarker = teamMarker.Split('|')[0];
            }

            if (!teamMarker.StartsWith("jianghu_deathmatch_enemy_team")) return;

            if (!_bots.TryGetValue(id, out var botInfo))
            {
                string botName = bot.GetPlayer.Profile?.Nickname ?? UniversalBotSpawner.GetBotDisplayName(bot.Profile.Info.Settings.Role);
                botInfo = new BotInfo
                {
                    Name = botName,
                    Role = bot.Profile.Info.Settings.Role,
                    IsHostile = true,
                    IsFriendly = false,
                    IsJiangHuBot = true,
                    JiangHuHostility = null
                };
                _bots[id] = botInfo;
            }

            botInfo.Position = bot.Position;
            botInfo.Distance = distance;
            botInfo.LastSeen = currentTime;
        }

        private BotOwner GetBotOwnerFromPlayerId(string playerId)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld?.AllAlivePlayersList != null)
            {
                foreach (Player player in gameWorld.AllAlivePlayersList)
                {
                    if (player.ProfileId == playerId)
                    {
                        return player.AIData?.BotOwner;
                    }
                }
            }
            return null;
        }

        private void CleanupOldBots()
        {
            float currentTime = Time.time;
            List<string> toRemove = new List<string>();
            foreach (var kvp in _bots)
                if (currentTime - kvp.Value.LastSeen > 1f)
                    toRemove.Add(kvp.Key);

            foreach (string id in toRemove)
                _bots.Remove(id);
        }

        void OnDestroy()
        {
            _bots.Clear();
        }
    }



    [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
    class GameWorldStartPatch
    {
        [HarmonyPostfix]
        static void Postfix(GameWorld __instance)
        {
            var existing = __instance.GetComponent<BotMarkerController>();
            if (existing != null) UnityEngine.Object.Destroy(existing);
            if (F12Manager.ShowBotIndicator.Value)
            {
                __instance.gameObject.AddComponent<BotMarkerController>();
            }

            var existingHighlighter = __instance.GetComponent<BotBodyHighlighter>();
            if (existingHighlighter != null) UnityEngine.Object.Destroy(existingHighlighter);
            if (F12Manager.ShowBotBodyHighlight.Value)
            {
                __instance.gameObject.AddComponent<BotBodyHighlighter>();
            }
        }
    }



    public class BotBodyHighlighter : MonoBehaviour
    {
        private Camera _camera;
        private Material _highlightMaterial;
        private readonly Dictionary<string, BotHighlightInfo> _highlightedBots = new();
        private bool _isInitialized = false;
        private GameWorld _gameWorld;
        private bool _lastHighlightEnabled = true;
        private bool _lastHighlightFriendly = true;
        private bool _lastHighlightEnemy = true;
        private bool _lastHighlightNeutral = false;
        private bool _lastHighlightTeam = true;


        private class BotHighlightInfo
        {
            public BotOwner Bot;
            public Renderer[] Renderers;
            public HostilityType Hostility;
            public bool IsJiangHuBot;
            public bool IsAlive = true;
            public Material[] OriginalMaterials;
        }

        void Start()
        {
            if (!F12Manager.ShowBotBodyHighlight.Value)
            {
                Destroy(this);
                return;
            }

            try
            {
                _gameWorld = Singleton<GameWorld>.Instance;
                _camera = Camera.main;

                if (_gameWorld == null || _camera == null)
                {
                    Destroy(this);
                    return;
                }

                _highlightMaterial = new Material(Shader.Find("Sprites/Default"));
                _highlightMaterial.color = F12Manager.FriendlyBotColor.Value;
                _highlightMaterial.SetFloat("_Mode", 2);
                _highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _highlightMaterial.SetInt("_ZWrite", 0);
                _highlightMaterial.DisableKeyword("_ALPHATEST_ON");
                _highlightMaterial.EnableKeyword("_ALPHABLEND_ON");
                _highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                _highlightMaterial.renderQueue = 3000;

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Destroy(this);
            }
        }

        void Update()
        {
            if (!_isInitialized || !F12Manager.ShowBotBodyHighlight.Value)
            {
                if (_highlightedBots.Count > 0)
                {
                    ClearAllHighlights();
                }
                return;
            }

            CheckHighlightSettingsChanged();


            if (!_isInitialized || !F12Manager.ShowBotBodyHighlight.Value)
                return;

            if (_gameWorld?.AllAlivePlayersList != null)
            {
                foreach (Player player in _gameWorld.AllAlivePlayersList)
                {
                    if (player.IsYourPlayer) continue;

                    var botOwner = player.AIData?.BotOwner;
                    if (botOwner != null && !botOwner.IsDead)
                    {
                        ProcessBot(botOwner);
                    }
                }
            }

            CleanupDeadBots();
        }

        private void CheckHighlightSettingsChanged()
        {
            bool settingsChanged = false;

            if (_lastHighlightEnabled != F12Manager.ShowBotBodyHighlight.Value ||
                _lastHighlightFriendly != F12Manager.HighlightFriendlyBots.Value ||
                _lastHighlightEnemy != F12Manager.HighlightEnemyBots.Value ||
                _lastHighlightNeutral != F12Manager.HighlightNeutralBots.Value ||
                _lastHighlightTeam != F12Manager.HighlightTeam.Value) 
            {
                settingsChanged = true;
                _lastHighlightEnabled = F12Manager.ShowBotBodyHighlight.Value;
                _lastHighlightFriendly = F12Manager.HighlightFriendlyBots.Value;
                _lastHighlightEnemy = F12Manager.HighlightEnemyBots.Value;
                _lastHighlightNeutral = F12Manager.HighlightNeutralBots.Value;
                _lastHighlightTeam = F12Manager.HighlightTeam.Value;
            }

            if (settingsChanged)
            {
                ReEvaluateAllBots();
            }
        }

        private void ReEvaluateAllBots()
        {
            List<string> botIds = new List<string>(_highlightedBots.Keys);

            foreach (string botId in botIds)
            {
                if (_highlightedBots.TryGetValue(botId, out var botInfo))
                {
                    if (botInfo.Bot == null || botInfo.Bot.IsDead)
                    {
                        RestoreBotMaterials(botInfo);
                        _highlightedBots.Remove(botId);
                        continue;
                    }

                    var marker = botInfo.Bot.gameObject.GetComponent<JiangHuBotMarker>();
                    bool isJiangHuBot = marker != null;
                    HostilityType? hostility = marker?.HostilityType;

                    bool shouldHighlight = false;
                    var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;

                    // Check DeathMatch enemy team bots
                    var spawnData = botInfo.Bot.SpawnProfileData as BotProfileDataClass;
                    bool isDeathMatchEnemy = spawnData?.SpawnParams?.Id_spawn?.StartsWith("jianghu_deathmatch_enemy_") == true;

                    if (isDeathMatchEnemy)
                    {
                        // DeathMatch enemies check 3 settings
                        shouldHighlight = F12Manager.HighlightTeam.Value ||
                                         F12Manager.HighlightEnemyBots.Value;
                    }
                    else if (botInfo.Bot.BotsGroup != null && mainPlayer != null)
                    {
                        if (botInfo.Bot.BotsGroup.Neutrals.ContainsKey(mainPlayer) &&
                            F12Manager.HighlightNeutralBots.Value)
                            shouldHighlight = true;
                        else if (F12Manager.HighlightFriendlyBots.Value &&
                                 botInfo.Bot.BotsGroup.Allies.Contains(mainPlayer))
                            shouldHighlight = true;
                        else if (F12Manager.HighlightEnemyBots.Value &&
                                 botInfo.Bot.BotsGroup.Enemies.ContainsKey(mainPlayer))
                            shouldHighlight = true;
                    }

                    if (!shouldHighlight)
                    {
                        RestoreBotMaterials(botInfo);
                        _highlightedBots.Remove(botId);
                    }
                }
            }
        }

        private void ClearAllHighlights()
        {
            foreach (var kvp in _highlightedBots)
            {
                RestoreBotMaterials(kvp.Value);
            }
            _highlightedBots.Clear();
        }


        void OnDestroy()
        {
            foreach (var kvp in _highlightedBots)
            {
                RestoreBotMaterials(kvp.Value);
            }
            _highlightedBots.Clear();

            if (_highlightMaterial != null)
            {
                Destroy(_highlightMaterial);
            }
        }

        private void ProcessBot(BotOwner bot)
        {
            // Check if DeathMatch enemy team bot - skip regular processing
            var spawnData = bot.SpawnProfileData as BotProfileDataClass;
            bool isDeathMatchEnemy = spawnData?.SpawnParams?.Id_spawn?.StartsWith("jianghu_deathmatch_enemy_") == true;

            if (isDeathMatchEnemy)
            {
                if (F12Manager.HighlightTeam.Value ||
                    F12Manager.HighlightEnemyBots.Value)
                {
                    ProcessDeathMatchEnemyHighlight(bot);
                }
                return;
            }

            string botId = bot.name;

            var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;

            if (_highlightedBots.ContainsKey(botId))
            {
                var existingInfo = _highlightedBots[botId];
                existingInfo.IsAlive = !bot.IsDead;

                // Recalculate hostility to check if changed
                HostilityType currentHostility = existingInfo.Hostility;
                HostilityType newHostility = HostilityType.Neutral;

                if (bot.BotsGroup != null && mainPlayer != null)
                {
                    if (bot.BotsGroup.Enemies.ContainsKey(mainPlayer))
                        newHostility = HostilityType.Enemy;
                    else if (bot.BotsGroup.Allies.Contains(mainPlayer))
                        newHostility = HostilityType.Friendly;
                    else if (bot.BotsGroup.Neutrals.ContainsKey(mainPlayer))
                        newHostility = HostilityType.Neutral;
                }

                // If hostility changed, update highlight
                if (currentHostility != newHostility)
                {
                    RestoreBotMaterials(existingInfo);
                    _highlightedBots.Remove(botId);
                    // Continue to re-process with new hostility
                }
                else
                {
                    return; 
                }
            }

            var marker = bot.gameObject.GetComponent<JiangHuBotMarker>();
            bool isJiangHuBot = marker != null;
            bool shouldShow = false;

            // Vanilla hostility check
            if (bot.BotsGroup != null && mainPlayer != null)
            {
                if (bot.BotsGroup.Neutrals.ContainsKey(mainPlayer) &&
                    F12Manager.HighlightNeutralBots.Value)
                    shouldShow = true;

                else if (F12Manager.HighlightFriendlyBots.Value &&
                         bot.BotsGroup.Allies.Contains(mainPlayer))
                    shouldShow = true;

                else if (F12Manager.HighlightEnemyBots.Value &&
                         bot.BotsGroup.Enemies.ContainsKey(mainPlayer))
                    shouldShow = true;
            }

            if (!shouldShow) return;

            var renderers = bot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            var originalMaterials = new List<Material[]>();
            foreach (var renderer in renderers)
            {
                originalMaterials.Add(renderer.sharedMaterials);
            }

            HostilityType vanillaHostility = HostilityType.Neutral;
            if (bot.BotsGroup != null && mainPlayer != null)
            {
                if (bot.BotsGroup.Enemies.ContainsKey(mainPlayer))
                    vanillaHostility = HostilityType.Enemy;
                else if (bot.BotsGroup.Allies.Contains(mainPlayer))
                    vanillaHostility = HostilityType.Friendly;
                else if (bot.BotsGroup.Neutrals.ContainsKey(mainPlayer))
                    vanillaHostility = HostilityType.Neutral;
            }

            Color highlightColor = GetHighlightColor(vanillaHostility);

            Material highlightMat = new Material(_highlightMaterial);
            highlightMat.color = highlightColor;

            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var materials = new List<Material>(renderer.sharedMaterials);
                materials.Add(highlightMat);
                renderer.sharedMaterials = materials.ToArray();
            }

            var botInfo = new BotHighlightInfo
            {
                Bot = bot,
                Renderers = renderers,
                Hostility = vanillaHostility,
                IsJiangHuBot = isJiangHuBot,
                IsAlive = !bot.IsDead,
                OriginalMaterials = originalMaterials.SelectMany(arr => arr).ToArray()
            };

            _highlightedBots[botId] = botInfo;
        }

        private void ProcessDeathMatchEnemyHighlight(BotOwner bot)
        {
            string botId = bot.name;
            string spawnId = (bot.SpawnProfileData as BotProfileDataClass)?.SpawnParams?.Id_spawn;
            if (string.IsNullOrEmpty(spawnId)) return;

            // Extract team marker
            string teamMarker = spawnId;
            int pipeIndex = teamMarker.IndexOf('|');
            if (pipeIndex > 0)
            {
                teamMarker = teamMarker.Substring(0, pipeIndex);
            }

            // Ensure it's a deathmatch enemy team
            if (!teamMarker.StartsWith("jianghu_deathmatch_enemy_team")) return;

            if (!_highlightedBots.ContainsKey(botId))
            {
                var renderers = bot.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;

                var originalMaterials = new List<Material[]>();
                foreach (var renderer in renderers)
                {
                    originalMaterials.Add(renderer.sharedMaterials);
                }

                // DECIDE WHICH COLOR TO USE:
                Color highlightColor;

                if (F12Manager.HighlightTeam.Value)
                {
                    // Use team color when HighlightTeam is enabled
                    highlightColor = DeathMatchShared.GetTeamColor(teamMarker);
                }
                else
                {
                    // Fall back to enemy color when only HighlightEnemyBots is enabled
                    highlightColor = F12Manager.EnemyBotColor.Value;
                }

                Material highlightMat = new Material(_highlightMaterial);
                highlightMat.color = highlightColor;

                for (int i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    var materials = new List<Material>(renderer.sharedMaterials);
                    materials.Add(highlightMat);
                    renderer.sharedMaterials = materials.ToArray();
                }

                var botInfo = new BotHighlightInfo
                {
                    Bot = bot,
                    Renderers = renderers,
                    Hostility = HostilityType.Enemy,
                    IsJiangHuBot = true,
                    IsAlive = !bot.IsDead,
                    OriginalMaterials = originalMaterials.SelectMany(arr => arr).ToArray()
                };

                _highlightedBots[botId] = botInfo;
            }
            else
            {
                var botInfo = _highlightedBots[botId];
                botInfo.IsAlive = !bot.IsDead;

                // Update color when HighlightTeam setting changes
                if (botInfo.Renderers != null && botInfo.Renderers.Length > 0)
                {
                    // Find the highlight material in the last renderer
                    var lastRenderer = botInfo.Renderers.Last();
                    if (lastRenderer.sharedMaterials.Length > 0)
                    {
                        var lastMaterial = lastRenderer.sharedMaterials.Last();
                        if (lastMaterial != null)
                        {
                            Color targetColor = F12Manager.HighlightTeam.Value
                                ? DeathMatchShared.GetTeamColor(teamMarker)
                                : F12Manager.EnemyBotColor.Value;

                            if (lastMaterial.color != targetColor)
                            {
                                lastMaterial.color = targetColor;
                            }
                        }
                    }
                }
            }
        }


        private Color GetHighlightColor(HostilityType hostility)
        {
            switch (hostility)
            {
                case HostilityType.Friendly:
                    return F12Manager.FriendlyBotColor.Value;
                case HostilityType.Enemy:
                    return F12Manager.EnemyBotColor.Value;
                case HostilityType.Neutral:
                    return F12Manager.NeutralBotColor.Value;
                default:
                    return F12Manager.FriendlyBotColor.Value;
            }
        }

        private void RestoreBotMaterials(BotHighlightInfo botInfo)
        {
            if (botInfo.Renderers == null || botInfo.OriginalMaterials == null)
                return;

            int materialIndex = 0;
            for (int i = 0; i < botInfo.Renderers.Length; i++)
            {
                var renderer = botInfo.Renderers[i];
                if (renderer != null && materialIndex < botInfo.OriginalMaterials.Length)
                {
                    var originalCount = renderer.sharedMaterials.Length - 1;
                    var originalMats = new Material[originalCount];
                    Array.Copy(botInfo.OriginalMaterials, materialIndex, originalMats, 0, originalCount);
                    renderer.sharedMaterials = originalMats;
                    materialIndex += originalCount;
                }
            }
        }

        private void CleanupDeadBots()
        {
            List<string> toRemove = new List<string>();

            foreach (var kvp in _highlightedBots)
            {
                var botInfo = kvp.Value;
                if (botInfo.Bot == null || botInfo.Bot.IsDead || !botInfo.IsAlive)
                {
                    RestoreBotMaterials(botInfo);
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                _highlightedBots.Remove(key);
            }
        }
    }
}