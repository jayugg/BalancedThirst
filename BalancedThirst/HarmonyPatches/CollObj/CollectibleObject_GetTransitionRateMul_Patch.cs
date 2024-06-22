using Vintagestory.API.Common;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_GetTransitionRateMul_Patch
{
    public static void Postfix(
        CollectibleObject __instance,
        ref float __result,
        IWorldAccessor world,
        ItemSlot inSlot,
        EnumTransitionType transType)
    {
        var stack = inSlot.Itemstack;
        if (!stack.Collectible.IsWaterPortion()) return;
        __result = 1f;
    }
}