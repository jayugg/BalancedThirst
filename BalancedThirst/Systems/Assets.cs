using System.Linq;
using BalancedThirst.ModBehavior;
using Vintagestory;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BalancedThirst.Systems;

public class Assets : ModSystem
{
    private ICoreAPI api;

    public override double ExecuteOrder() => 0.3;

    public override void Start(ICoreAPI api) => this.api = api;

    public override void AssetsFinalize(ICoreAPI api)
    {
        if (!api.Side.IsServer())
            return;
        this.AddHydrationToCollectibles(api);
    }
    
    public void AddHydrationToCollectibles(ICoreAPI api)
    {
        foreach (var collectible in api.World.Collectibles.Where(collectible => collectible?.Code != null))
        {
            var hydrationProps = new HydrationProperties();
            float saturation;
            bool shouldAddHydration = false;
            
            if (collectible.NutritionProps != null)
            {
                saturation = collectible.NutritionProps.Satiety;
                switch (collectible.NutritionProps.FoodCategory)
                {
                    case EnumFoodCategory.Fruit:
                        hydrationProps.Hydration += 0.3f * saturation;
                        shouldAddHydration = true;
                        break;
                    case EnumFoodCategory.Vegetable:
                        hydrationProps.Hydration += 0.2f * saturation;
                        shouldAddHydration = true;
                        break;
                    case EnumFoodCategory.Dairy:
                        hydrationProps.Hydration += 0.1f * saturation;
                        shouldAddHydration = true;
                        break;
                    case EnumFoodCategory.Protein:
                        hydrationProps.Hydration += 0.1f * saturation;
                        shouldAddHydration = true;
                        break;
                    case EnumFoodCategory.Grain:
                        hydrationProps.Hydration -= 0.2f * saturation;
                        shouldAddHydration = true;
                        break;
                }
            }

            if (collectible.Code.ToString().Contains("game:waterportion"))
            {
                hydrationProps.Hydration = 100;
                shouldAddHydration = true;
            }
            if (collectible.Code.ToString().Contains("game:rawjuice") ||
                collectible.Code.ToString().Contains("game:milkportion"))
            {
                hydrationProps.Hydration = 90;
                shouldAddHydration = true;
            }
            if (collectible.Code.ToString().Contains("game:vinegarportion") || collectible.Code.ToString().Contains("game:cider"))
            {
                hydrationProps.Hydration = 60;
                shouldAddHydration = true;
            }
            if (collectible.Code.ToString().Contains("game:spirit"))
            {
                hydrationProps.Hydration = 20;
                shouldAddHydration = true;
            }
            if (collectible.Code.ToString().Contains("game:honeyportion"))
            {
                hydrationProps.Hydration = 10;
                shouldAddHydration = true;
            }
            if (collectible.Code.ToString().Contains("game:boilingwaterportion"))
            {
                hydrationProps.Hydration = 100;
                hydrationProps.Scalding = true;
                hydrationProps.Purity = 0.99f;
                shouldAddHydration = true;
            }
            if (collectible.Code.ToString().Contains("game:saltwaterportion"))
            {
                hydrationProps.Hydration = 75;
                hydrationProps.Salty = true;
                shouldAddHydration = true;
            }
            if (!shouldAddHydration)
            {
                continue;
            }
            BtCore.Logger.Warning("Adding drinkable behavior to collectible: " + collectible.Code);
            var behavior = new DrinkableBehavior(collectible);
            collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
            collectible.SetHydrationProperties(hydrationProps);
        }

        foreach (Block block in api.World.Blocks)
        {
            if (block?.Code == null)
            {
                continue;
            }

            var hydrationProps = new HydrationProperties();
            var shouldAddHydration = false;
            if (block.Code.ToString().Contains("game:water"))
            {
                hydrationProps.Hydration = 100;
                hydrationProps.Purity = 0.9f;
                shouldAddHydration = true;
            }
            if (block.Code.ToString().Contains(BtCore.Modid + ":purewater"))
            {
                hydrationProps.Hydration = 100;
                hydrationProps.Purity = 1;
                shouldAddHydration = true;
            }
            if (block.Code.ToString().Contains("game:boilingwater"))
            {
                hydrationProps.Hydration = 100;
                hydrationProps.Scalding = true;
                hydrationProps.Purity = 0.99f;
                shouldAddHydration = true;
            }
            if (block.Code.ToString().Contains("game:saltwater"))
            {
                hydrationProps.Hydration = 75;
                hydrationProps.Purity = 1;
                hydrationProps.Salty = true;
                shouldAddHydration = true;
            }
            if (block is BlockLiquidContainerBase)
            {
                shouldAddHydration = true;
            }
            if (!shouldAddHydration)
            {
                continue;
            }
            BtCore.Logger.Warning("Adding hydration properties to block: " + block.Code);
            block.SetHydrationProperties(hydrationProps);
        }
    }
}