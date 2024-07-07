using BalancedThirst.Blocks;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.EntityFirepit;

public class BlockEntityFirepit_OnBurnTick_Patch
{
    public static void Postfix(BlockEntityFirepit __instance, float dt)
    {
        if (!__instance.IsBurning || !__instance.canSmeltInput()) return;
        if (__instance.inputStack?.Collectible is not BlockKettle kettle) return;
        if (__instance.inputStackCookingTime > (double) __instance.maxCookingTime())
        {
            __instance.smeltItems();
        }
    }
}