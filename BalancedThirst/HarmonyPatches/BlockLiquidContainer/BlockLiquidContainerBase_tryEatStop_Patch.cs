using System;
using System.Collections.Generic;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.BlockLiquidContainer;

public class BlockLiquidContainerBase_tryEatStop_Patch
{
    private static bool ShouldSkipPatch()
    {
        return BtCore.ConfigServer.YieldThirstManagementToHoD;
    }
    static bool _alreadyCalled = false;
    private static Dictionary<string, HydrationProperties> _capturedProperties = new();
    
    public static void Prefix(BlockLiquidContainerBase __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        if (ShouldSkipPatch())
        {
            return;
        }
        _alreadyCalled = false;
        if (!(byEntity.World is IServerWorldAccessor) || (double) secondsUsed < 0.95)
            return;
        var collObj = slot.Itemstack.Collectible;

        HydrationProperties hydrationProps = collObj.GetHydrationProperties(slot.Itemstack, byEntity);
        BtCore.Logger.Warning("HydrProps:" + hydrationProps?.Hydration.ToString() ?? "null");
        if (hydrationProps == null || byEntity is not EntityPlayer player) return;
        float val1 = 1f;
        float currentLitres = __instance.GetCurrentLitres(slot.Itemstack);
        float val2 = currentLitres * slot.StackSize;
        if (currentLitres > (double) val1)
        {
            hydrationProps.Hydration /= currentLitres;
            hydrationProps.HydrationLossDelay /= currentLitres;
        }
                
        TransitionState transitionState = collObj.UpdateAndGetTransitionState(byEntity.Api.World, slot, EnumTransitionType.Perish);
        double spoilState = transitionState?.TransitionLevel ?? 0.0;
        float spoilageFactor = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
        hydrationProps.Hydration *= spoilageFactor;
        hydrationProps.EuhydrationWeight *= spoilageFactor;
        hydrationProps.HydrationLossDelay *= spoilageFactor;
        _capturedProperties[player.PlayerUID] = hydrationProps;
        float num3 = Math.Min(val1, val2);
        __instance.TryTakeLiquid(slot.Itemstack, num3 / (float) slot.Itemstack.StackSize);
        BtCore.Logger.Warning("Prefix" + hydrationProps.Hydration);
    }
    
    public static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        BtCore.Logger.Warning("Postfix");
        if (ShouldSkipPatch())
        {
            return;
        }
        if (_alreadyCalled) return;
        _alreadyCalled = true;
        BtCore.Logger.Warning("Postfix2");
        if (byEntity is not EntityPlayer player || !_capturedProperties.ContainsKey(player.PlayerUID)) return;

        var api = byEntity.World?.Api;
        BtCore.Logger.Warning("Postfix3");
        if (api is not { Side: EnumAppSide.Server }) return;

        byEntity.ReceiveHydration(_capturedProperties[player.PlayerUID]);
        _capturedProperties.Remove(player.PlayerUID);
    }
}
