using BalancedThirst.Blocks;

namespace BalancedThirst.HarmonyPatches.InvSmelting;

public class InventorySmelting_GetOutputText_Patch
{
    public static void Postfix(ref string __result, Vintagestory.GameContent.InventorySmelting __instance)
    {
        if (__instance[1].Itemstack?.Collectible is BlockKettle kettle)
        {
            if (kettle.CanSmelt(__instance.Api.World, __instance, __instance[1].Itemstack, null))
                __result = kettle.GetOutputText(__instance.Api.World, __instance);
        }
    }
}