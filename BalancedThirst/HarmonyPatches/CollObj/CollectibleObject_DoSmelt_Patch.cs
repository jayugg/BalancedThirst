using System;
using BalancedThirst.Systems;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_DoSmelt_Patch
{
    public static bool ShouldSkipPatch => !ConfigSystem.SyncedConfigData.BoilWaterInFirepits;
    public static bool Prefix(
        CollectibleObject __instance,
        IWorldAccessor world,
        ISlotProvider cookingSlotsProvider,
        ItemSlot inputSlot,
        ItemSlot outputSlot)
    {
        if (ShouldSkipPatch) return true;
        if (__instance is not BlockLiquidContainerBase container) return true;
        var contentStack = container.GetContent(inputSlot.Itemstack); ItemStack stack = contentStack?.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack.Clone();
        if (stack == null) return true;
        var contentSize = contentStack.StackSize;
        
        TransitionState transitionState1 = __instance.UpdateAndGetTransitionState(world, new DummySlot(inputSlot.Itemstack), EnumTransitionType.Perish);
        if (transitionState1 != null)
        {
            TransitionState transitionState2 = stack.Collectible.UpdateAndGetTransitionState(world, new DummySlot(stack), EnumTransitionType.Perish);
            float val2 = (float) ( transitionState1.TransitionedHours / (transitionState1.TransitionHours + (double) transitionState1.FreshHours) * 0.8 * ( transitionState2.TransitionHours + (double) transitionState2.FreshHours) - 1.0);
            stack.Collectible.SetTransitionState(stack, EnumTransitionType.Perish, Math.Max(0.0f, val2));
        }
        stack.StackSize = contentSize;
        var outStack = new ItemStack(container);
        container.SetContent(outStack, stack);
        if (outputSlot.Itemstack == null)
        {
            outputSlot.Itemstack = outStack;
        }
        else
        {
            stack.StackSize = contentSize;
            ItemStackMergeOperation op = new ItemStackMergeOperation(world, EnumMouseButton.Left, (EnumModifierKey) 0, EnumMergePriority.ConfirmedMerge, 1);
            op.SourceSlot = new DummySlot(outStack);
            op.SinkSlot = new DummySlot(outputSlot.Itemstack);
            container.TryMergeStacks(op);
            outputSlot.Itemstack = op.SinkSlot.Itemstack;
        }
        inputSlot.Itemstack.StackSize -= 1;
        if (inputSlot.Itemstack.StackSize <= 0)
            inputSlot.Itemstack = null;
        outputSlot.MarkDirty();
        return false;
    }
}