using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.Game.Spawning;
using EFT.HealthSystem;
using EFT.UI;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using EFT.CameraControl;


namespace JiangHu
{
    #region Shared Utilities
    public static class DeathMatchShared
    {
        public static List<string> GetEnabledMapsFromConfig()
        {
            List<string> enabledMaps = new List<string>();

            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (configDict != null)
                    {
                        Dictionary<string, string> mapConfigKeys = new Dictionary<string, string>
                        {
                            { "Woods", "Enable_Woods" },
                            { "factory4_day", "Enable_factoryday" },
                            { "factory4_night", "Enable_factorynight" },
                            { "bigmap", "Enable_bigmap" },
                            { "Shoreline", "Enable_Shoreline" },
                            { "Interchange", "Enable_Interchange" },
                            { "RezervBase", "Enable_RezervBase" },
                            { "laboratory", "Enable_laboratory" },
                            { "Lighthouse", "Enable_Lighthouse" },
                            { "TarkovStreets", "Enable_TarkovStreets" },
                            { "Sandbox", "Enable_Sandbox" },
                            { "Sandbox_high", "Enable_Sandboxhigh" },
                            { "labyrinth", "Enable_labyrinth" }
                        };

                        foreach (var kvp in mapConfigKeys)
                        {
                            string mapName = kvp.Key;
                            string configKey = kvp.Value;

                            if (configDict.ContainsKey(configKey) && configDict[configKey] is bool && (bool)configDict[configKey])
                            {
                                enabledMaps.Add(mapName);
                            }
                        }

                        if (enabledMaps.Count == 0)
                        {
                            enabledMaps.AddRange(mapConfigKeys.Keys);
                        }
                    }
                }
                else
                {
                    enabledMaps.AddRange(new[] {
                        "Woods", "factory4_day", "factory4_night", "bigmap", "Shoreline",
                        "Interchange", "RezervBase", "laboratory", "Lighthouse", "TarkovStreets",
                        "Sandbox", "Sandbox_high", "labyrinth"
                    });
                }
            }
            catch
            {
                enabledMaps.AddRange(new[] {
                    "Woods", "factory4_day", "factory4_night", "bigmap", "Shoreline",
                    "Interchange", "RezervBase", "laboratory", "Lighthouse", "TarkovStreets",
                    "Sandbox", "Sandbox_high", "labyrinth"
                });
            }

