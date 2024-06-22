using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BalancedThirst.Items;

public class WaterCookingName : ICookingRecipeNamingHelper
{
    public string GetNameForIngredients(IWorldAccessor worldForResolve, string recipeCode, ItemStack[] stacks)
    {
        return Lang.Get("cookingrecipe-boilingwater");
    }
}