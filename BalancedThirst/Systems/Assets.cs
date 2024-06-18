using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BalancedThirst.Systems;

public static class Assets
{
    public static void AddHydrationToCollectibles(ICoreAPI api)
    {
        foreach (var collectible in api.World.Collectibles)
        {
            if (collectible?.Code == null)
            {
                continue;
            }
            if (collectible is BlockLiquidContainerBase)
            {
                BtCore.Logger.Warning("Adding drinkable behavior to container: " + collectible.Code);
                collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(new DrinkableBehavior(collectible));
                continue;
            }
            
            Dictionary<string, HydrationProperties> hydrationDictionary = new Dictionary<string, HydrationProperties>
            {
                { "game:waterportion", new HydrationProperties { Hydration = 100 } },
                { "game:rawjuice", new HydrationProperties { Hydration = 90 } },
                { "game:milkportion", new HydrationProperties { Hydration = 90 } },
                { "game:vinegarportion", new HydrationProperties { Hydration = 60 } },
                { "game:cider", new HydrationProperties { Hydration = 60 } },
                { "game:spirit", new HydrationProperties { Hydration = 20 } },
                { "game:honeyportion", new HydrationProperties { Hydration = 10 } },
                { "game:boilingwaterportion", new HydrationProperties { Hydration = 100, Scalding = true, Purity = 0.99f } },
                { "game:saltwaterportion", new HydrationProperties { Hydration = 75, Salty = true } }
            };

            string code = collectible.Code.ToString();

            foreach (var pair in hydrationDictionary)
            {
                if (code.Contains(pair.Key))
                {
                    HydrationProperties hydrationProps = pair.Value;
                    BtCore.Logger.Warning("Adding drinkable behavior and hydration to collectible: " + collectible.Code);
                    var behavior = new DrinkableBehavior(collectible);
                    collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
                    collectible.SetHydrationProperties(hydrationProps);
                    break;
                }
            }
        }

        
        /*foreach (Block block in api.World.Blocks)
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
            if (!shouldAddHydration)
            {
                continue;
            }
            BtCore.Logger.Warning("Adding hydration properties to block: " + block.Code);
            block.SetHydrationProperties(hydrationProps);
        }
        */
        
    }
}