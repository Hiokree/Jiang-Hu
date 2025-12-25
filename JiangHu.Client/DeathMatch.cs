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

namespace JiangHu
{
    public class DeathMatch : MonoBehaviour
    {
        public static DeathMatch Instance { get; private set; }
        public static bool DeathMatchModeActive = false;
        public int teleportCount = 0;

        public void Init()
        {
        }

        public static void EnableDeathMatchMode()
        {
            DeathMatchModeActive = true;
            BossSpawnSystem.EnableBossSpawnSystem();
            BossSpawnSystem.initialSpawnDone = false;
        }

        public static void DisableDeathMatchMode()
        {
            DeathMatchModeActive = false;
            BossSpawnSystem.systemActive = false;
        }

        void Update()
        {
            player = Singleton<GameWorld>.Instance?.MainPlayer;
        }

        private Player player;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void ValidateStateInMenu()
        {
            try
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                bool isInMenu = gameWorld == null;

                if (isInMenu && DeathMatchModeActive)
                {
                    DeathMatch.DisableDeathMatchMode();

                    if (Instance != null)
                    {
                        Instance.teleportCount = 0;
                    }
                }
            }
            catch { }
        }


        public bool TryStartDeathTeleport(Player targetPlayer, EDamageType damageType)
        {
            int maxLives = DeathMatchButtonPatch.GetDeathMatchLivesFromConfig();
            if (teleportCount >= maxLives)
            {
                return false;
            }

            teleportCount++;

            targetPlayer.ActiveHealthController.IsAlive = true;
            targetPlayer.ActiveHealthController.SetDamageCoeff(0f);

            player = targetPlayer;
            Vector3 spawnPosition = GetRandomSpawnPosition();
            if (spawnPosition != Vector3.zero)
            {
                TeleportPlayer(spawnPosition);
                RestorePlayerHealth();
                player.ActiveHealthController.SetDamageCoeff(1f);

                int remaining = DeathMatchButtonPatch.GetDeathMatchLivesFromConfig() - teleportCount;
                string message = remaining > 0
                    ? $"Teleported! {remaining} left"
                    : "Teleported!";

                NotificationManagerClass.DisplayMessageNotification(
                    message,
                    ENotificationDurationType.Default,
                    ENotificationIconType.Default,
                    Color.yellow
                );
            }
            return true;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            if (player == null) return Vector3.zero;

            SpawnPointMarker[] allMarkers = UnityEngine.Object.FindObjectsOfType<SpawnPointMarker>();
            if (allMarkers == null || allMarkers.Length == 0) return Vector3.zero;

            Vector3 playerPos = player.Position;
            const float excludeRadius = 60f;
            float excludeRadiusSquared = excludeRadius * excludeRadius;

            var validSpawnPoints = new System.Collections.Generic.List<ISpawnPoint>();

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

        private void TeleportPlayer(Vector3 position)
        {
            if (player == null) return;

            Vector2 currentLookDirection = player.Rotation;
            player.Teleport(position, true);
            player.Rotation = currentLookDirection;
        }

        private void RestorePlayerHealth()
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
                catch (Exception ex)
                {
                    Console.WriteLine($"🎮 [Jiang Hu] ERROR {bodyPart}: {ex.Message}");
                }
            }
        }

        public void ResetTeleportState()
        {
            teleportCount = 0;
        }
    }

    [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.Kill))]
    class DeathTeleportPatch
    {
        static bool Prefix(ActiveHealthController __instance, EDamageType damageType)
        {
            if (!DeathMatch.DeathMatchModeActive) return true;

            try
            {
                FieldInfo playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");
                if (playerField?.GetValue(__instance) is not Player player || player.IsAI)
                    return true;

                if (!player.IsYourPlayer)
                    return true;

                DeathMatch teleporter = DeathMatch.Instance;
                if (teleporter == null)
                    return true;

                bool teleportStarted = teleporter.TryStartDeathTeleport(player, damageType);
                return !teleportStarted;
            }
            catch { return true; }
        }
    }


    [HarmonyPatch]
    public class BotHostilityPatch
    {
        [HarmonyPatch(typeof(BotOwner), "method_10")]
        class BotActivatePatch
        {
            static void Postfix(BotOwner __instance)
            {
                try
                {
                    var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                    if (mainPlayer == null)
                    {
                        return;
                    }

                    // Check if it's OUR spawned Bots (by marker)
                    var marker = __instance.gameObject.GetComponent<JiangHuBotMarker>();
                    bool isOurBot = marker != null;


                    if (isOurBot)
                    {
                        // 1. Friendly to player
                        __instance.BotsGroup.RemoveEnemy(mainPlayer);
                        if (mainPlayer.BotsGroup != null)
                            mainPlayer.BotsGroup.RemoveEnemy(__instance);

                        // 2. Friendly to other OUR Bots
                        var botGame = Singleton<IBotGame>.Instance;
                        if (botGame != null)
                        {
                            foreach (var otherBot in botGame.BotsController.Bots.BotOwners)
                            {
                                if (otherBot == null || otherBot.Profile.Id == __instance.Profile.Id)
                                    continue;

                                var otherMarker = otherBot.gameObject.GetComponent<JiangHuBotMarker>();
                                if (otherMarker != null)
                                {
                                    // OUR Bot ↔ OUR Bot: Friendly
                                    __instance.BotsGroup.RemoveEnemy(otherBot);
                                    otherBot.BotsGroup.RemoveEnemy(__instance);
                                }
                                else
                                {
                                    // OUR Bot ↔ Others: Hostile
                                    if (!__instance.BotsGroup.Enemies.ContainsKey(otherBot))
                                        __instance.BotsGroup.AddEnemy(otherBot, EBotEnemyCause.initial);
                                }
                            }
                        }
                        return; // ⚡ OUR Bots handled, skip vanilla logic
                    }

                    // ⚡ VANILLA BOTS: Different rules per raid type
                    var botRole = __instance.Profile.Info.Settings.Role;
                    bool isBoss = UniversalBotSpawner.allBosses != null &&
                                  UniversalBotSpawner.allBosses.Contains(botRole);


                    if (DeathMatch.DeathMatchModeActive)
                    {
                        // DEATHMATCH RAID: Boss-vs-Player hostility
                        if (mainPlayer != null && isBoss)
                        {
                            // Boss ↔ Player: Hostile
                            __instance.BotsGroup.AddEnemy(mainPlayer, EBotEnemyCause.initial);
                        }

                        // Boss ↔ Boss: Friendly
                        var botGameDM = Singleton<IBotGame>.Instance;
                        if (botGameDM != null && isBoss)
                        {
                            var otherBosses = botGameDM.BotsController.Bots.BotOwners
                                .Where(b => b != null &&
                                       b.BotState != EBotState.Disposed &&
                                       b.BotState != EBotState.NonActive &&
                                       b.Profile.Id != __instance.Profile.Id &&
                                       UniversalBotSpawner.allBosses.Contains(b.Profile.Info.Settings.Role))
                                .ToList();

                            foreach (var otherBoss in otherBosses)
                            {
                                __instance.BotsGroup.RemoveEnemy(otherBoss);
                                otherBoss.BotsGroup.RemoveEnemy(__instance);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [BotHostilityPatch] Postfix() - EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }


    public class BossSpawnSystem
    {
        public static List<WildSpawnType> bossQueue = new List<WildSpawnType>();
        public static int currentSpawnIndex = 0;
        private static int bossesKilled = 0;
        public static bool systemActive = false;
        public static bool initialSpawnDone = false;



        public static void EnableBossSpawnSystem()
        {
            if (!systemActive)
            {
                systemActive = true;
            }
        }

        public static void InitializeBossQueue()
        {
            bossQueue = UniversalBotSpawner.allBosses.ToList();
            for (int i = 0; i < bossQueue.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, bossQueue.Count);
                var temp = bossQueue[i];
                bossQueue[i] = bossQueue[j];
                bossQueue[j] = temp;
            }
            currentSpawnIndex = 0;
            bossesKilled = 0;
        }

        public static async Task SpawnNextBoss()
        {
            int attemptIndex = currentSpawnIndex;
            try
            {
                if (currentSpawnIndex >= bossQueue.Count)
                {
                    InitializeBossQueue();
                    attemptIndex = 0;
                }

                var botGame = Singleton<IBotGame>.Instance;
                if (botGame == null || botGame.BotsController == null) return;

                var spawner = botGame.BotsController.BotSpawner;
                if (spawner == null) return;

                var nextBoss = bossQueue[attemptIndex];

                var profileData = new BotProfileDataClass(
                    EPlayerSide.Savage,
                    nextBoss,
                    BotDifficulty.hard,
                    0f,
                    new BotSpawnParams()
                    {
                        ShallBeGroup = new ShallBeGroupParams(true, true, 1),
                        Id_spawn = $"JiangHu_DeathMatch|{Guid.NewGuid()}"
                    },
                    false
                );

                var spawnZone = spawner.GetRandomBotZone(false); 
                if (spawnZone != null)
                {
                    var bossLocationSpawn = new BossLocationSpawn()
                    {
                        BossZone = "",
                        Time = 1f,
                        Delay = 0f,
                        TriggerId = $"JiangHu_DeathMatch|{Guid.NewGuid()}",
                        TriggerName = "",
                        BossChance = 100f,
                        BossName = nextBoss.ToString(),
                        BossDifficult = BotDifficulty.hard.ToString(),
                        BossEscortAmount = "0",
                        BossEscortDifficult = BotDifficulty.normal.ToString(),
                        BossEscortType = WildSpawnType.followerBully.ToString(),
                        ForceSpawn = true,
                        IgnoreMaxBots = true
                    };

                    bossLocationSpawn.ParseMainTypesTypes();
                    spawner.ActivateBotsByWave(bossLocationSpawn);
                }
                currentSpawnIndex = attemptIndex + 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🎮 [Jiang Hu] SpawnNextBoss error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static void OnRaidStarted()
        {
            if (!initialSpawnDone)
            {
                InitializeBossQueue();
                initialSpawnDone = true;
            }
        }

        public static void OnBossKilled(WildSpawnType bossType)
        {
            if (!DeathMatch.DeathMatchModeActive) return;

            bossesKilled++;
            SpawnNextBoss().ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Console.WriteLine($"🎮 [Jiang Hu] OnBossKilled spawn failed: {task.Exception}");
            });
        }

        public static WildSpawnType? GetNextBossForWave()
        {
            if (currentSpawnIndex < bossQueue.Count)
                return bossQueue[currentSpawnIndex];
            return null;
        }
    }

    [HarmonyPatch]
    public class BossSpawnPatches
    {
        [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
        class DeathMatchGameStartPatch
        {
            static void Postfix()
            {
                try
                {
                    if (DeathMatch.DeathMatchModeActive && !BossSpawnSystem.initialSpawnDone)
                    {
                        var gameWorld = Singleton<GameWorld>.Instance;
                        if (gameWorld != null)
                        {
                            gameWorld.StartCoroutine(DelayedDirectSpawn());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🎮 [DeathMatch] Game start error: {ex.Message}");
                }
            }

            private static System.Collections.IEnumerator DelayedDirectSpawn()
            {
                yield return new WaitForSeconds(1f);

                if (!BossSpawnSystem.initialSpawnDone && DeathMatch.DeathMatchModeActive)
                {
                    BossSpawnSystem.OnRaidStarted();

                    int startingBots = DeathMatchButtonPatch.GetStartingBotsFromConfig();

                    for (int i = 0; i < startingBots; i++)
                    {
                        BossSpawnSystem.SpawnNextBoss().ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                                Console.WriteLine($"🎮 [DeathMatch] Initial spawn failed: {task.Exception}");
                        });
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BotSpawner), "method_11")]
        class FinalSpawnInterceptPatch
        {
            static bool Prefix(BotOwner bot, BotCreationDataClass data, Action<BotOwner> callback,
                              bool shallBeGroup, Stopwatch stopWatch, BotSpawner __instance)
            {
                if (!DeathMatch.DeathMatchModeActive &&
                    !F12Manager.DisableVanillaBotSpawn.Value)
                    return true;

                var spawnData = bot.SpawnProfileData as BotProfileDataClass;

                bool isOurBot = spawnData?.SpawnParams?.Id_spawn != null &&
                               spawnData.SpawnParams.Id_spawn.StartsWith("JiangHu_");

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
        class SpawnNextBossOnDeathPatch
        {
            private static HashSet<string> processedBotIds = new HashSet<string>();

            static void Postfix(BotOwner bot)
            {
                if (!DeathMatch.DeathMatchModeActive) return;

                try
                {
                    var spawnData = bot.SpawnProfileData as BotProfileDataClass;

                    // 1. Must be OUR DeathMatch bot
                    if (spawnData?.SpawnParams?.Id_spawn?.StartsWith("JiangHu_DeathMatch") != true)
                        return;

                    // 2. Must be a boss type
                    var role = bot.Profile.Info.Settings.Role;
                    if (!UniversalBotSpawner.allBosses.Contains(role))
                        return;

                    // 3. Duplicate death protection
                    string botId = bot.Profile.Id;
                    if (processedBotIds.Contains(botId))
                        return;

                    processedBotIds.Add(botId);

                    // 4. Spawn next boss
                    BossSpawnSystem.OnBossKilled(role);
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(LocalGame), "Stop")]
    class DeathMatchRaidEndPatch
    {
        static void Postfix(string profileId, ExitStatus exitStatus, string exitName, float delay)
        {
            if (exitStatus != ExitStatus.Transit)
            {
                DeathMatch.DisableDeathMatchMode();
            }
        }
    }

    internal class DeathMatchButtonPatch : ModulePatch
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
                DeathMatch.ValidateStateInMenu();
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
                    DeathMatch.EnableDeathMatchMode();

                    NotificationManagerClass.DisplayMessageNotification(
                        "Boss Death Match 头目对决",
                        ENotificationDurationType.Long,
                        ENotificationIconType.Default,
                        new Color(1f, 0.8f, 0f) // Gold color
                    );

                    if (!TarkovApplication.Exist(out var app))
                    {
                        return;
                    }

                    List<string> enabledMaps = GetEnabledMapsFromConfig();

                    if (enabledMaps.Count == 0)
                    {
                        Console.WriteLine($"🎮 [Jiang Hu] No maps available!");
                        return;
                    }

                    string randomMap = enabledMaps[UnityEngine.Random.Range(0, enabledMaps.Count)];
                    app.InternalStartGame(randomMap, true, true);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🎮 [Jiang Hu] 乐园 button error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static List<string> GetEnabledMapsFromConfig()
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
                    else
                    {
                        Console.WriteLine($"🎮 [Jiang Hu] Config dict is null");
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
            catch (Exception ex)
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
            return 10;
        }

        public static int GetStartingBotsFromConfig()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (configDict != null && configDict.ContainsKey("DeathMatch_Starting_Bot"))
                    {
                        return Convert.ToInt32(configDict["DeathMatch_Starting_Bot"]);
                    }
                }
            }
            catch { }
            return 5;
        }
    }
}