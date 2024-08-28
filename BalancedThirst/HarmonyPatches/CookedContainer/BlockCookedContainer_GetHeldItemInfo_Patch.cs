using System;
using System.Text;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CookedContainer;

public class BlockCookedContainer_GetHeldItemInfo_Patch
{
    private static bool ShouldSkipPatch => !ConfigSystem.SyncedConfigData.EnableThirst;
    
    public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (ShouldSkipPatch) return;
        HydrationProperties hydrationProps = inSlot.Itemstack.Collectible.GetHydrationProperties(world, inSlot.Itemstack, null);
        if (hydrationProps != null && hydrationProps.Hydration != 0)
        {
            string hydrationText = Lang.Get($"{BtCore.Modid}:blockinfo-meal-hyd", hydrationProps.Hydration);
            string dscString = dsc.ToString();
            if (dscString.Contains(hydrationText)) return;
            int index = dscString.IndexOf(Lang.Get("Nutrition Facts"), StringComparison.Ordinal);
            if (index != -1)
            {
                dsc.Insert(index + Lang.Get("Nutrition Facts").Length, $"\n- {hydrationText}");
            }
            else
            {
                dsc.AppendLine(hydrationText);
            }
        }
    }
}