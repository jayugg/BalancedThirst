using System.Collections.Generic;
using System.Linq;
using BalancedThirst.ModBehavior;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BalancedThirst.Systems;

public static class EditAssets
{ // Todo: fix recipes, change boiling water pickup item to hot water
    public static void AddHydrationToCollectibles(ICoreAPI api)
    {
        foreach (var collectible in api.World.Collectibles.Where(c => c?.Code != null))
        {

            string code = collectible.Code.ToString();

            foreach (var pair in BtConstants.HydratingLiquids)
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

        foreach (Block block in api.World.Blocks.Where(b => b?.Code != null))
        {
            foreach (var pair in BtConstants.HydratingBlocks)
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
    
    public static void AddContainerProps(ICoreAPI api)
    {
        foreach (var block in api.World.Blocks) {
        if (block is not BlockLiquidContainerBase container) continue;
        if (!BtConstants.HeatableLiquidContainers.Any(code => block.Code.Path.Contains(code))) continue;

        container.SetAttribute("maxTemperature", 100);
        container.SetAttribute("allowHeating", true);
        }
    }
}