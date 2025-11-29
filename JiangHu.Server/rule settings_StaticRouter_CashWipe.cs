using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Routers.Static;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace JiangHu.Server
{
    [Injectable]
    public class CashWipeStaticRouter : StaticRouter
    {
        public CashWipeStaticRouter(JsonUtil jsonUtil, SaveServer saveServer)
            : base(jsonUtil, new List<RouteAction>
            {
                new RouteAction<EndLocalRaidRequestData>(
                    "/client/match/local/end",
                    async (url, info, sessionId, output) => await HandleCashWipe(url, info, sessionId, output, saveServer)
                )
            })
        {
            if (IsCashWipeEnabled())
            {
                Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Cash Wipe Enabled    死亡清空现金\x1b[0m");
            }
        }

        private static async Task<string> HandleCashWipe(string url, EndLocalRaidRequestData info, MongoId sessionId, string output, SaveServer saveServer)
        {
            if (!IsCashWipeEnabled())
                return output;

            var exitStatus = info.Results?.Result;
            if (exitStatus == ExitStatus.SURVIVED || exitStatus == ExitStatus.RUNNER || exitStatus == ExitStatus.TRANSIT)
                return output;

            var profiles = saveServer.GetProfiles();
            if (!profiles.ContainsKey(sessionId))
                return output;

            var pmc = profiles[sessionId].CharacterData?.PmcData;
            var inventory = pmc?.Inventory?.Items;
            if (inventory == null)
                return output;

            var wipeCoefficient = GetWipeCoefficient();
            var CASH_TPLS = new Dictionary<string, MongoId>
            {
                { "RUB", new MongoId("5449016a4bdc2d6f028b456f") },
                { "USD", new MongoId("569668774bdc2da2298b4568") },
                { "EUR", new MongoId("5696686a4bdc2da3298b456a") }
            };

            var thresholds = new Dictionary<string, long>
            {
                { "RUB", 1000000 },
                { "USD", 0 },
                { "EUR", 0 }
            };

            var removedByType = new Dictionary<string, long>
            {
                { "RUB", 0 },
                { "USD", 0 },
                { "EUR", 0 }
            };

            var amountToRemoveByType = new Dictionary<string, long>
            {
                { "RUB", 0 },
                { "USD", 0 },
                { "EUR", 0 }
            };

            var cashItems = inventory.Where(i => CASH_TPLS.Values.Contains(i.Template)).ToList();

            var totalCash = new Dictionary<string, long> { { "RUB", 0 }, { "USD", 0 }, { "EUR", 0 } };
            foreach (var cashItem in cashItems)
            {
                string type = CASH_TPLS.FirstOrDefault(x => x.Value == cashItem.Template).Key ?? "UNKNOWN";
                totalCash[type] += (long) (cashItem.Upd?.StackObjectsCount ?? 0);
            }

            foreach (var type in totalCash.Keys)
            {
                if (totalCash[type] > thresholds[type])
                {
                    long amountToRemove = (long) (totalCash[type] * wipeCoefficient);
                    long newTotal = totalCash[type] - amountToRemove;

                    if (newTotal < thresholds[type])
                    {
                        amountToRemove = totalCash[type] - thresholds[type];
                    }

                    amountToRemoveByType[type] = amountToRemove;
                }
            }

            foreach (var cashItem in cashItems.ToList())
            {
                string type = CASH_TPLS.FirstOrDefault(x => x.Value == cashItem.Template).Key ?? "UNKNOWN";

                if (!amountToRemoveByType.ContainsKey(type) || amountToRemoveByType[type] <= 0)
                    continue;

                long currentStack = (long) (cashItem.Upd?.StackObjectsCount ?? 0);
                long removeFromThisStack = Math.Min(currentStack, amountToRemoveByType[type]);

                if (removeFromThisStack > 0)
                {
                    long newStack = currentStack - removeFromThisStack;
                    amountToRemoveByType[type] -= removeFromThisStack;
                    removedByType[type] += removeFromThisStack;

                    if (newStack > 0)
                    {
                        cashItem.Upd ??= new Upd();
                        cashItem.Upd.StackObjectsCount = newStack;
                    }
                    else
                    {
                        inventory.Remove(cashItem);
                    }
                }
            }

            Console.WriteLine($"\x1b[91m🔥 [Jiang Hu] Cash wiped: {removedByType["RUB"]} RUB, {removedByType["USD"]} USD, {removedByType["EUR"]} EUR (Coefficient: {wipeCoefficient}) \x1b[0m");

            return output;
        }

        private static bool IsCashWipeEnabled()
        {
            try
            {
                var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configPath = System.IO.Path.Combine(modPath, "config", "config.json");

                if (!File.Exists(configPath))
                    return false;

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                return config?.TryGetValue("Enable_Cash_Wipe", out var cashWipeValue) == true && cashWipeValue.GetBoolean();
            }
            catch
            {
                return false;
            }
        }

        private static double GetWipeCoefficient()
        {
            try
            {
                var modPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var configPath = System.IO.Path.Combine(modPath, "config", "config.json");

                if (!File.Exists(configPath)) return 1.0;

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (config?.TryGetValue("Cash_Wipe_Coefficiency", out var coeffValue) == true)
                    return coeffValue.GetDouble();
            }
            catch { }

            return 1.0;
        }
    }
}
