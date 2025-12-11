using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.Game.Spawning;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace JiangHu
{
    public class PMCBotSpawner : MonoBehaviour
    {
        private ConfigEntry<KeyboardShortcut> _spawnHotkey;
        private bool _isSpawning = false;
        private float _lastSpawnTime = 0f;
        private const float SPAWN_COOLDOWN = 2f;

        void Update()
        {
            if (_spawnHotkey != null && _spawnHotkey.Value.IsDown())
            {
                SpawnPMCBot();
            }
        }

        public void Init(ConfigEntry<KeyboardShortcut> spawnHotkey)
        {
            _spawnHotkey = spawnHotkey;
        }

        private void SpawnPMCBot()
        {
            if (_isSpawning) return;
            if (Time.time - _lastSpawnTime < SPAWN_COOLDOWN) return;

            _isSpawning = true;

            try
            {
                var botGame = Singleton<IBotGame>.Instance;
                if (botGame == null || botGame.BotsController == null) return;

                var playerZone = GetPlayerCurrentZone();
                if (playerZone == null) return;

                var spawnParams = new BotSpawnParams()
                {
                    ShallBeGroup = new ShallBeGroupParams(false, true, 1),
                    Id_spawn = $"JiangHu_OurPMC|{playerZone.name}",
                };

                var profileData = new BotProfileDataClass(
                    EPlayerSide.Savage,
                    UnityEngine.Random.Range(0, 2) == 0 ? WildSpawnType.pmcBEAR : WildSpawnType.pmcUSEC,
                    BotDifficulty.hard,
                    0f,
                    spawnParams,
                    false
                );

                botGame.BotsController.ActivateBotsWithoutWave(1, profileData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [PMCBotSpawner] {ex.Message}");
            }
            finally
            {
                _lastSpawnTime = Time.time;
                _isSpawning = false;
            }
        }

        private BotZone GetPlayerCurrentZone()
        {
            try
            {
                var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                if (mainPlayer == null) return null;

                var botGame = Singleton<IBotGame>.Instance;
                if (botGame?.BotsController == null) return null;

                float dist;
                var zone = botGame.BotsController.GetClosestZone(mainPlayer.Position, out dist);
                return zone;
            }
            catch
            {
                return null;
            }
        }
    }

    public class OurPMCMarker : MonoBehaviour { }

    [HarmonyPatch(typeof(BotSpawner), "method_11")]
    class MarkOurPMCsOnSpawn
    {
        static void Postfix(BotOwner bot, BotCreationDataClass data, Action<BotOwner> callback,
                           bool shallBeGroup, Stopwatch stopWatch)
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

                    string zoneName = spawnData.SpawnParams.Id_spawn.Split('|')[1];
                    var botGame = Singleton<IBotGame>.Instance;
                    var playerZone = botGame?.BotsController?.BotSpawner?.GetZoneByName(zoneName);

                    if (playerZone != null)
                    {
                        var spawnPoints = playerZone.SpawnPointMarkers;
                        if (spawnPoints != null && spawnPoints.Count > 0)
                        {
                            var mainPlayer = Singleton<GameWorld>.Instance?.MainPlayer;
                            if (mainPlayer != null)
                            {
                                ISpawnPoint closestSpawn = null;
                                float closestDist = float.MaxValue;

                                foreach (var marker in spawnPoints)
                                {
                                    if (marker?.SpawnPoint == null) continue;

                                    float dist = (marker.SpawnPoint.Position - mainPlayer.Position).sqrMagnitude;
                                    if (dist < closestDist)
                                    {
                                        closestDist = dist;
                                        closestSpawn = marker.SpawnPoint;
                                    }
                                }

                                if (closestSpawn != null)
                                {
                                    bot.GetPlayer.Teleport(closestSpawn.Position, true);
                                }
                            }
                        }
                    }

                    NotificationManagerClass.DisplayMessageNotification(
                        $"召唤了 PMC队友",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Friend,
                        Color.green
                    );
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

                __instance.BotsGroup.RemoveEnemy(mainPlayer);
                if (mainPlayer.BotsGroup != null)
                    mainPlayer.BotsGroup.RemoveEnemy(__instance);

                if (__instance.BotsGroup != null)
                    __instance.BotsGroup.AddAlly(mainPlayer);

                if (mainPlayer.BotsGroup != null)
                    mainPlayer.BotsGroup.AddAlly(__instance.GetPlayer);

                var botGame = Singleton<IBotGame>.Instance;
                if (botGame != null)
                {
                    foreach (var otherBot in botGame.BotsController.Bots.BotOwners)
                    {
                        if (otherBot == null || otherBot.Profile.Id == __instance.Profile.Id)
                            continue;

                        if (otherBot.gameObject.GetComponent<OurPMCMarker>() != null)
                        {
                            __instance.BotsGroup.RemoveEnemy(otherBot);
                            otherBot.BotsGroup.RemoveEnemy(__instance);
                        }
                        else
                        {
                            if (!__instance.BotsGroup.Enemies.ContainsKey(otherBot))
                                __instance.BotsGroup.AddEnemy(otherBot, EBotEnemyCause.initial);
                        }
                    }
                }
            }
            catch { }
        }
    }
}