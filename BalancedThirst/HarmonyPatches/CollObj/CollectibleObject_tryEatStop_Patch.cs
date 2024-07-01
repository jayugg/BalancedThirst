using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_tryEatStop_Patch
{
    private static bool ShouldSkipPatch()
    {
        return BtCore.ConfigServer.YieldThirstManagementToHoD;
    }
    
    static bool _alreadyCalled = false;
    private static Dictionary<string, HydrationProperties> _capturedProperties = new();
    
    public static void Prefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        if (ShouldSkipPatch())
        {
            return;
        }   
        _alreadyCalled = false;
        var api = byEntity?.World?.Api;
        if (api is not { Side: EnumAppSide.Server } || slot?.Itemstack == null) return;
        var collObj = slot.Itemstack.Collectible;
        if (!(secondsUsed >= 0.95f)) return;
        HydrationProperties hydrationProps = collObj.GetHydrationProperties(slot.Itemstack, byEntity);
        if (hydrationProps == null || byEntity is not EntityPlayer player) return;
        TransitionState transitionState = collObj.UpdateAndGetTransitionState(byEntity.Api.World, slot, EnumTransitionType.Perish);
        double spoilState = transitionState?.TransitionLevel ?? 0.0;
        float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
        hydrationProps.Hydration *= num1;
        hydrationProps.EuhydrationWeight *= num1;
        hydrationProps.HydrationLossDelay *= num1;
        _capturedProperties[player.PlayerUID] = hydrationProps;
    }
    public static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        if (ShouldSkipPatch())
        {
            return;
        }
        if (_alreadyCalled) return;
        _alreadyCalled = true;
        if (byEntity is not EntityPlayer player || !_capturedProperties.ContainsKey(player.PlayerUID)) return;
        var api = byEntity.World?.Api;
        if (api is not { Side: EnumAppSide.Server }) return;
        byEntity.ReceiveHydration(_capturedProperties[player.PlayerUID]);
        _capturedProperties.Remove(player.PlayerUID);
    }
}