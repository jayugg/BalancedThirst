using System;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.BlockLiquidContainer;

public class BlockLiquidContainerBase_tryEatStop_Patch
{
    public static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        var container = (BlockLiquidContainerBase) slot.Itemstack.Collectible;
        if (container == null) return;
        HydrationProperties hydrationProperties = container.GetHydrationProperties(slot.Itemstack, byEntity);
        if (!(byEntity.World is IServerWorldAccessor) || hydrationProperties == null || secondsUsed < 0.949999988079071) return;
        float val1 = 1f;
        float currentLitres = container.GetCurrentLitres(slot.Itemstack);
        float val2 = currentLitres * slot.StackSize;
        if (currentLitres > (double) val1)
        {
            hydrationProperties.Hydration /= currentLitres;
            hydrationProperties.HydrationLossDelay /= currentLitres;
        }
        TransitionState transitionState = container.UpdateAndGetTransitionState(byEntity.World, slot, EnumTransitionType.Perish);
        double spoilState = transitionState?.TransitionLevel ?? 0.0;
        float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
        hydrationProperties.Hydration *= num1;
        hydrationProperties.HydrationLossDelay *= num1;
        byEntity.ReceiveHydration(hydrationProperties);
        IPlayer player = null;
        if (byEntity is EntityPlayer entityPlayer) player = entityPlayer.World.PlayerByUid(entityPlayer.PlayerUID);
        float num3 = Math.Min(val1, val2);
        container.TryTakeLiquid(slot.Itemstack, num3 / slot.Itemstack.StackSize);
        slot.MarkDirty();
        player?.InventoryManager.BroadcastHotbarSlot();
    }
}