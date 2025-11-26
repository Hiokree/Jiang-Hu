using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JiangHu.Patches
{
    public class PatchSlotItemViewRefresh : ModulePatch
    {
        private static readonly string[] CosmosCasketIds = new[] { "e983002c4ab4d99999889007", "e983002c4ab4d99999889008", "e983002c4ab4d99999889009" };
        private static readonly List<GridView> ActiveCosmosGrids = new List<GridView>();

        protected override MethodBase GetTargetMethod()
        {
            return typeof(SlotItemView).GetMethod("method_39");
        }

        [PatchPostfix]
        private static void PatchPostfix(SlotItemView __instance, bool forcedRedraw)
        {
            if (!IsCosmosCasket(__instance.Item?.TemplateId)) return;

            var address = __instance.Item.CurrentAddress as GClass3391;
            if (address != null && address.IsSpecialSlotAddress())
            {
                foreach (var grid in ActiveCosmosGrids.ToArray())
                {
                    if (grid != null && grid.GameObject != null && grid.GameObject.activeSelf)
                    {
                        grid.GameObject.SetActive(false);
                    }
                }
            }
        }

        public static void TrackCosmosGrid(GridView grid)
        {
            if (IsCosmosCasket(grid?.Grid?.ParentItem?.TemplateId) && !ActiveCosmosGrids.Contains(grid))
            {
                ActiveCosmosGrids.Add(grid);
            }
        }

        public static void UntrackCosmosGrid(GridView grid)
        {
            ActiveCosmosGrids.Remove(grid);
        }

        public static bool IsCosmosCasket(string templateId)        {
            foreach (var id in CosmosCasketIds)
            {
                if (templateId == id) return true;
            }
            return false;
        }
    }

    public class PatchGridViewShow : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GridView).GetMethod("Show");
        }

        [PatchPostfix]
        private static void PatchPostfix(GridView __instance, StashGridClass grid, ItemContextAbstractClass parentContext, TraderControllerClass itemController, ItemUiContext itemUiContext, FilterPanel filterPanel, bool magnify)
        {
            if (PatchSlotItemViewRefresh.IsCosmosCasket(grid?.ParentItem?.TemplateId) && IsGridInSlotContext(__instance))
            {
                __instance.GameObject.SetActive(false);
                PatchSlotItemViewRefresh.TrackCosmosGrid(__instance);
            }
        }

        private static bool IsGridInSlotContext(GridView grid)
        {
            Transform current = grid.transform;
            while (current != null)
            {
                if (current.GetComponent<SlotItemView>() != null) return true;
                if (current.GetComponent<GridWindow>() != null) return false;
                current = current.parent;
            }
            return true;
        }
    }
}