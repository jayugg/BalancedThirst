using System;
using System.Collections.Generic;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.BlockLiquidContainer;

public class BlockLiquidContainerBase_tryEatStop_Patch
{
    private static bool ShouldSkipPatch => !ConfigSystem.SyncedConfigData.EnableThirst;
    
    static bool _alreadyCalled = false;
    private static Dictionary<string, Tuple<ItemSlot, float, float>> _capturedSlot = new();
    private static Dictionary<string, float> _capturedSaturation = new();
    
    public static void Prefix(BlockLiquidContainerBase __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        if (ShouldSkipPatch) return;
        _alreadyCalled = false;
        if (!(byEntity.World is IServerWorldAccessor) || secondsUsed < 0.95)
            return;
        var collObj = slot.Itemstack.Collectible;
        HydrationProperties hydrationProps = collObj.GetHydrationProperties(slot.Itemstack, byEntity);
        if (hydrationProps == null || byEntity is not EntityPlayer player) return;
        float litresToDrink = ConfigSystem.ConfigServer.ContainerDrinkSpeed;
        float currentLitres = __instance.GetCurrentLitres(slot.Itemstack);
        float litresDrank = Math.Min(currentLitres, litresToDrink);
        var slotClone = new ItemSlot(slot.Inventory) { Itemstack = slot.Itemstack.Clone() };
        _capturedSlot[player.PlayerUID] = new Tuple<ItemSlot, float, float>(slotClone, currentLitres, litresDrank);
    }
    
    public static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        if (ShouldSkipPatch) return;
        if (!(byEntity.World is IServerWorldAccessor) || secondsUsed < 0.95)
            return;
        if (_alreadyCalled) return;
        _alreadyCalled = true;
        if (byEntity is not EntityPlayer player) return;
        if (!_capturedSlot.TryGetValue(player.PlayerUID, out Tuple<ItemSlot, float, float> slotInfo)) return;
        
        AdjustLiquidContents(slotInfo, out ItemSlot retrievedSlot, out var litresDrank);
        HydrateFromContainer(retrievedSlot, litresDrank, player);
    }

    private static void AdjustLiquidContents(Tuple<ItemSlot, float, float> slotInfo, out ItemSlot retrievedSlot, out float litresDrank)
    {
        retrievedSlot = slotInfo.Item1;
        float previousLitres = slotInfo.Item2;
        litresDrank = slotInfo.Item3;
        BlockLiquidContainerBase block = retrievedSlot.Itemstack.Collectible as BlockLiquidContainerBase;
        float desiredLitres = previousLitres - litresDrank;
        float currentLitres = block?.GetCurrentLitres(retrievedSlot.Itemstack) ?? 0;
        var contentStack = block?.GetContent(retrievedSlot.Itemstack);
        if (desiredLitres > currentLitres)
        {
            block?.TryPutLiquid(retrievedSlot.Itemstack, contentStack, desiredLitres - currentLitres);
        }
        else
        {
            block?.TryTakeLiquid(retrievedSlot.Itemstack, currentLitres - desiredLitres);
        }
    }

    private static void HydrateFromContainer(ItemSlot retrievedSlot, float litresDrank, EntityPlayer player)
    {
        var container = retrievedSlot.Itemstack.Collectible as BlockLiquidContainerBase;
        HydrationProperties hydrationProps = container?.GetHydrationPropsPerLitre(player.World, retrievedSlot.Itemstack, player);
        if (hydrationProps == null) return;
        hydrationProps.Hydration *= litresDrank;
        hydrationProps.HydrationLossDelay *= litresDrank;
                
        TransitionState transitionState = container.UpdateAndGetTransitionState(player.Api.World, retrievedSlot, EnumTransitionType.Perish);
        double spoilState = transitionState?.TransitionLevel ?? 0.0;
        float spoilageFactor = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, retrievedSlot.Itemstack, player);
        hydrationProps.Hydration *= spoilageFactor;
        hydrationProps.EuhydrationWeight *= spoilageFactor;
        hydrationProps.HydrationLossDelay *= spoilageFactor;
        player.ReceiveHydration(hydrationProps);
    }
}
