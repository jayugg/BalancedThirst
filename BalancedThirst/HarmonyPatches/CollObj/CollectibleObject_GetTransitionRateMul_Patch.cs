using System.Linq;
using BalancedThirst.Util;
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
        if (!stack.Collectible.IsWaterPortion() || !__instance.IsWaterContainer()) return;
        __result = __result*GetWaterContainerTransitionMultiplier(__instance);
    }
    
    public static float GetWaterContainerTransitionMultiplier(CollectibleObject collectible) { return BtCore.ConfigServer.WaterContainers.FirstOrDefault(keyVal => collectible.MyWildCardMatch(keyVal.Key)).Value; }
}