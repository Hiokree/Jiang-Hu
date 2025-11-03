using HarmonyLib;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using System.Reflection;

namespace JiangHu.Patches
{
    public class PatchUseRepairKitInRaid
    {
        public static void Enable()
        {
            try
            {
                var harmony = new Harmony("com.jianghu.raidrepair");
                harmony.PatchAll();
                Console.WriteLine("🛠️ [Jiang Hu] Raid repair patch applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Jiang Hu] Error applying raid repair patch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ContextInteractionSwitcherClass))]
    [HarmonyPatch("Boolean_1", MethodType.Getter)]
    public class RaidCheckPatch
    {
        static bool Prefix(ref bool __result)
        {
            if (GClass2340.InRaid)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ContextInteractionSwitcherClass))]
    [HarmonyPatch("IsActive")]
    public class InsurancePatch
    {
        static void Postfix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref bool __result)
        {
            if (GClass2340.InRaid && button == EItemInfoButton.Insure)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(GClass905))]
    [HarmonyPatch("Repairers", MethodType.Getter)]
    public class GClass905Patch
    {
        static bool Prefix(GClass905 __instance, ref IEnumerable<IRepairer> __result)
        {
            if (!GClass2340.InRaid)
            {
                return true;
            }

            EnsureRepairKitsInRaid(__instance);
            __result = __instance.RepairKitsCollections.Cast<IRepairer>();
            return false;
        }

        static void EnsureRepairKitsInRaid(GClass905 __instance)
        {
            try
            {
                var allPlayerItems = __instance.RepairControllerClass.Inventory_0.GetPlayerItems(EPlayerItems.Equipment | EPlayerItems.Stash);
                var pmcRepairKits = allPlayerItems.OfType<RepairKitsItemClass>().ToList();

                var field = typeof(GClass905).GetField("List_1", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (field == null) return;

                var currentCollections = (List<GClass904>)field.GetValue(__instance);
                var templateToCollection = currentCollections.ToDictionary(c => c.RepairKitsTemplateClass);

                foreach (var repairKit in pmcRepairKits)
                {
                    if (repairKit.Resource.Positive() &&
                        __instance.RepairControllerClass.Profile_0.Examined(repairKit) &&
                        repairKit.CanRepair(__instance.Item_0) &&
                        repairKit.PinLockState != EItemPinLockState.Locked)
                    {
                        var template = repairKit.RepairKitTemplate;
                        if (templateToCollection.TryGetValue(template, out var collection))
                        {
                            collection.AddRepairKit(repairKit);
                        }
                        else
                        {
                            var newCollection = __instance.RepairControllerClass.CreateRepairKitsCollection(repairKit);
                            templateToCollection[template] = newCollection;
                            currentCollections.Add(newCollection);
                        }
                    }
                }

                if (__instance.CurrentRepairer == null && currentCollections.Count > 0)
                {
                    __instance.CurrentRepairer = currentCollections.First();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Jiang Hu] Error ensuring repair kits in GClass905: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GClass906))]
    [HarmonyPatch("Repairers", MethodType.Getter)]
    public class GClass906Patch
    {
        static bool Prefix(GClass906 __instance, ref IEnumerable<IRepairer> __result)
        {
            if (!GClass2340.InRaid)
            {
                return true;
            }

            EnsureRepairKitsInRaid(__instance);
            __result = __instance.RepairKitsCollections.Cast<IRepairer>();
            return false;
        }

        static void EnsureRepairKitsInRaid(GClass906 __instance)
        {
            try
            {
                var allPlayerItems = __instance.RepairControllerClass.Inventory_0.GetPlayerItems(EPlayerItems.Equipment | EPlayerItems.Stash);
                var pmcRepairKits = allPlayerItems.OfType<RepairKitsItemClass>().ToList();

                var field = typeof(GClass906).GetField("List_0", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (field == null) return;

                var currentCollections = (List<GClass904>)field.GetValue(__instance);
                var templateToCollection = currentCollections.ToDictionary(c => c.RepairKitsTemplateClass);

                foreach (var repairKit in pmcRepairKits)
                {
                    if (repairKit.Resource.Positive() &&
                        __instance.RepairControllerClass.Profile_0.Examined(repairKit) &&
                        repairKit.PinLockState != EItemPinLockState.Locked)
                    {
                        bool canRepairAny = false;
                        foreach (var armorComponent in __instance.Class528_0)
                        {
                            if (repairKit.CanRepair(armorComponent.Item))
                            {
                                canRepairAny = true;
                                break;
                            }
                        }

                        if (canRepairAny)
                        {
                            var template = repairKit.RepairKitTemplate;
                            if (templateToCollection.TryGetValue(template, out var collection))
                            {
                                collection.AddRepairKit(repairKit);
                            }
                            else
                            {
                                var newCollection = __instance.RepairControllerClass.CreateRepairKitsCollection(repairKit);
                                templateToCollection[template] = newCollection;
                                currentCollections.Add(newCollection);
                            }
                        }
                    }
                }

                if (__instance.CurrentRepairer == null && currentCollections.Count > 0)
                {
                    __instance.CurrentRepairer = currentCollections.First();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Jiang Hu] Error ensuring repair kits in GClass906: {ex.Message}");
            }
        }
    }
}