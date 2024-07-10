using System;
using BalancedThirst.Systems;

using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BalancedThirst.HarmonyPatches.BlockLiquidContainer;
public static class BlockLiquidContainerBase_tryEatStop_Transpiler
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            // Look for the ldc.r4 opcode which loads a float32 (in this case, 1f) onto the evaluation stack
            if (codes[i].opcode == OpCodes.Ldc_R4 && Math.Abs((float)codes[i].operand - 1f) < 0.0001)
            {
                codes[i].operand = ConfigSystem.SyncedConfigData.ContainerDrinkSpeed;
                break; // Assuming only one occurrence needs to be changed
            }
        }
        return codes;
    }
}