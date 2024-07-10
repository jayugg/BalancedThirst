using System;
using System.Linq;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_GetTransitionRateMul_Patch
{
    public static bool ShouldSkipPatch => !ConfigSystem.SyncedConfigData.EnableThirst;
    public static void Postfix(
        CollectibleObject __instance,
        ref float __result,
        IWorldAccessor world,
        ItemSlot inSlot,
        EnumTransitionType transType)
    {
        if (ShouldSkipPatch) return;
        var stack = inSlot.Itemstack;
        if (!stack.Collectible.IsWaterPortion() || __instance is not BlockLiquidContainerBase) return;
        __result *= (float) Math.Sqrt(WaterContainerBehavior.GetTransitionRateMul(__instance, transType));
    }
    
}