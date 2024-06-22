using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches;

public class CookingRecipe_Matches_Patch
{
    public static void Postfix(CookingRecipe __instance, ref bool __result, ItemStack[] inputStacks,
        ref int quantityServings)
    {
        bool allIngredientsAreLiquids = inputStacks.All(itemStack =>
        {
            return itemStack.Collectible.IsLiquid();
        });
        
        var myRecipeIngredients = new List<string> { "game:waterportion" };

        bool allIngredientsMatchMyRecipe = inputStacks.All(itemStack =>
        {
            return myRecipeIngredients.Contains(itemStack.Collectible.Code.ToString());
        });
        

        if (allIngredientsAreLiquids && allIngredientsMatchMyRecipe)
        {
            for (int i = 0; i < inputStacks.Length; i++)
            {
                var stack = inputStacks[i];
                if (stack == null) continue;

                int qportions = stack.StackSize;

                if (stack.Collectible.Attributes?["waterTightContainerProps"].Exists == true)
                {
                    var props = BlockLiquidContainerBase.GetContainableProps(stack);
                    if (props == null) continue;
                    qportions = (int)(stack.StackSize / props.ItemsPerLitre / __instance.GetIngrendientFor(stack).PortionSizeLitres);
                }
                if (qportions <= 0) continue;
                quantityServings = qportions;
                __result = true;
            }
        }
    }
}