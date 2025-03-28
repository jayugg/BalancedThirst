using System;
using System.Text;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.Meal;

public class BlockMeal_GetHeldItemInfo_Patch
{
    private static bool ShouldSkipPatch => !ConfigSystem.ConfigServer.EnableThirst;
    
    public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (ShouldSkipPatch) return;
        var hydrationProps = inSlot.Itemstack.Collectible.GetHydrationProperties(world, inSlot.Itemstack, null);
        if (hydrationProps != null && Math.Round(hydrationProps.Hydration) != 0)
        {
            var hydrationText = $"- {Lang.Get($"{BtCore.Modid}:blockinfo-meal-hyd", Math.Round(hydrationProps.Hydration))}";
            if (!dsc.ToString().Contains(hydrationText))
            {
                dsc.AppendLine(hydrationText);
            }
        }
    }
}