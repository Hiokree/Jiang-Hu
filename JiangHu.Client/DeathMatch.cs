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
            Console.WriteLine($"🎮 [Jiang Hu] DeathMatch mode enabled");
        }

        public static void DisableDeathMatchMode()
        {
            DeathMatchModeActive = false;
            Console.WriteLine($"🎮 [Jiang Hu] DeathMatch mode disabled");
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
                    Console.WriteLine($"🎮 [BotHostilityPatch] Postfix() - Bot activating: {__instance.Profile?.Id}");

                    var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                    if (mainPlayer == null)
                    {
                        Console.WriteLine($"❌ [BotHostilityPatch] No main player");
                        return;
                    }

                    // Check if it's OUR PMC (by marker)
                    var marker = __instance.gameObject.GetComponent<OurPMCMarker>();
                    bool isOurPMC = marker != null;

                    Console.WriteLine($"🎮 [BotHostilityPatch] isOurPMC: {isOurPMC}");
                    Console.WriteLine($"🎮 [BotHostilityPatch] Role: {__instance.Profile.Info.Settings.Role}");
                    Console.WriteLine($"🎮 [BotHostilityPatch] DeathMatchModeActive: {DeathMatch.DeathMatchModeActive}");

                    if (isOurPMC)
                    {
                        Console.WriteLine($"✅ [BotHostilityPatch] Configuring OUR PMC...");

                        // 1. Friendly to player
                        __instance.BotsGroup.RemoveEnemy(mainPlayer);
                        if (mainPlayer.BotsGroup != null)
                            mainPlayer.BotsGroup.RemoveEnemy(__instance);
                        Console.WriteLine($"✅ [BotHostilityPatch] OUR PMC friendly to player");

                        // 2. Friendly to other OUR PMCs
                        var botGame = Singleton<IBotGame>.Instance;
                        if (botGame != null)
                        {
                            foreach (var otherBot in botGame.BotsController.Bots.BotOwners)
                            {
                                if (otherBot == null || otherBot.Profile.Id == __instance.Profile.Id)
                                    continue;

                                var otherMarker = otherBot.gameObject.GetComponent<OurPMCMarker>();
                                if (otherMarker != null)
                                {
                                    // OUR PMC ↔ OUR PMC: Friendly
                                    __instance.BotsGroup.RemoveEnemy(otherBot);
                                    otherBot.BotsGroup.RemoveEnemy(__instance);
                                    Console.WriteLine($"✅ [BotHostilityPatch] OUR PMC friendly to OUR PMC: {otherBot.Profile.Id}");
                                }
                                else
                                {
                                    // OUR PMC ↔ Others: Hostile
                                    if (!__instance.BotsGroup.Enemies.ContainsKey(otherBot))
                                        __instance.BotsGroup.AddEnemy(otherBot, EBotEnemyCause.initial);
                                    Console.WriteLine($"🔥 [BotHostilityPatch] OUR PMC hostile to: {otherBot.Profile.Info.Settings.Role}");
                                }
                            }
                        }
                        return; // ⚡ OUR PMCs handled, skip vanilla logic
                    }

                    // ⚡ VANILLA BOTS: Different rules per raid type
                    var botRole = __instance.Profile.Info.Settings.Role;
                    bool isBoss = BossSpawnSystem.allBosses != null &&
                                  BossSpawnSystem.allBosses.Contains(botRole);

                    Console.WriteLine($"🎮 [BotHostilityPatch] Vanilla bot: {botRole}, isBoss: {isBoss}");

                    if (DeathMatch.DeathMatchModeActive)
                    {
                        Console.WriteLine($"🔥 [BotHostilityPatch] DeathMatch mode - configuring...");

                        // DEATHMATCH RAID: Boss-vs-Player hostility
                        if (mainPlayer != null && isBoss)
                        {
                            // Boss ↔ Player: Hostile
                            __instance.BotsGroup.AddEnemy(mainPlayer, EBotEnemyCause.initial);
                            Console.WriteLine($"🔥 [BotHostilityPatch] Boss {botRole} hostile to player");
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
                                       BossSpawnSystem.allBosses.Contains(b.Profile.Info.Settings.Role))
                                .ToList();

                            foreach (var otherBoss in otherBosses)
                            {
                                __instance.BotsGroup.RemoveEnemy(otherBoss);
                                otherBoss.BotsGroup.RemoveEnemy(__instance);
                                Console.WriteLine($"🤝 [BotHostilityPatch] Boss {botRole} friendly to {otherBoss.Profile.Info.Settings.Role}");
                            }
                        }
                    }
                    else
                    {
                        // ⚡ NORMAL RAID: Vanilla behavior
                        Console.WriteLine($"🏞️ [BotHostilityPatch] Normal raid - vanilla behavior");
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
        private static List<WildSpawnType> bossQueue = new List<WildSpawnType>();
        private static int currentSpawnIndex = 0;
        private static int bossesKilled = 0;
        public static bool systemActive = false;
        public static bool initialSpawnDone = false;

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
            WildSpawnType.bossBoarSniper,
            WildSpawnType.bossKolontay,
            WildSpawnType.bossPartisan,
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye,
            WildSpawnType.bossTagillaAgro,
            WildSpawnType.bossKillaAgro,
            WildSpawnType.tagillaHelperAgro,
            WildSpawnType.sectantPriest
        };

        public static void EnableBossSpawnSystem()
        {
            if (!systemActive)
            {
                systemActive = true;
            }
        }

        private static void InitializeBossQueue()
        {
            bossQueue = allBosses.OrderBy(x => UnityEngine.Random.value).ToList();
            currentSpawnIndex = 0;
            bossesKilled = 0;
        }

        public static void SpawnNextBoss()
        {
            try
            {
                if (currentSpawnIndex >= bossQueue.Count)
                {
                    InitializeBossQueue();
                }

                var botGame = Singleton<IBotGame>.Instance;
                if (botGame == null || botGame.BotsController == null) return;

                var spawner = botGame.BotsController.BotSpawner;
                if (spawner == null) return;

                var nextBoss = bossQueue[currentSpawnIndex];
                Console.WriteLine($"🎮 [Jiang Hu] Spawning boss: {nextBoss}");

                var profileData = new BotProfileDataClass(
                    EPlayerSide.Savage,
                    nextBoss,
                    BotDifficulty.hard,
                    0f,
                    new BotSpawnParams()
                    {
                        ShallBeGroup = new ShallBeGroupParams(true, true, 1)
                    },
                    false
                );

                spawner.ActivateBotsWithoutWave(1, profileData);
                currentSpawnIndex++;
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
            bossesKilled++;
            Console.WriteLine($"🎮 [Jiang Hu] Boss killed: {bossType} ({bossesKilled} total)");
            SpawnNextBoss();
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
        [HarmonyPatch(typeof(BossSpawnScenario), "smethod_0")]
        class BossSpawnScenarioCreationPatch
        {
            static void Postfix(ref BossSpawnScenario __result)
            {
                if (!DeathMatch.DeathMatchModeActive) return;
                try
                {
                    if (__result == null) return;

                    __result.SpawnBossAction = (wave) =>
                    {
                        if (BossSpawnSystem.systemActive && !BossSpawnSystem.initialSpawnDone)
                        {
                            BossSpawnSystem.OnRaidStarted();

                            int startingBots = DeathMatchButtonPatch.GetStartingBotsFromConfig();
                            for (int i = 0; i < startingBots; i++)
                            {
                                BossSpawnSystem.SpawnNextBoss();
                            }
                        }
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🎮 [Jiang Hu] Creation patch error: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(BossSpawnScenario), "Run", typeof(List<BotZone>), typeof(EBotsSpawnMode))]
        class BossSpawnOverridePatch
        {
            static bool Prefix(BossSpawnScenario __instance, List<BotZone> pmcZones, EBotsSpawnMode spawnMode)
            {
                return true;
            }
        }

        [HarmonyPatch(typeof(BotSpawner), "method_11")]
        class FinalSpawnInterceptPatch
        {
            static bool Prefix(BotOwner bot, BotCreationDataClass data, Action<BotOwner> callback,
                              bool shallBeGroup, Stopwatch stopWatch, BotSpawner __instance)
            {
                try
                {
                    if (!DeathMatch.DeathMatchModeActive) return true;
                    if (bot == null) return true;

                    var marker = bot.gameObject.GetComponent<OurPMCMarker>();
                    if (marker != null) return true;

                    var botRole = bot.Profile.Info.Settings.Role;
                    if (botRole == WildSpawnType.pmcBEAR || botRole == WildSpawnType.pmcUSEC) return true;

                    bool isBoss = BossSpawnSystem.allBosses != null &&
                                  BossSpawnSystem.allBosses.Contains(botRole);
                    if (isBoss) return true;

                    bot.LeaveData.RemoveFromMap();
                    UnityEngine.Object.Destroy(bot.gameObject);

                    if (bot.Profile.Info.Settings.IsFollower())
                        __instance.FollowersBotsCount--;
                    else if (bot.Profile.Info.Settings.IsBoss())
                        __instance.BossBotsCount--;

                    __instance.AllBotsCount--;
                    __instance.InSpawnProcess--;

                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(BotSpawner), "BotDied")]
        class AllBotDeathDetectionPatch
        {
            private static System.Collections.Generic.HashSet<string> processedBotIds = new System.Collections.Generic.HashSet<string>();

            static void Postfix(BotOwner bot)
            {
                if (!DeathMatch.DeathMatchModeActive) return;
                try
                {
                    if (bot == null || bot.Profile == null) return;

                    string botId = bot.Profile.Id;

                    if (processedBotIds.Contains(botId))
                        return;

                    processedBotIds.Add(botId);

                    var role = bot.Profile.Info.Settings.Role;

                    if (BossSpawnSystem.allBosses.Contains(role))
                    {
                        BossSpawnSystem.OnBossKilled(role);
                    }
                }
                catch { }
            }

            public static void ClearProcessedIds()
            {
                processedBotIds.Clear();
                Console.WriteLine($"🎮 [Jiang Hu] Cleared processed bot IDs");
            }
        }

        [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
        class RaidStartPatch
        {
            static void Postfix()
            {
                try
                {
                    AllBotDeathDetectionPatch.ClearProcessedIds();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🎮 [Jiang Hu] Raid start error: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(BotSpawner), "ActivateBotsByWave", typeof(BotWaveDataClass))]
        class BlockNormalSpawnWavesPatch
        {
            static bool Prefix(BotWaveDataClass wave, BotSpawner __instance)
            {
                if (!DeathMatch.DeathMatchModeActive) return true;
                return false;
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
                Console.WriteLine($"🎮 [Jiang Hu] Raid ended, disabling DeathMatch mode");
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
                    Console.WriteLine($"🎮 [Jiang Hu] Starting raid on: {randomMap}");
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
                            Console.WriteLine($"🎮 [Jiang Hu] No maps enabled, using all maps");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"🎮 [Jiang Hu] Config dict is null");
                    }
                }
                else
                {
                    Console.WriteLine($"🎮 [Jiang Hu] Config file not found, using default maps");
                    enabledMaps.AddRange(new[] {
                        "Woods", "factory4_day", "factory4_night", "bigmap", "Shoreline",
                        "Interchange", "RezervBase", "laboratory", "Lighthouse", "TarkovStreets",
                        "Sandbox", "Sandbox_high", "labyrinth"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🎮 [Jiang Hu] Config read error: {ex.Message}\n{ex.StackTrace}");
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