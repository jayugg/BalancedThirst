using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.ServerMods;
using Core = Vintagestory.GameContent.Core;

namespace BalancedThirst.Systems;

public class Recipes : ModSystem
{
    public GridRecipeLoader GridRecipeLoader { get; set; }

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

    public override double ExecuteOrder() => 1.01;

    public override void AssetsLoaded(ICoreAPI api)
    {
        if (api.Side != EnumAppSide.Server)
        {
            return;
        }

        GridRecipeLoader = api.ModLoader.GetModSystem<GridRecipeLoader>();
        AddPureWaterToRecipes(api);
    }
    
    public static void AddPureWaterToRecipes(ICoreAPI api)
    {
        foreach (var recipe in api.World.GridRecipes)
        {
            var waterIngredientKey = recipe.Ingredients.FirstOrDefault(ingredient =>
                ingredient.Value.ResolvedItemstack?.Collectible?.Code?.ToString() == "game:waterportion").Key;

            if (waterIngredientKey != null)
            {
                var pureWaterIngredient = new CraftingRecipeIngredient()
                {
                    Type = EnumItemClass.Item,
                    Code = new AssetLocation("balancedthirst:purewaterportion"),
                    Quantity = recipe.Ingredients[waterIngredientKey].Quantity
                };
                
                recipe.Ingredients[waterIngredientKey] = pureWaterIngredient;
            }
        }
    }
}