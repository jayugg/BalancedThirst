using BalancedThirst.Blocks;

namespace BalancedThirst.HarmonyPatches.InvSmelting;

public class InventorySmelting_GetOutputText_Patch
{
    public static void Postfix(ref string __result, Vintagestory.GameContent.InventorySmelting __instance)
    {
        if (__instance[1].Itemstack?.Collectible is BlockKettle)
        {
            __result = (__instance[1].Itemstack.Collectible as BlockKettle)?.GetOutputText(__instance.Api.World, __instance);
        }
    }
}