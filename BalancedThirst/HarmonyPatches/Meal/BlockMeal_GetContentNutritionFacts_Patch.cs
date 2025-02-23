using System;
using System.Text;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BalancedThirst.HarmonyPatches.Meal;

public class BlockMeal_GetContentNutritionFacts_Patch
{
    private static bool ShouldSkipPatch => !ConfigSystem.ConfigServer.EnableThirst;
    
    public static void Postfix(
        string __result,
        IWorldAccessor world,
        ItemSlot inSlotorFirstSlot,
        ItemStack[] contentStacks,
        EntityAgent forEntity,
        bool mulWithStacksize = false,
        float nutritionMul = 1f,
        float healthMul = 1f)
    {
        if (ShouldSkipPatch) return;
        if (contentStacks == null || contentStacks.Length == 0) return;
        HydrationProperties hydrationProps = contentStacks.GetHydrationProperties(world, forEntity);
        if (hydrationProps == null) return;
        string hydrationText = Lang.Get($"{BtCore.Modid}:blockinfo-meal-hyd", hydrationProps.Hydration);
        StringBuilder dsc = new(__result);
        dsc.AppendLine($"- {hydrationText}");
        __result = dsc.ToString();
    }
}