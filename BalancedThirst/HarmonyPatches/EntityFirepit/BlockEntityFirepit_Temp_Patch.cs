using System;
using BalancedThirst.Blocks;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.EntityFirepit;

public class BlockEntityFirepit_Temp_Patch
{
    public static void GetTemp(BlockEntityFirepit __instance, ref float __result, ItemStack stack)
    {
        if (stack?.Collectible is not BlockKettle) return;

        var inventory = __instance?.GetField<InventorySmelting>("inventory");
        if (inventory == null) return;
        if (inventory.CookingSlots == null || inventory.CookingSlots.Length == 0)
        {
            __result = stack.Collectible.GetTemperature(__instance?.Api?.World, stack);
            return;
        }
        float temperature = stack.Collectible.GetTemperature(__instance?.Api?.World, stack);
        for (int index = 0; index < inventory.CookingSlots.Length; ++index)
        {
            ItemStack itemstack = inventory.CookingSlots[index]?.Itemstack;
            if (itemstack?.Collectible != null)
            {
                temperature = Math.Min(itemstack.Collectible.GetTemperature(__instance.Api.World, itemstack), temperature);
            }
        }
        __result = temperature;
    }

    
    public static void SetTemp(BlockEntityFirepit __instance, ItemStack stack, float value)
    {
        if (stack.Collectible is not BlockKettle) return;
        var inventory = __instance.GetField<InventorySmelting>("inventory");
        if (inventory == null) return;
        stack.Collectible?.SetTemperature(__instance.Api.World, stack, value);
        if (inventory.CookingSlots.Length != 0)
        {
            for (int index = 0; index < inventory.CookingSlots.Length; ++index)
                inventory.CookingSlots[index].Itemstack?.Collectible.SetTemperature(__instance.Api.World, inventory.CookingSlots[index].Itemstack, value);
        }
    }
}