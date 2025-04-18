using System;
using System.Collections.Generic;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_OnHeldInteractStop_Patch
{
    private static bool ShouldSkipPatch => !ConfigSystem.ConfigServer.EnableThirst;

    private static bool _alreadyCalled = false;
    private static Dictionary<string, Tuple<ItemSlot, float, float>> _capturedSlot = new();
    
    public static void Prefix(
        CollectibleObject __instance,
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel)
    {
        if (ShouldSkipPatch) return;
        if (__instance is not BlockLiquidContainerBase container || slot?.Itemstack?.Collectible is not BlockLiquidContainerBase) return;
        _alreadyCalled = false;
        if (!(byEntity.World is IServerWorldAccessor) || secondsUsed < 0.95)
            return;
        var collObj = slot.Itemstack.Collectible;
        var hydrationProps = collObj.GetHydrationProperties(byEntity.World, slot.Itemstack, byEntity);
        if (hydrationProps == null || byEntity is not EntityPlayer player) return;
        var litresToDrink = ConfigSystem.ConfigServer.ContainerDrinkSpeed;
        var currentLitres = container.GetCurrentLitres(slot.Itemstack);
        var litresDrank = Math.Min(currentLitres, litresToDrink);
        var slotClone = new ItemSlot(slot.Inventory) { Itemstack = slot.Itemstack.Clone() };
        _capturedSlot[player.PlayerUID] = new Tuple<ItemSlot, float, float>(slotClone, currentLitres, litresDrank);
    }
    
    public static void Postfix(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel)
    {
        if (ShouldSkipPatch) return;
        if (slot?.Itemstack?.Collectible is not BlockLiquidContainerBase) return;
        if (!(byEntity.World is IServerWorldAccessor) || secondsUsed < 0.95)
            return;
        if (_alreadyCalled) return;
        _alreadyCalled = true;
        if (byEntity is not EntityPlayer player) return;
        if (!_capturedSlot.TryGetValue(player.PlayerUID, out var slotInfo)) return;
        var slotClone = new ItemSlot(slotInfo.Item1.Inventory) { Itemstack = slotInfo.Item1.Itemstack.Clone() };
        AdjustLiquidContents(slotInfo, out var retrievedSlot, out var litresDrank);
        HydrateFromContainer(slotClone, litresDrank, player);
    }

    private static void AdjustLiquidContents(Tuple<ItemSlot, float, float> slotInfo, out ItemSlot retrievedSlot, out float litresDrank)
    {
        retrievedSlot = slotInfo.Item1;
        var previousLitres = slotInfo.Item2;
        litresDrank = slotInfo.Item3;
        var block = retrievedSlot.Itemstack.Collectible as BlockLiquidContainerBase;
        var desiredLitres = previousLitres - litresDrank;
        var currentLitres = block?.GetCurrentLitres(retrievedSlot.Itemstack) ?? 0;
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
        var hydrationProps = container?.GetHydrationPropsPerLitre(player.World, retrievedSlot.Itemstack, player);
        if (hydrationProps == null) return;
        hydrationProps *= litresDrank;
                
        var transitionState = container.UpdateAndGetTransitionState(player.Api.World, retrievedSlot, EnumTransitionType.Perish);
        var spoilState = transitionState?.TransitionLevel ?? 0.0;
        var spoilageFactor = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, retrievedSlot.Itemstack, player);
        hydrationProps *= spoilageFactor;
        hydrationProps.EuhydrationWeight *= spoilageFactor;
        player.ReceiveHydration(hydrationProps);
    }
}