using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.Game.Spawning;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace JiangHu
{
    public class PMCBotSpawner : MonoBehaviour
    {
        private ConfigEntry<KeyboardShortcut> _spawnHotkey;
        private bool _isSpawning = false;
        private float _lastSpawnTime = 0f;
        private const float SPAWN_COOLDOWN = 2f;
        public static PMCBotSpawner Instance { get; private set; }

        private List<BotZone> _availableZones = new List<BotZone>();
        private List<BotZone> _failedZones = new List<BotZone>();
        private Dictionary<string, (BotZone zone, float time)> _pendingSpawns = new Dictionary<string, (BotZone, float)>();

        void Awake() { Instance = this; }

        void Update()
        {
            CheckSpawnTimeouts();
            if (_spawnHotkey != null && _spawnHotkey.Value.IsDown() && !_isSpawning)
            {
                SpawnPMCBot();
            }
        }

        public void Init(ConfigEntry<KeyboardShortcut> spawnHotkey) { _spawnHotkey = spawnHotkey; }

        private async void SpawnPMCBot()
        {
            if (_isSpawning) return;
            if (Time.time - _lastSpawnTime < SPAWN_COOLDOWN) return;

            _isSpawning = true;
            _lastSpawnTime = Time.time;

            NotificationManagerClass.DisplayMessageNotification($"Start Spawning 开始召唤", ENotificationDurationType.Long, ENotificationIconType.Alert, Color.cyan);

            try
            {
                var botGame = Singleton<IBotGame>.Instance;
                if (botGame?.BotsController == null) return;

                if (_availableZones.Count == 0)
                {
                    _availableZones = botGame.BotsController.BotSpawner.GetPmcZones();
                }

                var validZones = _availableZones.Where(z => !_failedZones.Contains(z)).ToList();
                if (validZones.Count == 0)
                {
                    _availableZones = botGame.BotsController.BotSpawner.GetPmcZones();
                    _failedZones.Clear();
                    validZones = _availableZones.ToList();
                }

                var spawnZone = validZones.RandomElement();

                var spawnParams = new BotSpawnParams()
                {
                    ShallBeGroup = new ShallBeGroupParams(false, true, 1),
                    Id_spawn = $"JiangHu_OurPMC|{Guid.NewGuid()}",
                };

                var profileData = new BotProfileDataClass(
                    EPlayerSide.Savage,
                    UnityEngine.Random.Range(0, 2) == 0 ? WildSpawnType.pmcBEAR : WildSpawnType.pmcUSEC,
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

                if (creationData == null) return;

                botGame.BotsController.BotSpawner.TryToSpawnInZoneAndDelay(
                    spawnZone,
                    creationData,
                    false,
                    true,
                    null,
                    true
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [SpawnPMCBot] Exception: {ex.Message}\n{ex.StackTrace}");
                NotificationManagerClass.DisplayMessageNotification(
                    "No available PMC spawn point 无PMC刷新点",
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

        public void TrackPendingSpawn(string spawnId, BotZone zone)
        {
            if (!_pendingSpawns.ContainsKey(spawnId))
            {
                _pendingSpawns[spawnId] = (zone, Time.time);
            }
        }

        public void RemovePendingSpawn(string spawnId)
        {
            _pendingSpawns.Remove(spawnId);
        }

        private void CheckSpawnTimeouts()
        {
            float currentTime = Time.time;
            List<string> toRemove = new List<string>();

            foreach (var kvp in _pendingSpawns)
            {
                if (currentTime - kvp.Value.time > 5f)
                {
                    AddFailedZone(kvp.Value.zone);
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (string spawnId in toRemove)
            {
                _pendingSpawns.Remove(spawnId);
            }
        }

        public void AddFailedZone(BotZone zone)
        {
            if (zone != null && !_failedZones.Contains(zone))
            {
                _failedZones.Add(zone);
                int availableZones = _availableZones.Count - _failedZones.Count;

                NotificationManagerClass.DisplayMessageNotification(
                    $"Spawn fails 召唤失败, {availableZones}/{_availableZones.Count} PMC spawn zones available 可用PMC刷新区域",
                    ENotificationDurationType.Default,
                    ENotificationIconType.Alert,
                    Color.yellow
                );
            }
        }
    }

    [HarmonyPatch(typeof(BotSpawner), "method_10")]
    class BotSpawnMethod10Patch
    {
        static void Prefix(BotZone zone, BotCreationDataClass data, Action<BotOwner> callback, CancellationToken cancellationToken)
        {
            try
            {
                if (data?._profileData?.SpawnParams?.Id_spawn?.StartsWith("JiangHu_OurPMC") == true)
                {
                    if (PMCBotSpawner.Instance != null)
                    {
                        PMCBotSpawner.Instance.TrackPendingSpawn(data._profileData.SpawnParams.Id_spawn, zone);
                    }
                }
            }
            catch { }
        }
    }

    public class OurPMCMarker : MonoBehaviour { }

    [HarmonyPatch(typeof(BotSpawner), "method_11")]
    class MarkOurPMCsOnSpawn
    {
        static void Postfix(BotOwner bot, BotCreationDataClass data, Action<BotOwner> callback, bool shallBeGroup, Stopwatch stopWatch)
        {
            try
            {
                var spawnData = bot.SpawnProfileData as BotProfileDataClass;
                if (spawnData == null) return;

                bool isOurPMC = spawnData.Side == EPlayerSide.Savage &&
                               (spawnData.WildSpawnType_0 == WildSpawnType.pmcBEAR ||
                                spawnData.WildSpawnType_0 == WildSpawnType.pmcUSEC) &&
                               spawnData.SpawnParams?.Id_spawn != null &&
                               spawnData.SpawnParams.Id_spawn.StartsWith("JiangHu_OurPMC");

                if (isOurPMC)
                {
                    bot.gameObject.AddComponent<OurPMCMarker>();
                    
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
                            
                            if (closestSpawn != null) bot.GetPlayer.Teleport(closestSpawn.Position, true);
                        }
                    }
                    
                    NotificationManagerClass.DisplayMessageNotification($"Spawned 召唤成功", ENotificationDurationType.Default, ENotificationIconType.Friend, Color.green);
                }

                if (PMCBotSpawner.Instance != null && spawnData?.SpawnParams?.Id_spawn != null)
                {
                    PMCBotSpawner.Instance.RemovePendingSpawn(spawnData.SpawnParams.Id_spawn);
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(BotOwner), "method_10")]
    class ConfigureOurPMCHostility
    {
        static void Postfix(BotOwner __instance)
        {
            try
            {
                if (__instance.gameObject.GetComponent<OurPMCMarker>() == null) return;
                var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                if (mainPlayer == null) return;

                if (__instance.BotsGroup != null) __instance.BotsGroup.AddAlly(mainPlayer);

                var botGame = Singleton<IBotGame>.Instance;
                if (botGame != null && botGame.BotsController?.Bots?.BotOwners != null)
                {
                    foreach (var otherBot in botGame.BotsController.Bots.BotOwners)
                    {
                        if (otherBot == null || otherBot.Profile.Id == __instance.Profile.Id) continue;
                        if (otherBot.gameObject.GetComponent<OurPMCMarker>() == null)
                        {
                            if (__instance.BotsGroup != null && !__instance.BotsGroup.Enemies.ContainsKey(otherBot))
                                __instance.BotsGroup.AddEnemy(otherBot, EBotEnemyCause.initial);
                        }
                    }
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(BotsGroup), "CheckAndAddEnemy")]
    class BotsGroupCheckAndAddEnemyPatch
    {
        static bool Prefix(IPlayer player, bool ignoreAI, BotsGroup __instance, ref bool __result)
        {
            try
            {
                bool isOurPMCGroup = false;
                foreach (var member in __instance.Members)
                {
                    if (member.gameObject.GetComponent<OurPMCMarker>() != null)
                    {
                        isOurPMCGroup = true;
                        break;
                    }
                }

                if (!isOurPMCGroup) return true; 

                var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                if (mainPlayer != null && player.ProfileId == mainPlayer.ProfileId)
                {
                    __result = false;
                    return false;
                }
            }
            catch { }
            return true;
        }
    }

    [HarmonyPatch(typeof(BotsGroup), "AddEnemy")]
    class BotsGroupAddEnemyPatch
    {
        static bool Prefix(IPlayer person, EBotEnemyCause cause, BotsGroup __instance)
        {
            try
            {
                bool isOurPMCGroup = false;
                foreach (var member in __instance.Members)
                {
                    if (member.gameObject.GetComponent<OurPMCMarker>() != null)
                    {
                        isOurPMCGroup = true;
                        break;
                    }
                }

                if (!isOurPMCGroup) return true; 

                var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                if (mainPlayer != null && person.ProfileId == mainPlayer.ProfileId)
                {
                    return false;
                }
            }
            catch { }
            return true;
        }
    }



    public class BotMarkerController : MonoBehaviour
    {
        private bool _enabled = false;
        private const float UpdateInterval = 0.25f;
        private GameWorld _gameWorld;
        private Player _player;
        private float _nextUpdate;
        private GUIStyle _guiStyle;
        private Camera _camera;
        private readonly Dictionary<string, BotInfo> _bots = new Dictionary<string, BotInfo>();

        private class BotInfo
        {
            public string Name;
            public Vector3 Position;
            public float Distance;
            public string DisplayText;
            public Rect GuiRect;
            public Vector3 LastScreenPos;
            public float LastSeen;
        }

        void Start()
        {
            if (!LoadConfig() || !_enabled) { Destroy(this); return; }
            _gameWorld = Singleton<GameWorld>.Instance;
            _player = _gameWorld?.MainPlayer;
            _camera = Camera.main;
            if (_gameWorld == null || _player == null || _camera == null) Destroy(this);
        }

        void Update() { if (_enabled) { UpdateBots(); CleanupOldBots(); } }
        void LateUpdate() { if (_enabled) CalculateGUIPositions(); }

        private void CalculateGUIPositions()
        {
            if (_camera == null) return;
            foreach (var kvp in _bots)
            {
                var bot = kvp.Value;
                Vector3 screenPos = _camera.WorldToScreenPoint(bot.Position + Vector3.up * 1.7f);
                bot.LastScreenPos = screenPos;
                if (screenPos.z > 0)
                {
                    Vector2 size = _guiStyle.CalcSize(new GUIContent(bot.DisplayText));
                    bot.GuiRect = new Rect(screenPos.x - size.x / 2, Screen.height - screenPos.y - size.y, size.x, size.y);
                }
            }
        }

        void OnGUI()
        {
            if (!_enabled || _camera == null) return;
            if (_guiStyle == null)
            {
                _guiStyle = new GUIStyle(GUI.skin.label);
                _guiStyle.alignment = TextAnchor.MiddleCenter;
                _guiStyle.fontSize = 12;
                _guiStyle.richText = true;
                _guiStyle.padding = new RectOffset(4, 4, 2, 2);
            }
            foreach (var bot in _bots.Values) if (bot.LastScreenPos.z > 0) GUI.Box(bot.GuiRect, bot.DisplayText, _guiStyle);
        }

        private bool LoadConfig()
        {
            try
            {
                string configPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");
                if (!System.IO.File.Exists(configPath)) return false;
                string json = System.IO.File.ReadAllText(configPath);
                var config = Newtonsoft.Json.Linq.JObject.Parse(json);
                _enabled = config["Show_Teammate"]?.Value<bool>() ?? false;
                return true;
            }
            catch { return false; }
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
            if (botOwner.gameObject.GetComponent<OurPMCMarker>() == null) return;

            if (!_bots.TryGetValue(id, out var bot))
            {
                bot = new BotInfo { Name = player.Profile?.Nickname ?? "Teammate" };
                _bots[id] = bot;
            }
            bot.Position = player.Position;
            bot.Distance = distance;
            bot.DisplayText = $"<color=green>{bot.Name}</color>\n{distance:F0}m";
            bot.LastSeen = currentTime;
        }

        private void CleanupOldBots()
        {
            float currentTime = Time.time;
            List<string> toRemove = new List<string>();
            foreach (var kvp in _bots) if (currentTime - kvp.Value.LastSeen > 3f) toRemove.Add(kvp.Key);
            foreach (string id in toRemove) _bots.Remove(id);
        }

        void OnDestroy() { _bots.Clear(); }
    }

    [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
    class GameWorldStartPatch
    {
        [HarmonyPostfix]
        static void Postfix(GameWorld __instance)
        {
            var existing = __instance.GetComponent<BotMarkerController>();
            if (existing != null) UnityEngine.Object.Destroy(existing);
            __instance.gameObject.AddComponent<BotMarkerController>();
        }
    }
}