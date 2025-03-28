using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.BlockLiquidContainer;
public static class BlockLiquidContainerBase_SpillContents_Transpiler
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        BtCore.Logger.Warning("Transpiling BlockLiquidContainerBase.SpillContents");
        var codes = new List<CodeInstruction>(instructions);
        for (var i = 0; i < codes.Count; i++)
        {
            // Find the instruction where the currentLitres is compared to 10.0
            if (codes[i].opcode != OpCodes.Ldc_R8 || !(Math.Abs((double)codes[i].operand - 10.0) < 1e-3f)) continue;
            // Insert the extra check before this comparison
            codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0)); // Load containerSlot
            codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, typeof(BlockLiquidContainerBase_SpillContents_Transpiler).GetMethod("ExtraCheck", BindingFlags.Public | BindingFlags.Static))); // Call ExtraCheck method
            i += 2; // Skip the inserted instructions
        }
        return codes;
    }

    public static bool ExtraCheck(ItemSlot containerSlot)
    {
        BtCore.Logger.Warning("ExtraCheck");
        if (containerSlot.Itemstack?.Block is not BlockLiquidContainerBase container) return true;
        var contentStack = container.GetContent(containerSlot.Itemstack);
        return !contentStack.Collectible.IsWaterPortion();
    }
}