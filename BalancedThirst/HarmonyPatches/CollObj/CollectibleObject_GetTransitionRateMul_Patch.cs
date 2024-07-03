using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Common;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_GetTransitionRateMul_Patch
{
    public static bool ShouldSkipPatch => !ConfigSystem.SyncedConfigData.EnableThirst;
    public static void Postfix(
        CollectibleObject __instance,
        ref float __result,
        IWorldAccessor world,
        ItemSlot inSlot,
        EnumTransitionType transType)
    {
        if (ShouldSkipPatch) return;
        var stack = inSlot.Itemstack;
        if (!stack.Collectible.IsWaterPortion() || !__instance.IsWaterContainer()) return;
        __result *= GetWaterContainerTransitionMultiplier(__instance);
    }

    public static float GetWaterContainerTransitionMultiplier(CollectibleObject collectible)
    {
        return collectible.Attributes?["waterTransitionMul"]?.AsFloat(1) ?? 1;
    }
}