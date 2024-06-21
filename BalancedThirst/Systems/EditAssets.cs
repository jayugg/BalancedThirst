using System.Collections.Generic;
using System.Linq;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace BalancedThirst.Systems;

public static class EditAssets
{ // Todo: fix recipes, change boiling water pickup item to hot water
    public static void AddHydrationToCollectibles(ICoreAPI api)
    {
        foreach (var collectible in api.World.Collectibles.Where(c => c?.Code != null))
        {
            Dictionary<string, HydrationProperties> hydrationDictionary = new Dictionary<string, HydrationProperties>
            {
                { "game:waterportion", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Okay } },
                { "game:rawjuice", new HydrationProperties { Hydration = 90 } },
                { "game:milkportion", new HydrationProperties { Hydration = 90 } },
                { "game:vinegarportion", new HydrationProperties { Hydration = 60 } },
                { "game:cider", new HydrationProperties { Hydration = 60 } },
                { "game:spirit", new HydrationProperties { Hydration = 20 } },
                { "game:honeyportion", new HydrationProperties { Hydration = 10 } },
                { "game:boilingwaterportion", new HydrationProperties { Hydration = 100, Scalding = true, Purity = EnumPurityLevel.Boiled } },
                { "game:saltwaterportion", new HydrationProperties { Hydration = 75, Salty = true, Purity = EnumPurityLevel.Okay } },
                { "game:brineportion", new HydrationProperties { Hydration = 60, Salty = true, Purity = EnumPurityLevel.Okay } },
                { BtCore.Modid + ":waterportion-pure", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Pure } },
                { BtCore.Modid + ":waterportion-stagnant", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Stagnant } },
                { "game:rot", new HydrationProperties { Hydration = 20, Purity = EnumPurityLevel.Yuck } }
            };

            string code = collectible.Code.ToString();

            foreach (var pair in hydrationDictionary)
            {
                if (code.Contains(pair.Key))
                {
                    HydrationProperties hydrationProps = pair.Value;
                    //BtCore.Logger.Warning("Adding drinkable behavior and hydration to collectible: " + collectible.Code);
                    var behavior = new DrinkableBehavior(collectible);
                    collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
                    collectible.SetHydrationProperties(hydrationProps);
                    break;
                }
            }
        }
        
        Dictionary<string, HydrationProperties> hydrationBlockDictionary = new Dictionary<string, HydrationProperties>
        {
            { "game:water", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Okay } },
            { "game:" + BtCore.Modid + "-purewater", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Pure } },
            { "game:boilingwater", new HydrationProperties { Hydration = 100, Scalding = true, Purity = EnumPurityLevel.Boiled } },
            { "game:saltwater", new HydrationProperties { Hydration = 75, Purity = EnumPurityLevel.Okay, Salty = true } }
        };

        foreach (Block block in api.World.Blocks.Where(b => b?.Code != null))
        {
            foreach (var pair in hydrationBlockDictionary)
            {
                if (block.Code.ToString().Contains(pair.Key))
                {
                    HydrationProperties hydrationProps = pair.Value;
                    block.SetHydrationProperties(hydrationProps);
                    break;
                }
            }
        }
    }
    
}