            return enabledMaps;
        }

        public static int GetDeathMatchLivesFromConfig()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (configDict != null && configDict.ContainsKey("DeathMatch_Lives"))
                    {
                        return Convert.ToInt32(configDict["DeathMatch_Lives"]);
                    }
                }
            }
            catch { }
            return 0;
        }

        public static int GetOurSquadNumber()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (configDict != null && configDict.ContainsKey("Our_SquadNumber"))
                    {
                        return Convert.ToInt32(configDict["Our_SquadNumber"]);
                    }
                }
            }
            catch { }
            return 0; 
        }

        public static int GetEnemySquadNumber()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (configDict != null && configDict.ContainsKey("Enemy_SquadNumber"))
                    {
                        return Convert.ToInt32(configDict["Enemy_SquadNumber"]);
                    }
                }
            }
            catch { }
            return 0; 
        }

        public static int GetEnemyTeamCount()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (configDict != null && configDict.ContainsKey("Enemy_Team_Number"))
                    {
                        int count = Convert.ToInt32(configDict["Enemy_Team_Number"]);
                        return Math.Max(0, Math.Min(count, 6)); // Limit to 6 teams for colors
                    }
                }
            }
            catch { }
            return 0; 
        }

        public static List<WildSpawnType> GetWeightedBotPool()
        {
            var botPool = new List<WildSpawnType>();

            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (configDict != null)
                    {
                        var botConfigMapping = new Dictionary<string, WildSpawnType>
                        {
                            { "PMC_BEAR", WildSpawnType.pmcBEAR },
                            { "PMC_USEC", WildSpawnType.pmcUSEC },
                            { "Scav", WildSpawnType.assault },
                            { "Raider", WildSpawnType.pmcBot },
                            { "Rogue", WildSpawnType.exUsec },
                            { "Smuggler", WildSpawnType.arenaFighterEvent },
                            { "Cultist Warrior", WildSpawnType.sectantWarrior },
                            { "Scav Sniper", WildSpawnType.marksman },
                            { "Reshala", WildSpawnType.bossBully },
                            { "Gluhar", WildSpawnType.bossGluhar },
                            { "Shturman", WildSpawnType.bossKojaniy },
                            { "Sanitar", WildSpawnType.bossSanitar },
                            { "Killa", WildSpawnType.bossKilla },
                            { "Tagilla", WildSpawnType.bossTagilla },
                            { "Knight", WildSpawnType.bossKnight },
                            { "Zryachiy", WildSpawnType.bossZryachiy },
                            { "Kaban", WildSpawnType.bossBoar },
                            { "Kolontay", WildSpawnType.bossKolontay },
                            { "Partisan", WildSpawnType.bossPartisan },
                            { "Big Pipe", WildSpawnType.followerBigPipe },
                            { "Bird Eye", WildSpawnType.followerBirdEye },
                            { "Tagilla_Aggro", WildSpawnType.bossTagillaAgro },
                            { "Killa_Aggro", WildSpawnType.bossKillaAgro },
                            { "Cultist Priest", WildSpawnType.sectantPriest }
                        };

                        foreach (var kvp in botConfigMapping)
                        {
                            string configKey = kvp.Key;
                            WildSpawnType spawnType = kvp.Value;

                            if (configDict.ContainsKey(configKey))
                            {
                                int weight = Convert.ToInt32(configDict[configKey]);
                                for (int i = 0; i < weight; i++)
                                {
                                    botPool.Add(spawnType);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // Default pool if empty
            if (botPool.Count == 0)
            {
                botPool.AddRange(new[] {
                    WildSpawnType.bossKnight,
                    WildSpawnType.bossGluhar,
                    WildSpawnType.pmcUSEC,
                    WildSpawnType.pmcBEAR,
                    WildSpawnType.bossKilla,
                    WildSpawnType.bossTagilla
                });
            }

            return botPool;
        }

        public static string GetTeamMarker(int teamIndex)
        {
            if (teamIndex == 0)
                return "jianghu_deathmatch_teammate";
            else
                return $"jianghu_deathmatch_enemy_team{teamIndex}";
        }

        public static Color GetTeamColor(string teamMarker)
        {
            if (teamMarker == "jianghu_deathmatch_teammate")
                return Color.green;

            // Extract team number from marker
            if (teamMarker.StartsWith("jianghu_deathmatch_enemy_team"))
            {
                string numStr = teamMarker.Replace("jianghu_deathmatch_enemy_team", "");
                if (int.TryParse(numStr, out int teamNum))
                {
                    switch (teamNum)
                    {
                        case 1: return ColorFromHex("#2775b6");  // Blue
                        case 2: return ColorFromHex("#fed71a");  // Yellow  
                        case 3: return ColorFromHex("#eea2a4");  // Pink
                        case 4: return ColorFromHex("#c02c38");  // 高粱红
                        case 5: return ColorFromHex("#753117");  // Dark Brown
                        case 6: return ColorFromHex("#8c4356");  // Purple-Red
                        default: return Color.red;
                    }
                }
            }

            return Color.red;
        }

        private static Color ColorFromHex(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return color;
        }
    }
    #endregion

    #region Core Systems
    public class DeathMatchCore : MonoBehaviour
    {
        public static DeathMatchCore Instance { get; private set; }
        public static bool DeathMatchModeActive = false;
        public int TeleportCount = 0;
        public Player CurrentPlayer { get; private set; }

        public DeathMatchPlayer PlayerComponent { get; private set; }


        void Awake()
        {
            Instance = this;

            PlayerComponent = GetComponent<DeathMatchPlayer>();
            if (PlayerComponent == null)
                PlayerComponent = gameObject.AddComponent<DeathMatchPlayer>();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            CurrentPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
        }

        public void Init()
        {
        }

        public static void EnableDeathMatchMode()
        {
            DeathMatchModeActive = true;
            DeathMatchBotSpawn.EnableTeamSpawnSystem();
            DeathMatchBotSpawn.InitialSpawnDone = false;
        }

        public static void DisableDeathMatchMode()
        {
            DeathMatchModeActive = false;
            DeathMatchBotSpawn.SystemActive = false;

            if (FreeCameraController.Instance != null)
                FreeCameraController.Instance.ResetFreeCamera();
        }

        public static void ValidateStateInMenu()
        {
            try
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                bool isInMenu = gameWorld == null;

                if (isInMenu && DeathMatchModeActive)
                {
                    DisableDeathMatchMode();

                    if (Instance != null)
                    {
                        Instance.TeleportCount = 0;
                    }
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(LocalGame), "Stop")]
    class DeathMatchRaidEndPatch
    {
        static void Postfix(string profileId, ExitStatus exitStatus, string exitName, float delay)
        {
            if (exitStatus != ExitStatus.Transit)
            {
                DeathMatchCore.DisableDeathMatchMode();
            }
        }
    }
    #endregion

    #region Player Systems
    public class DeathMatchPlayer : MonoBehaviour
    {

        public bool TryStartDeathTeleport(Player targetPlayer, EDamageType damageType)
        {
            int maxLives = DeathMatchShared.GetDeathMatchLivesFromConfig();
            if (DeathMatchCore.Instance.TeleportCount >= maxLives)
            {
                return false;
            }

            DeathMatchCore.Instance.TeleportCount++;

            targetPlayer.ActiveHealthController.IsAlive = true;
            targetPlayer.ActiveHealthController.SetDamageCoeff(0f);

            Vector3 spawnPosition = GetRandomSpawnPosition(targetPlayer); 
            if (spawnPosition != Vector3.zero)
            {
                TeleportPlayer(targetPlayer, spawnPosition); 
                RestorePlayerHealth(targetPlayer); 
                targetPlayer.ActiveHealthController.SetDamageCoeff(1f);

                int remaining = DeathMatchShared.GetDeathMatchLivesFromConfig() - DeathMatchCore.Instance.TeleportCount;
                string message = remaining > 0
                    ? $"Teleported! 传送成功，剩余 {remaining} left"
                    : "Teleported! 传送成功";

                NotificationManagerClass.DisplayMessageNotification(
                    message,
                    ENotificationDurationType.Default,
                    ENotificationIconType.Default,
                    Color.yellow
                );
            }
            return true;
        }

        private Vector3 GetRandomSpawnPosition(Player player)
        {
            if (player == null) return Vector3.zero;

            SpawnPointMarker[] allMarkers = UnityEngine.Object.FindObjectsOfType<SpawnPointMarker>();
            if (allMarkers == null || allMarkers.Length == 0) return Vector3.zero;

            Vector3 playerPos = player.Position;
            const float excludeRadius = 60f;
            float excludeRadiusSquared = excludeRadius * excludeRadius;

            var validSpawnPoints = new List<ISpawnPoint>();

            foreach (var marker in allMarkers)
            {
                if (marker != null && marker.SpawnPoint != null)
                {
                    Vector3 spawnPos = marker.SpawnPoint.Position;
                    float distanceSquared = (spawnPos - playerPos).sqrMagnitude;

                    if (distanceSquared > excludeRadiusSquared)
                    {
                        validSpawnPoints.Add(marker.SpawnPoint);
                    }
                }
            }

            if (validSpawnPoints.Count == 0)
            {
                foreach (var marker in allMarkers)
                {
                    if (marker != null && marker.SpawnPoint != null)
                    {
                        validSpawnPoints.Add(marker.SpawnPoint);
                    }
                }
            }

            if (validSpawnPoints.Count == 0) return Vector3.zero;

            int randomIndex = UnityEngine.Random.Range(0, validSpawnPoints.Count);
            ISpawnPoint selectedSpawn = validSpawnPoints[randomIndex];

            return selectedSpawn.Position;
        }

        private void TeleportPlayer(Player player, Vector3 position)
        {
            if (player == null) return;

            Vector2 currentLookDirection = player.Rotation;
            player.Teleport(position, true);
            player.Rotation = currentLookDirection;
        }

        private void RestorePlayerHealth(Player player)
        {
            ActiveHealthController healthController = player.ActiveHealthController;

            EBodyPart[] allBodyParts = {
                EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach,
                EBodyPart.LeftArm, EBodyPart.RightArm,
                EBodyPart.LeftLeg, EBodyPart.RightLeg
            };

            foreach (EBodyPart bodyPart in allBodyParts)
            {
                try
                {
                    var bodyPartState = healthController.Dictionary_0[bodyPart];

                    float oldHealth = bodyPartState.Health.Current;
                    float maxHealth = healthController.GetBodyPartHealth(bodyPart).Maximum;

                    bodyPartState.Health = new HealthValue(maxHealth, maxHealth, 0f);
                    bodyPartState.IsDestroyed = false;

                    healthController.method_44(bodyPart, EDamageType.Medicine);
                    healthController.method_36(bodyPart);
                }
                catch { }
            }
        }

        public void ResetTeleportState()
        {
            DeathMatchCore.Instance.TeleportCount = 0;
        }
    }

    [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.Kill))]
    class DeathMatchPlayerTeleportPatch
    {
        static bool Prefix(ActiveHealthController __instance, EDamageType damageType)
        {
            if (!DeathMatchCore.DeathMatchModeActive) return true;

            try
            {
                FieldInfo playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");
                if (playerField?.GetValue(__instance) is not Player player || player.IsAI)
                {
                    return true;
                }

                if (!player.IsYourPlayer)
                {
                    return true;
                }

                var deathMatchPlayer = DeathMatchCore.Instance?.PlayerComponent;
                if (deathMatchPlayer == null)
                {
                    return true;
                }

                bool teleportStarted = deathMatchPlayer.TryStartDeathTeleport(player, damageType);
                return !teleportStarted;
            }
            catch (Exception ex)
            {
                return true;
            }
        }
    }
    #endregion

    #region Bot Spawn Systems
    public class DeathMatchBotSpawn : MonoBehaviour
    {
        public static List<WildSpawnType> WeightedBotPool = new List<WildSpawnType>();
        public static Dictionary<string, List<string>> TeamMembers = new Dictionary<string, List<string>>();
        public static bool SystemActive = false;
        public static bool InitialSpawnDone = false;
        public static Dictionary<string, int> teamDeaths = new Dictionary<string, int>();

        public static void EnableTeamSpawnSystem()
        {
            if (!SystemActive)
            {
                SystemActive = true;
                WeightedBotPool = DeathMatchShared.GetWeightedBotPool();
                TeamMembers.Clear();
                StartSquadMaintenance();
            }
        }

        private static void InitializeTeams()
        {
            TeamMembers.Clear();

            TeamMembers["jianghu_deathmatch_teammate"] = new List<string>();

            int enemyTeamCount = DeathMatchShared.GetEnemyTeamCount();
            for (int i = 1; i <= enemyTeamCount; i++)
            {
                TeamMembers[$"jianghu_deathmatch_enemy_team{i}"] = new List<string>();
            }
        }

        private static WildSpawnType GetRandomBotFromPool()
        {
            if (WeightedBotPool.Count == 0)
            {
                WeightedBotPool = DeathMatchShared.GetWeightedBotPool();
            }

            if (WeightedBotPool.Count == 0)
            {
                return UniversalBotSpawner.allBosses[UnityEngine.Random.Range(0, UniversalBotSpawner.allBosses.Length)];
            }

            return WeightedBotPool[UnityEngine.Random.Range(0, WeightedBotPool.Count)];
        }

        public static async Task SpawnTeamBots(string teamMarker, int count)
        {
            if (WeightedBotPool.Count == 0)
                WeightedBotPool = DeathMatchShared.GetWeightedBotPool();

            for (int i = 0; i < count; i++)
            {
                await SpawnBotForTeam(teamMarker);
                await Task.Delay(500); 
            }
        }


        private static List<ISpawnPoint> GetValidSpawnPointsForTeam(string teamMarker, float minDistance = 10f)
        {
            var validPoints = new List<ISpawnPoint>();
            var botGame = Singleton<IBotGame>.Instance;
            if (botGame == null || botGame.BotsController == null) return validPoints;

            var spawner = botGame.BotsController.BotSpawner;
            var allZones = spawner.AllBotZones;
            if (allZones == null) return validPoints;

            var enemyPlayers = GetEnemyPlayersForTeam(teamMarker);

            foreach (var zone in allZones)
            {
                if (zone == null || zone.SpawnPointMarkers == null) continue;

                foreach (var marker in zone.SpawnPointMarkers)
                {
                    if (marker?.SpawnPoint == null) continue;

                    Vector3 spawnPos = marker.SpawnPoint.Position;
                    bool isValid = true;

                    foreach (var enemy in enemyPlayers)
                    {
                        float distance = Vector3.Distance(spawnPos, enemy.Position);
                        if (distance < minDistance)
                        {
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        validPoints.Add(marker.SpawnPoint);
                    }
                }
            }

            return validPoints;
        }

        private static BotZone GetZoneForSpawnPoint(ISpawnPoint spawnPoint)
        {
            var botGame = Singleton<IBotGame>.Instance;
            if (botGame == null || botGame.BotsController == null) return null;

            var allZones = botGame.BotsController.BotSpawner.AllBotZones;
            if (allZones == null) return null;

            foreach (var zone in allZones)
            {
                if (zone?.SpawnPointMarkers == null) continue;

                foreach (var marker in zone.SpawnPointMarkers)
                {
                    if (marker?.SpawnPoint != null && marker.SpawnPoint == spawnPoint)
                    {
                        return zone;
                    }
                }
            }

            return null;
        }

        private static List<Player> GetEnemyPlayersForTeam(string teamMarker)
        {
            var enemies = new List<Player>();
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld?.AllAlivePlayersList == null) return enemies;

            foreach (Player player in gameWorld.AllAlivePlayersList)
            {
                if (player.IsYourPlayer) continue;

                var botOwner = player.AIData?.BotOwner;
                if (botOwner == null) continue;

                var spawnData = botOwner.SpawnProfileData as BotProfileDataClass;
                string spawnId = spawnData?.SpawnParams?.Id_spawn;
                if (string.IsNullOrEmpty(spawnId)) continue;

                string otherTeamMarker = spawnId.Split('|')[0];

                if (otherTeamMarker != teamMarker &&
                    (teamMarker != "jianghu_deathmatch_teammate" ||
                     !otherTeamMarker.StartsWith("jianghu_deathmatch_teammate")))
                {
                    enemies.Add(player);
                }
            }

            return enemies;
        }

        public static async Task SpawnBotForTeam(string teamMarker)
        {
            try
            {
                var botGame = Singleton<IBotGame>.Instance;
                if (botGame == null || botGame.BotsController == null) return;

                var spawner = botGame.BotsController.BotSpawner;
                if (spawner == null) return;

                var validSpawnPoints = GetValidSpawnPointsForTeam(teamMarker);
                if (validSpawnPoints.Count == 0)
                {
                    return;
                }

                var spawnPoint = validSpawnPoints[UnityEngine.Random.Range(0, validSpawnPoints.Count)];
                var spawnZone = GetZoneForSpawnPoint(spawnPoint);
                if (spawnZone == null) return;

                var botType = GetRandomBotFromPool();

                var spawnParams = new BotSpawnParams()
                {
                    ShallBeGroup = new ShallBeGroupParams(false, true, 1),
                    Id_spawn = $"{teamMarker}|{Guid.NewGuid()}",
                };

                if (!TeamMembers.ContainsKey(teamMarker))
                    TeamMembers[teamMarker] = new List<string>();
                TeamMembers[teamMarker].Add(spawnParams.Id_spawn);

                var profileData = new BotProfileDataClass(
                    EPlayerSide.Savage,
                    botType,
                    BotDifficulty.hard,
                    0f,
                    spawnParams,
                    false
                );

                var creationData = await BotCreationDataClass.Create(
                    profileData,
                    spawner.BotCreator,
                    1,
                    spawner
                );

                if (creationData == null) return;

                var pointsToSpawn = new List<ISpawnPoint> { spawnPoint };
                spawner.TryToSpawnInZoneAndDelay(
                    spawnZone,
                    creationData,
                    false,
                    true,
                    pointsToSpawn,
                    true
                );

                string botName = UniversalBotSpawner.GetBotDisplayName(botType);
                string teamName = teamMarker == "jianghu_deathmatch_teammate" ? "Teammate" :
                                 teamMarker.Replace("jianghu_deathmatch_enemy_", "");

                NotificationManagerClass.DisplayMessageNotification(
                    $"{teamName} {botName} spawned",
                    ENotificationDurationType.Default,
                    ENotificationIconType.Quest,
                    DeathMatchShared.GetTeamColor(teamMarker)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m[DeathMatch] Spawn error: {ex.Message}\x1b[0m");
            }
        }

        public static void OnRaidStarted()
        {
            if (!InitialSpawnDone)
            {
                InitializeTeams();
                InitialSpawnDone = true;
            }
        }

        public static void SpawnInitialTeams()
        {
            if (!DeathMatchCore.DeathMatchModeActive) return;

            try
            {
                int ourSquad = DeathMatchShared.GetOurSquadNumber();
                SpawnTeamBots("jianghu_deathmatch_teammate", ourSquad).ContinueWith(t => { });

                int enemySquad = DeathMatchShared.GetEnemySquadNumber();
                int enemyTeamCount = DeathMatchShared.GetEnemyTeamCount();

                DeathMatchCore.Instance.StartCoroutine(StaggeredEnemySpawn(enemyTeamCount, enemySquad));
            }
            catch { }
        }

        private static System.Collections.IEnumerator StaggeredEnemySpawn(int enemyTeamCount, int enemySquad)
        {
            for (int teamIndex = 1; teamIndex <= enemyTeamCount; teamIndex++)
            {
                yield return new WaitForSeconds(5f); // Wait 5 seconds between teams

                string teamMarker = DeathMatchShared.GetTeamMarker(teamIndex);
                SpawnTeamBots(teamMarker, enemySquad).ContinueWith(t => { });
            }
        }



        public static void StartSquadMaintenance()
        {
            if (DeathMatchCore.Instance != null)
            {
                DeathMatchCore.Instance.StartCoroutine(SquadMaintenanceRoutine());
            }
        }

        private static System.Collections.IEnumerator SquadMaintenanceRoutine()
        {            
            yield return new WaitForSeconds(180f); // Wait time before starting

            while (DeathMatchCore.DeathMatchModeActive)
            {
                yield return new WaitForSeconds(180f); // interval

                try
                {
                    MaintainSquadSizes();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\x1b[31m[DeathMatch] Squad maintenance error: {ex.Message}\x1b[0m");
                }
            }
        }

        private static void MaintainSquadSizes()
        {
            if (!DeathMatchCore.DeathMatchModeActive) return;

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return;

            Dictionary<string, int> aliveCounts = new Dictionary<string, int>();
            Dictionary<string, List<Player>> teamAlivePlayers = new Dictionary<string, List<Player>>();

            foreach (var team in TeamMembers.Keys)
            {
                aliveCounts[team] = 0;
                teamAlivePlayers[team] = new List<Player>();
            }

            if (gameWorld.AllAlivePlayersList != null)
            {
                foreach (Player player in gameWorld.AllAlivePlayersList)
                {
                    if (player.IsYourPlayer) continue;

                    var botOwner = player.AIData?.BotOwner;
                    if (botOwner == null) continue;

                    var spawnData = botOwner.SpawnProfileData as BotProfileDataClass;
                    string spawnId = spawnData?.SpawnParams?.Id_spawn;
                    if (string.IsNullOrEmpty(spawnId)) continue;

                    string teamMarker = spawnId.Split('|')[0];
                    if (TeamMembers.ContainsKey(teamMarker))
                    {
                        aliveCounts[teamMarker]++;
                        teamAlivePlayers[teamMarker].Add(player);
                    }
                }
            }

            Dictionary<string, int> teamsNeedingBots = new Dictionary<string, int>();

            foreach (var team in TeamMembers.Keys)
            {
                int targetSquadSize = team == "jianghu_deathmatch_teammate"
                    ? DeathMatchShared.GetOurSquadNumber()
                    : DeathMatchShared.GetEnemySquadNumber();

                int alive = aliveCounts[team];
                int difference = targetSquadSize - alive;

                if (difference > 0)
                {
                    teamsNeedingBots[team] = difference;
                }
                else if (difference < 0)
                {
                    int toRemove = -difference;
                    var playersToRemove = teamAlivePlayers[team]
                        .Take(toRemove)
                        .ToList();

                    foreach (var player in playersToRemove)
                    {
                        var botOwner = player.AIData?.BotOwner;
                        if (botOwner != null)
                        {
                            botOwner.LeaveData.RemoveFromMap();
                            UnityEngine.Object.Destroy(botOwner.gameObject);
                        }
                    }
                }
            }
            if (teamsNeedingBots.Count > 0)
            {
                DeathMatchCore.Instance.StartCoroutine(StaggeredMaintenanceSpawn(teamsNeedingBots));
            }
        }

        private static System.Collections.IEnumerator StaggeredMaintenanceSpawn(Dictionary<string, int> teamsToSpawn)
        {
            foreach (var kvp in teamsToSpawn)
            {
                string team = kvp.Key;
                int count = kvp.Value;

                SpawnBotForTeam(team).ContinueWith(t => { });

                yield return new WaitForSeconds(5f);
            }
        }

        public static void OnBotKilled(string teamMarker, string botId)
        {
            if (!DeathMatchCore.DeathMatchModeActive) return;

            if (TeamMembers.ContainsKey(teamMarker))
            {
                TeamMembers[teamMarker].Remove(botId);
            }

            if (!teamDeaths.ContainsKey(teamMarker))
                teamDeaths[teamMarker] = 0;
            teamDeaths[teamMarker]++;

            SpawnBotForTeam(teamMarker).ContinueWith(t => { });
        }


        public static string GetTeamMarkerFromSpawnId(string spawnId)
        {
            if (spawnId.StartsWith("jianghu_deathmatch_teammate"))
                return "jianghu_deathmatch_teammate";

            if (spawnId.StartsWith("jianghu_deathmatch_enemy_team"))
            {
                string marker = spawnId;
                int pipeIndex = marker.IndexOf('|');
                if (pipeIndex > 0)
                {
                    marker = marker.Substring(0, pipeIndex);
                }
                return marker;
            }

            return null;
        }

        public static int GetTeamDeaths(string teamMarker)
        {
            if (teamDeaths.ContainsKey(teamMarker))
                return teamDeaths[teamMarker];
            return 0;
        }
    }

    [HarmonyPatch]
    public class DeathMatchBotSpawnPatches
    {
        [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
        class DeathMatchBotSpawnGameStartPatch
        {
            static void Postfix()
            {
                try
                {
                    if (DeathMatchCore.DeathMatchModeActive && !DeathMatchBotSpawn.InitialSpawnDone)
                    {
                        var gameWorld = Singleton<GameWorld>.Instance;
                        if (gameWorld != null)
                        {
                            gameWorld.StartCoroutine(DelayedTeamSpawn());
                        }
                    }
                }
                catch { }
            }

            private static System.Collections.IEnumerator DelayedTeamSpawn()
            {
                yield return new WaitForSeconds(1f);

                if (!DeathMatchBotSpawn.InitialSpawnDone && DeathMatchCore.DeathMatchModeActive)
                {
                    DeathMatchBotSpawn.OnRaidStarted();
                    DeathMatchBotSpawn.SpawnInitialTeams();
                }
            }
        }

        [HarmonyPatch(typeof(BotSpawner), "method_11")]
        class DeathMatchBotSpawnInterceptPatch
        {
            static bool Prefix(BotOwner bot, BotCreationDataClass data, Action<BotOwner> callback,
                              bool shallBeGroup, Stopwatch stopWatch, BotSpawner __instance)
            {
                if (!DeathMatchCore.DeathMatchModeActive) return true;

                var spawnData = bot.SpawnProfileData as BotProfileDataClass;

                bool isOurBot = spawnData?.SpawnParams?.Id_spawn?.StartsWith("jianghu_") == true ||
                                spawnData?.SpawnParams?.Id_spawn?.StartsWith("JiangHu_Bot|") == true;

                if (!isOurBot)
                {
                    bot.LeaveData.RemoveFromMap();
                    UnityEngine.Object.Destroy(bot.gameObject);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BotSpawner), "BotDied")]
        class DeathMatchBotSpawnOnDeathPatch
        {
            private static HashSet<string> ProcessedBotIds = new HashSet<string>();

            static void Postfix(BotOwner bot)
            {
                if (!DeathMatchCore.DeathMatchModeActive) return;

                try
                {
                    var spawnData = bot.SpawnProfileData as BotProfileDataClass;

                    if (spawnData?.SpawnParams?.Id_spawn == null)
                        return;

                    string spawnId = spawnData.SpawnParams.Id_spawn;

                    if (!spawnId.StartsWith("jianghu_deathmatch_"))
                        return;

                    string botId = bot.Profile.Id;
                    if (ProcessedBotIds.Contains(botId))
                        return;

                    ProcessedBotIds.Add(botId);

                    string teamMarker = DeathMatchBotSpawn.GetTeamMarkerFromSpawnId(spawnId);
                    if (teamMarker != null)
                    {
                        DeathMatchBotSpawn.OnBotKilled(teamMarker, botId);
                    }
                }
                catch { }
            }
        }
    }
    #endregion

    #region UI Systems
    public class DeathMatchUI : MonoBehaviour
    {
        void Start()
        {
            new DeathMatchUIButtonPatch().Enable();
        }
    }

    internal class DeathMatchUIButtonPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod("Show", new[] {
            typeof(Profile),
            typeof(MatchmakerPlayerControllerClass),
            typeof(ESessionMode)
        });
        }

        [PatchPostfix]
        private static void Postfix(MenuScreen __instance)
        {
            __instance.StartCoroutine(AddButtonDelayed(__instance));
        }

        private static System.Collections.IEnumerator AddButtonDelayed(MenuScreen menuScreen)
        {
            yield return new WaitForSeconds(0.1f);

            try
            {
                DeathMatchCore.ValidateStateInMenu();
                var escapeField = typeof(MenuScreen).GetField("_playButton",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var templateButton = (DefaultUIButton)escapeField.GetValue(menuScreen);

                if (templateButton == null)
                {
                    yield break;
                }

                GameObject buttonObj = GameObject.Instantiate(templateButton.gameObject,
                    templateButton.transform.parent);
                buttonObj.name = "DeathMatchButton";

                RectTransform rt = buttonObj.GetComponent<RectTransform>();

                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-10f, 200f);

                rt.localScale = new Vector3(0.75f, 0.75f, 0.75f);

                DefaultUIButton button = buttonObj.GetComponent<DefaultUIButton>();

                var rawTextField = typeof(DefaultUIButton).GetField("_rawText",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                rawTextField.SetValue(button, true);

                var textField = typeof(DefaultUIButton).GetField("_text",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                textField.SetValue(button, "<color=#f9d367>樂 園</color>");

                var fontSizeField = typeof(DefaultUIButton).GetField("_fontSize",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                fontSizeField.SetValue(button, 42);

                button.method_9();

                var allTexts = buttonObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in allTexts)
                {
                    text.richText = true;
                }

                button.OnClick.RemoveAllListeners();
                button.OnClick.AddListener(() =>
                {
                    DeathMatchCore.EnableDeathMatchMode();

                    NotificationManagerClass.DisplayMessageNotification(
                        "Boss Death Match 头目对决",
                        ENotificationDurationType.Long,
                        ENotificationIconType.Default,
                        new Color(1f, 0.8f, 0f)
                    );

                    if (!TarkovApplication.Exist(out var app))
                    {
                        return;
                    }

                    List<string> enabledMaps = DeathMatchShared.GetEnabledMapsFromConfig();

                    if (enabledMaps.Count == 0)
                    {
                        return;
                    }

                    string randomMap = enabledMaps[UnityEngine.Random.Range(0, enabledMaps.Count)];
                    app.InternalStartGame(randomMap, true, true);
                });
            }
            catch { }
        }
    }
    #endregion

    #region Teleport Command System
    public class TeleportCommand : MonoBehaviour
        {
        private static BotOwner _selectedBot = null;
        private static float _lastActionTime = 0f;
        private const float MIN_PRESS_INTERVAL = 0.3f;

        void Update()
        {
            if (!DeathMatchCore.DeathMatchModeActive) return;

            if (F12Manager.TeleportBotHotkey != null &&
                F12Manager.TeleportBotHotkey.Value.IsUp())
            {
                var player = GamePlayerOwner.MyPlayer;
                if (player != null && player.HealthController.IsAlive)
                {
                    Execute(player);
                }
            }
        }

        public bool Execute(Player requester)
        {
            try
            {
                if (Time.time - _lastActionTime < MIN_PRESS_INTERVAL)
                {
                    return false;
                }
                _lastActionTime = Time.time;

                if (_selectedBot != null && _selectedBot.HealthController.IsAlive)
                {
                    bool success = TeleportSelectedBot(requester);
                    if (success)
                    {
                        _selectedBot = null;
                    }
                    return success;
                }
                else
                {
                    if (_selectedBot != null && !_selectedBot.HealthController.IsAlive)
                    {
                        NotificationManagerClass.DisplayMessageNotification(
                            "Selected bot died 选中对象已凉凉",
                            ENotificationDurationType.Default,
                            ENotificationIconType.Alert,
                            Color.red
                        );
                        _selectedBot = null;
                    }

                    return SelectNewBot(requester);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SelectNewBot(Player requester)
        {
            var bot = GetBotPlayerIsLookingAt(requester);

            if (bot == null)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    "No bot in crosshair 未选中",
                    ENotificationDurationType.Default,
                    ENotificationIconType.Alert,
                    Color.yellow
                );
                return false;
            }

            if (!bot.HealthController.IsAlive)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    "Bot is dead 该目标已凉凉",
                    ENotificationDurationType.Default,
                    ENotificationIconType.Alert,
                    Color.red
                );
                return false;
            }

            _selectedBot = bot;

            NotificationManagerClass.DisplayMessageNotification(
                $"Selected {bot.Profile.Nickname}\nPress again to teleport 选中啦，再按一次放置",
                ENotificationDurationType.Default,
                ENotificationIconType.Quest,
                Color.green
            );

            return true;
        }

        private bool TeleportSelectedBot(Player requester)
        {
            try
            {
                Vector3 teleportPos = GetCrosshairPosition(requester);

                if (teleportPos == Vector3.zero)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        "No valid teleport location 该地点无法放置",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Alert,
                        Color.yellow
                    );
                    return false;
                }

                bool success = ExecuteTeleport(_selectedBot, teleportPos);

                string message = success ? $"Teleported {_selectedBot.Profile.Nickname} 已放置" : "Teleport failed 放置失败";

                NotificationManagerClass.DisplayMessageNotification(
                    message,
                    ENotificationDurationType.Default,
                    success ? ENotificationIconType.Quest : ENotificationIconType.Alert,
                    success ? Color.green : Color.red
                );

                return success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Vector3 GetCrosshairPosition(Player requester)
        {
            try
            {
                Vector3 eyePos;
                Vector3 lookDir;

                FreeCameraController freeCam = FreeCameraController.Instance;
                if (freeCam != null && freeCam.IsInFreecam() && freeCam.GetVirtualCameraObject() != null)
                {
                    Transform virtualCam = freeCam.GetVirtualCameraObject().transform;
                    eyePos = virtualCam.position;
                    lookDir = virtualCam.forward;
                }
                else
                {
                    eyePos = requester.PlayerBones.Head.position;
                    lookDir = requester.LookDirection;
                }

                float maxDistance = 100f;
                LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;
                Ray ray = new Ray(eyePos, lookDir);

                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask))
                    return hit.point + Vector3.up * 0.5f;

                return eyePos + lookDir * maxDistance;
            }
            catch (Exception)
            {
                return Vector3.zero;
            }
        }

        private bool ExecuteTeleport(BotOwner bot, Vector3 teleportPos)
        {
            try
            {
                NavMeshHit hit;
                if (!NavMesh.SamplePosition(teleportPos, out hit, 2f, -1))
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        "No navmesh at target location 该地点无人机路径",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Alert,
                        Color.yellow
                    );
                    return false;
                }

                Vector3 navmeshPos = hit.position + Vector3.up * 0.5f;

                if (bot.Mover != null)
                {
                    bot.Mover.Stop();
                    bot.Mover.AllowTeleport();
                    bot.Mover.PrevSuccessLinkedFrom_1 = navmeshPos;
                    bot.Mover.PrevPosLinkedTime_1 = Time.time;
                    bot.Mover.LastGoodCastPoint = navmeshPos;
                    bot.Mover.LastGoodCastPointTime = Time.time;
                    bot.Mover.PositionOnWayInner = navmeshPos;
                    bot.Mover.LinkedToNavmeshInitially = true;
                    bot.Mover.PositionOnWayCasted = navmeshPos;
                    bot.Mover.Next = Time.time + 2f;
                    bot.Mover.PrevOffsetGoodCasted = Vector3.zero;
                    bot.Mover.PrevOffsetGoodCastedTime = Time.time;
                    bot.Mover.PrevLinkPos = navmeshPos;
                    bot.Mover.SetPointOnWay(navmeshPos);
                }

                bot.GetPlayer.Teleport(navmeshPos, true);

                NotificationManagerClass.DisplayMessageNotification(
                    $"Teleported {bot.Profile.Nickname}",
                    ENotificationDurationType.Default,
                    ENotificationIconType.Quest,
                    Color.green
                );

                return true;
            }
            catch (Exception ex)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    $"Teleport failed: {ex.Message}",
                    ENotificationDurationType.Default,
                    ENotificationIconType.Alert,
                    Color.red
                );
                return false;
            }
        }

        private BotOwner GetBotPlayerIsLookingAt(Player player, float maxDistance = 50f)
        {
            try
            {
                FreeCameraController freeCam = FreeCameraController.Instance;
                bool useFreecam = freeCam != null && freeCam.IsInFreecam();

                Vector3 eyePos;
                Vector3 lookDir;

                if (useFreecam && freeCam.GetVirtualCameraObject() != null)
                {
                    Transform virtualCam = freeCam.GetVirtualCameraObject().transform;
                    eyePos = virtualCam.position;
                    lookDir = virtualCam.forward;
                }
                else
                {
                    eyePos = player.PlayerBones.Head.position;
                    lookDir = player.LookDirection;
                }

                LayerMask shootMask = LayerMaskClass.HighPolyWithTerrainMask | (1 << LayerMaskClass.PlayerLayer);
                Ray ray = new Ray(eyePos, lookDir);

                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, shootMask))
                {
                    Player hitPlayer = hit.transform.GetComponentInParent<Player>();
                    if (hitPlayer != null && hitPlayer.ProfileId != player.ProfileId)
                    {
                        float distance = Vector3.Distance(eyePos, hitPlayer.Position);
                        if (distance <= maxDistance)
                        {
                            BotOwner bot = hitPlayer.AIData?.BotOwner;
                            if (bot != null && bot.HealthController.IsAlive)
                            {
                                return bot;
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
    #endregion

    #region Free Camera System
    public class FreeCameraController : MonoBehaviour
    {
        private Camera _mainCamera;
        private Player _player;
        private EftGamePlayerOwner _playerOwner;
        private GameObject _virtualCameraObject;
        private FreeCamera _freeCameraComponent;
        private bool _isInFreecam = false;

        public float MoveSpeed = 7f;
        public float FastMoveSpeed = 15f;
        public float LookSpeed = 1.5f;
        public float UpDownSpeed = 2f;

        public bool IsInFreecam() => _isInFreecam;
        public GameObject GetVirtualCameraObject() => _virtualCameraObject;
        public static FreeCameraController Instance;

        void Start()
        {
            StartCoroutine(WaitForCamera());
            Instance = this;
        }

        private System.Collections.IEnumerator WaitForCamera()
        {
            while (CameraClass.Instance == null || CameraClass.Instance.Camera == null)
                yield return new WaitForSeconds(0.1f);
            _mainCamera = CameraClass.Instance.Camera;
        }

        void Update()
        {
            if (!DeathMatchCore.DeathMatchModeActive) return;
            if (F12Manager.FreeCameraHotkey != null &&
                F12Manager.FreeCameraHotkey.Value.IsUp())
                ToggleFreeCamera();
        }

        void LateUpdate()
        {
            if (_isInFreecam && _mainCamera != null && _virtualCameraObject != null)
            {
                _mainCamera.transform.position = _virtualCameraObject.transform.position;
                _mainCamera.transform.rotation = _virtualCameraObject.transform.rotation;
                var camController = _player.GetComponent<PlayerCameraController>();
                if (camController != null) camController.enabled = false;
            }
        }

        private void ToggleFreeCamera()
        {
            _player = GamePlayerOwner.MyPlayer;
            if (_player == null || _player.HealthController == null || !_player.HealthController.IsAlive)
                return;

            if (_mainCamera == null)
            {
                _mainCamera = CameraClass.Instance?.Camera;
                if (_mainCamera == null) return;
            }

            _playerOwner = _player.GetComponent<EftGamePlayerOwner>();
            if (_playerOwner == null) return;

            if (!_isInFreecam)
            {
                _player.PointOfView = EPointOfView.FreeCamera;
                StartOurFreecamSystem();
            }
            else
            {
                _player.PointOfView = EPointOfView.FirstPerson;
                StopOurFreecamSystem();
            }
        }

        private void StartOurFreecamSystem()
        {
            _isInFreecam = true;
            CreateVirtualCamera();
            if (_freeCameraComponent != null)
                _freeCameraComponent.method_0();

            if (_playerOwner != null)
                _playerOwner.enabled = false;

            NotificationManagerClass.DisplayMessageNotification(
                "Free Camera Active 自由视角开启",
                ENotificationDurationType.Default,
                ENotificationIconType.Quest,
                Color.green
            );
        }

        private void StopOurFreecamSystem()
        {
            _isInFreecam = false;

            var camController = _player.GetComponent<PlayerCameraController>();
            if (camController != null)
            {
                camController.enabled = true;
                camController.InitiateOperation<FirstPersonCameraOperationClass>();
            }

            if (_playerOwner != null) _playerOwner.enabled = true;

            if (Singleton<CommonUI>.Instance != null &&
                Singleton<CommonUI>.Instance.EftBattleUIScreen != null &&
                Singleton<CommonUI>.Instance.EftBattleUIScreen.CanvasGroup != null)
            {
                Singleton<CommonUI>.Instance.EftBattleUIScreen.CanvasGroup.gameObject.SetActive(true);
            }

            if (_freeCameraComponent != null)
            {
                _freeCameraComponent.method_1();
                Destroy(_freeCameraComponent);
                _freeCameraComponent = null;
            }

            DestroyVirtualCamera();

            NotificationManagerClass.DisplayMessageNotification(
                "Returned to First Person 已回到正常视角",
                ENotificationDurationType.Default,
                ENotificationIconType.Quest,
                Color.green
            );
        }

        private void CreateVirtualCamera()
        {
            if (_mainCamera == null)
            {
                _mainCamera = CameraClass.Instance?.Camera;
                if (_mainCamera == null) return;
            }
            _virtualCameraObject = new GameObject("FreeCamera_VirtualObject");
            _virtualCameraObject.transform.position = _mainCamera.transform.position;
            _virtualCameraObject.transform.rotation = _mainCamera.transform.rotation;

            _freeCameraComponent = _virtualCameraObject.AddComponent<FreeCamera>();
            _freeCameraComponent.enableInputCapture = true;
            _freeCameraComponent.holdRightMouseCapture = true;
            _freeCameraComponent.lookSpeed = LookSpeed;
            _freeCameraComponent.moveSpeed = MoveSpeed;
            _freeCameraComponent.sprintSpeed = FastMoveSpeed;
        }

        public void ResetFreeCamera()
        {
            _isInFreecam = false;
        }

        private void DestroyVirtualCamera()
        {
            if (_virtualCameraObject != null)
            {
                Destroy(_virtualCameraObject);
                _virtualCameraObject = null;
            }
        }

        void OnDestroy()
        {
            DestroyVirtualCamera();
            if (Instance == this)
                Instance = null;
        }
    }
    #endregion
}