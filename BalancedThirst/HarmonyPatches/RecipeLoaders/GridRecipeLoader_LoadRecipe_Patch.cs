using System.Linq;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace BalancedThirst.HarmonyPatches.RecipeLoaders;

public class GridRecipeLoader_LoadRecipe_Patch
{
    public static AssetLocation SkipPatch = new AssetLocation("skippatch");

    public static void Postfix(GridRecipeLoader __instance, AssetLocation loc, GridRecipe recipe)
    {
        if (loc == SkipPatch || !recipe.Enabled)
        {
            return;
        }

        var api = __instance.GetField<ICoreServerAPI>("api");
        if (!recipe.Ingredients.Any(ing =>
                (ing.Value.Attributes?["ucontents"]?.AsObject<ItemStack[]>() is { } contents && contents.Any(content =>
                    content.Collectible.Code.PathStartsWith("game:waterportion"))) ||
                ing.Value.Code.PathStartsWith("game:waterportion"))) return;

        foreach (var ingredient in recipe.Ingredients)
        {
            if (ingredient.Value.Attributes?["ucontents"].AsObject<ItemStack[]>() is { } contents)
            {
                foreach (var content in contents)
                {
                    if (content.Collectible.Code.PathStartsWith("game:waterportion"))
                    {
                        CreateNewRecipe(recipe, ingredient.Key, content.Collectible.Code);
                    }
                }
            }

            if (ingredient.Value.Code.PathStartsWith("game:waterportion"))
            {
                CreateNewRecipe(recipe, ingredient.Key, ingredient.Value.Code);
            }
        }

        void CreateNewRecipe(GridRecipe originalRecipe, string ingredientKey, AssetLocation waterportionCode)
        {
            var newRecipe = new GridRecipe()
            {
                Name = new AssetLocation(originalRecipe.Name.Domain, originalRecipe.Name.Path + "_morewater"),
                Ingredients = originalRecipe.Ingredients
                    .ToDictionary(
                        ing => ing.Key, // Use the original key
                        ing => ing.Key == ingredientKey
                            ? new CraftingRecipeIngredient()
                            {
                                Type = EnumItemClass.Item,
                                Code = new AssetLocation("balancedthirst:waterportion-*"),
                                IsWildCard = true,
                                SkipVariants = new[] { "stagnant" }
                            }
                            : ing.Value
                    ),
                Output = originalRecipe.Output,
            };
            __instance.LoadRecipe(SkipPatch, newRecipe);
        }
    }
}