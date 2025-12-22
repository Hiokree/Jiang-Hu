using EFT;
using EFT.Communications;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace JiangHu
{
    public class BossNotificationSystem : MonoBehaviour
    {
        public static BossNotificationSystem Instance { get; private set; }

        private HashSet<string> processedBossSpawns = new HashSet<string>();
        private HashSet<string> processedBossDeaths = new HashSet<string>();

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void OnBossSpawned(BotOwner boss)
        {
            if (boss == null || boss.Profile == null) return;

            string bossId = boss.Profile.Id;
            if (processedBossSpawns.Contains(bossId)) return;

            processedBossSpawns.Add(bossId);

            string bossName = UniversalBotSpawner.GetBossDisplayName(boss.Profile.Info.Settings.Role);
            Color notificationColor;
            UnityEngine.ColorUtility.TryParseHtmlString("#fbb957", out notificationColor);

            string notificationMessage = $"<color=#fbb957>BOSS {bossName.ToUpper()} ENTERED THE RAID</color>";

            NotificationManagerClass.DisplayMessageNotification(
                notificationMessage,
                ENotificationDurationType.Long,
                ENotificationIconType.Alert,
                notificationColor
            );
        }

        public void OnBossDied(BotOwner boss)
        {
            if (boss == null || boss.Profile == null) return;

            string bossId = boss.Profile.Id;
            if (processedBossDeaths.Contains(bossId)) return;

            processedBossDeaths.Add(bossId);

            string bossName = UniversalBotSpawner.GetBossDisplayName(boss.Profile.Info.Settings.Role);
            Color notificationColor;
            UnityEngine.ColorUtility.TryParseHtmlString("#0eb0c9", out notificationColor);

            string notificationMessage = $"<color=#0eb0c9>BOSS {bossName.ToUpper()} ELIMINATED</color>";

            NotificationManagerClass.DisplayMessageNotification(
                notificationMessage,
                ENotificationDurationType.Long,
                ENotificationIconType.Default,
                notificationColor
            );
        }


        public void ClearProcessedIds()
        {
            processedBossSpawns.Clear();
            processedBossDeaths.Clear();
        }
    }

    [HarmonyPatch(typeof(BotSpawner), "method_11")]
    class BossSpawnDetectionPatch
    {
        static void Postfix(BotOwner bot, BotCreationDataClass data, Action<BotOwner> callback, bool shallBeGroup, Stopwatch stopWatch)
        {
            try
            {
                if (bot == null || bot.Profile == null) return;

                var role = bot.Profile.Info.Settings.Role;

                if (UniversalBotSpawner.allBosses != null &&
                    Array.Exists(UniversalBotSpawner.allBosses, bossType => bossType == role))
                {
                    BossNotificationSystem.Instance?.StartCoroutine(DelayedBossSpawnNotification(bot));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🎮 [Jiang Hu] Boss spawn detection error: {ex.Message}");
            }
        }

        private static System.Collections.IEnumerator DelayedBossSpawnNotification(BotOwner boss)
        {
            yield return new WaitForSeconds(1f);
            BossNotificationSystem.Instance?.OnBossSpawned(boss);
        }
    }

    [HarmonyPatch(typeof(BotSpawner), "BotDied")]
    class BossDeathNotificationPatch
    {
        static void Postfix(BotOwner bot)
        {
            try
            {
                if (bot == null || bot.Profile == null) return;

                var role = bot.Profile.Info.Settings.Role;

                if (UniversalBotSpawner.allBosses != null &&
                    Array.Exists(UniversalBotSpawner.allBosses, bossType => bossType == role))
                {
                    BossNotificationSystem.Instance?.OnBossDied(bot);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🎮 [Jiang Hu] Boss death notification error: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
    class RaidStartClearPatch
    {
        static void Postfix()
        {
            try
            {
                BossNotificationSystem.Instance?.ClearProcessedIds();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🎮 [Jiang Hu] Raid start clear error: {ex.Message}");
            }
        }
    }
}