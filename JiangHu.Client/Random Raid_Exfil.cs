using Comfort.Common;
using EFT;
using EFT.Interactive;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JiangHu.ExfilRandomizer
{
    internal class RandomExfilDestinationPatch : ModulePatch
    {
        private static List<LocationSettingsClass.Location> _allLocations;
        private static List<string> _availableTargetPool;
        private static Dictionary<int, string> _pointToTargetMap = new Dictionary<int, string>();
        private static Dictionary<int, OriginalTransitData> _originalTransitData = new Dictionary<int, OriginalTransitData>();
        private static bool _poolInitialized = false;
        private static bool _buttonClickedForThisRaid = false;

        private class OriginalTransitData
        {
            public string OriginalTarget { get; set; }
            public string OriginalLocation { get; set; }
            public bool OriginalEventsEnable { get; set; }
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(TransitPoint).GetMethod("method_7",
                BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void Prefix(TransitPoint __instance, HashSet<string> players)
        {
            try
            {

                StoreOriginalIfNeeded(__instance);

                if (!_buttonClickedForThisRaid)
                {
                    RestoreToOriginal(__instance);
                    Console.WriteLine($"\x1b[35m📊 [Jiang Hu] DEBUG: Skipping normal raid\x1b[0m");
                    return;
                }

                if (players == null || players.Count == 0)
                    return;

                if (_allLocations == null)
                {
                    CacheAllLocations();
                }

                if (_allLocations == null || _allLocations.Count == 0)
                {
                    return;
                }

                if (!_poolInitialized)
                {
                    InitializeTargetPool();
                }

                if (_availableTargetPool == null || _availableTargetPool.Count == 0)
                {
                    return;
                }

                int pointId = __instance.parameters.id;

                if (!_pointToTargetMap.TryGetValue(pointId, out string currentTarget))
                {
                    string newTarget = GetRandomTargetFromPool();
                    _pointToTargetMap[pointId] = newTarget;
                    currentTarget = newTarget;
                }

                UpdateTransitParameters(__instance, currentTarget);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m❌ [Jiang Hu] Exfil randomization error: {ex.Message}\x1b[0m");
            }
        }

        private static void StoreOriginalIfNeeded(TransitPoint transitPoint)
        {
            int pointId = transitPoint.parameters.id;

            if (!_originalTransitData.ContainsKey(pointId))
            {
                _originalTransitData[pointId] = new OriginalTransitData
                {
                    OriginalTarget = transitPoint.parameters.target,
                    OriginalLocation = transitPoint.parameters.location,
                    OriginalEventsEnable = transitPoint.parameters.eventsEnable
                };

                Console.WriteLine($"\x1b[35m📊 [Jiang Hu] DEBUG: Stored original for point {pointId}: {transitPoint.parameters.target}\x1b[0m");
            }
        }

        private static void RestoreAllOriginals()
        {
            var allTransitPoints = UnityEngine.Object.FindObjectsOfType<TransitPoint>();
            int restoredCount = 0;

            foreach (var transitPoint in allTransitPoints)
            {
                int pointId = transitPoint.parameters.id;

                if (_originalTransitData.TryGetValue(pointId, out var originalData))
                {
                    transitPoint.parameters.target = originalData.OriginalTarget;
                    transitPoint.parameters.location = originalData.OriginalLocation;
                    transitPoint.parameters.eventsEnable = originalData.OriginalEventsEnable;
                    restoredCount++;
                }
            }

            if (restoredCount > 0)
            {
                Console.WriteLine($"\x1b[35m📊 [Jiang Hu] DEBUG: Restored {restoredCount} transit points to original values\x1b[0m");
            }
        }

        public static void SetButtonClickedForThisRaid()
        {
            _buttonClickedForThisRaid = true;
            Console.WriteLine($"\x1b[32m✅ [Jiang Hu] buttonClicked flag set to true\x1b[0m");
        }

        private static void RestoreToOriginal(TransitPoint transitPoint)
        {
            int pointId = transitPoint.parameters.id;

            if (_originalTransitData.TryGetValue(pointId, out var originalData))
            {
                transitPoint.parameters.target = originalData.OriginalTarget;
                transitPoint.parameters.location = originalData.OriginalLocation;
                transitPoint.parameters.eventsEnable = originalData.OriginalEventsEnable;
            }
        }

        public static void ResetAfterRaid()
        {
            _buttonClickedForThisRaid = false;
            _pointToTargetMap.Clear();
            _originalTransitData.Clear();
            Console.WriteLine($"\x1b[33m⚠️ [Jiang Hu] buttonClicked flag set to false (raid ended)\x1b[0m");
        }

        private static void UpdateTransitParameters(TransitPoint transitPoint, string targetLocation)
        {
            transitPoint.parameters.target = targetLocation;
            transitPoint.parameters.location = targetLocation;
            transitPoint.parameters.eventsEnable = false;
        }

        private static string GetRandomTargetFromPool()
        {
            if (_availableTargetPool == null || _availableTargetPool.Count == 0)
                return "unknown";

            var currentMap = Singleton<GameWorld>.Instance?.MainPlayer?.Location;

            var filteredPool = _availableTargetPool
                .Where(l => l != currentMap)
                .ToList();

            if (filteredPool.Count == 0)
                filteredPool = _availableTargetPool;

            return filteredPool[UnityEngine.Random.Range(0, filteredPool.Count)];
        }

        private static void InitializeTargetPool()
        {
            try
            {
                var currentMap = Singleton<GameWorld>.Instance?.MainPlayer?.Location;

                _availableTargetPool = _allLocations
                    .Where(l => l.Id != currentMap &&
                           l.Id != "hideout" &&
                           l.Id != "develop" &&
                           l.Id != "Private Area" &&
                           !l.Id.Contains("Suburbs") &&
                           !l.Id.Contains("Terminal") &&
                           !l.Id.Contains("Town"))
                    .Select(l => l.Id)
                    .Distinct()
                    .ToList();

                _poolInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m❌ [Jiang Hu] Target pool init error: {ex.Message}\x1b[0m");
            }
        }

        private static void CacheAllLocations()
        {
            try
            {
                if (TarkovApplication.Exist(out var app))
                {
                    var session = app.Session;
                    var locationSettings = session?.LocationSettings;

                    if (locationSettings?.locations != null)
                    {
                        _allLocations = locationSettings.locations.Values
                            .Where(l => l.Id != "hideout" &&
                                   l.Id != "develop" &&
                                   l.Id != "Private Area" &&
                                   !l.Id.Contains("Suburbs") &&
                                   !l.Id.Contains("Terminal") &&
                                   !l.Id.Contains("Town"))
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[31m❌ [Jiang Hu] Location cache error: {ex.Message}\x1b[0m");
            }
        }

        public static bool IsRandomRaidActive()
        {
            return _buttonClickedForThisRaid;
        }

        public static void ValidateStateInMenu()
        {
            try
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                bool isInMenu = gameWorld == null;

                if (isInMenu && _buttonClickedForThisRaid)
                {
                    ResetAfterRaid();
                    RestoreAllOriginals();
                }
            }
            catch { }
        }
    }
}