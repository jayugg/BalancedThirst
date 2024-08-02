using System;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.Container;

public class BlockContainer_GetContainingTransitionModifier
{
    public static bool ShouldSkipPatch => !ConfigSystem.SyncedConfigData.EnableThirst;
    
    public static void Contained_Postfix(
        BlockContainer __instance,
        ref float __result,
        IWorldAccessor world,
        ItemSlot inSlot,
        EnumTransitionType transType)
    {
        if (ShouldSkipPatch) return;
        if (transType != EnumTransitionType.Perish || __instance is not BlockLiquidContainerBase container || !container.IsWaterContainer()) return;
        var contentStack = container.GetContent(inSlot.Itemstack);
        if (!contentStack.Collectible.IsWaterPortion()) return;
        var exp = IsSinglePlayer(world) ? 0.5f : 1f; // Have to adjust because somehow it gets applied twice in single player
        float multiplier = (float) Math.Pow(WaterContainerBehavior.GetTransitionRateMul(container, transType), exp);
        __result *= multiplier;
    }
    
    public static void Placed_Postfix(
        BlockContainer __instance,
        ref float __result,
        IWorldAccessor world,
        BlockPos pos,
        EnumTransitionType transType)
    {
        if (ShouldSkipPatch) return;
        if (transType != EnumTransitionType.Perish || __instance is not BlockLiquidContainerBase container || !container.IsWaterContainer()) return;
        var contentStack = container.GetContent(pos);
        if (!contentStack.Collectible.IsWaterPortion()) return;
        var exp = IsSinglePlayer(world) ? 0.5f : 1f; // Have to adjust because somehow it gets applied twice in single player
        float multiplier = (float) Math.Pow(WaterContainerBehavior.GetTransitionRateMul(container, transType), exp);
        __result *= multiplier;
    }

    public static bool IsSinglePlayer(IWorldAccessor world)
    {
        return world.Side == EnumAppSide.Client && world.Api is ICoreClientAPI { IsSinglePlayer: true };
    }
}