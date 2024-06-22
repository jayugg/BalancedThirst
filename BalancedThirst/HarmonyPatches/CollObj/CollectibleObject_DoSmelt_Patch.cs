using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_DoSmelt_Patch
{
    public static bool Prefix(
        CollectibleObject __instance,
        IWorldAccessor world,
        ISlotProvider cookingSlotsProvider,
        ItemSlot inputSlot,
        ItemSlot outputSlot)
    {
        world.Api.Logger.Error("Entering DoSmelt method");
        BtCore.Logger.Error("Entering DoSmelt method");
        BtCore.Logger.Warning("DoSmelt");
        BtCore.Logger.Warning("Input slot: {0}", inputSlot?.Itemstack);
        BtCore.Logger.Warning("Called cansmelt, smelting");
        if (__instance is not BlockLiquidContainerBase container) return true;
        if (!__instance.CanSmelt(world, cookingSlotsProvider, inputSlot?.Itemstack, outputSlot.Itemstack) ||
            inputSlot?.StackSize != 1)
            return true;
        var contentStack = container.GetContent(inputSlot.Itemstack);
        BtCore.Logger.Warning("Do: Trying to smelt {0} into {1}", inputSlot.Itemstack, contentStack?.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack);
        ItemStack stack = contentStack?.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack.Clone();
        if (stack == null) return true;
        var contentSize = contentStack.StackSize;
        TransitionState transitionState1 = __instance.UpdateAndGetTransitionState(world, new DummySlot(inputSlot.Itemstack), EnumTransitionType.Perish);
        if (transitionState1 != null)
        {
            TransitionState transitionState2 = stack.Collectible.UpdateAndGetTransitionState(world, new DummySlot(stack), EnumTransitionType.Perish);
            float val2 = (float) ( transitionState1.TransitionedHours / (transitionState1.TransitionHours + (double) transitionState1.FreshHours) * 0.800000011920929 * ( transitionState2.TransitionHours + (double) transitionState2.FreshHours) - 1.0);
            stack.Collectible.SetTransitionState(stack, EnumTransitionType.Perish, Math.Max(0.0f, val2));
        }
        var smeltRatio = contentStack.Collectible.CombustibleProps.SmeltedRatio;
        stack.StackSize = contentSize / smeltRatio;
        if (outputSlot.Itemstack != null) return true;
        outputSlot.Itemstack = new ItemStack(container);
        if (outputSlot.Itemstack.Collectible is not BlockLiquidContainerBase outContainer) return true;
        outContainer.SetContent(outputSlot.Itemstack, stack);
        inputSlot.Itemstack.StackSize -= 1;
        if (inputSlot.Itemstack.StackSize <= 0)
            inputSlot.Itemstack = null;
        outputSlot.MarkDirty();
        return false;
    }
}