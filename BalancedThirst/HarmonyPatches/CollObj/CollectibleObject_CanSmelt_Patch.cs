using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_CanSmelt_Patch
{
    public static void Postfix(
        CollectibleObject __instance,
        ref bool __result,
        IWorldAccessor world,
        ISlotProvider cookingSlotsProvider,
        ItemStack inputStack,
        ItemStack outputStack)
    {
        if (__instance is not BlockLiquidContainerBase container) return;
        if (!__instance.IsHeatableLiquidContainer()) return;
        var contentStack = container.GetContent(inputStack);
        ItemStack resolvedItemstack = contentStack?.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
        if (resolvedItemstack == null ||
            inputStack.StackSize < contentStack?.Collectible.CombustibleProps?.SmeltedRatio ||
            contentStack?.Collectible.CombustibleProps?.MeltingPoint <= 0)
        {
            __result = false;
            return;
        }
        __result = outputStack == null;
    }
}