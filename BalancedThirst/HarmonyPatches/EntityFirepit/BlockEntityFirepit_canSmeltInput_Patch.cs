using BalancedThirst.Blocks;
using BalancedThirst.Util;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.EntityFirepit;

public class BlockEntityFirepit_canSmeltInput_Patch
{
    public static void Postfix(BlockEntityFirepit __instance, ref bool __result)
    {
        
        if(__instance.inputStack?.Collectible is not BlockKettle kettle) return;

        var inventory = __instance.GetField<InventorySmelting>("inventory");
        if (__instance.Api?.World != null &&
            __instance.inputSlot?.Itemstack != null &&
            kettle.CanSmelt(__instance.Api.World, inventory, __instance.inputSlot.Itemstack, __instance.outputSlot.Itemstack))
        {
            __result = true;
        }
        else
        {
            __result = false;
        }
        
    }
}