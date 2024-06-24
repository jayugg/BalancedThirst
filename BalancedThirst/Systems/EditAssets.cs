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
            HydrationProperties hydrationProps = BtConstants.HydratingLiquids.FirstOrDefault(keyVal => collectible.WildCardMatchDomain(keyVal.Key)).Value;
            if (hydrationProps != null)
            {
                var behavior = new DrinkableBehavior(collectible);
                collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
                collectible.SetHydrationProperties(hydrationProps);
            }
        }
        foreach (Block block in api.World.Blocks.Where(b => b?.Code != null))
        {
            HydrationProperties hydrationProps = BtCore.ConfigServer.HydratingLiquids.FirstOrDefault(keyVal => block.WildCardMatchDomain(keyVal.Key)).Value;
            if (hydrationProps != null)
            {
                block.SetHydrationProperties(hydrationProps);
            }
        }
    }
    
    public static void AddContainerProps(ICoreAPI api)
    {
        foreach (var block in api.World.Blocks) {
        if (block is not BlockLiquidContainerBase container) continue;
        if (!block.IsHeatableLiquidContainer()) continue;

        container.SetAttribute("maxTemperature", 100);
        container.SetAttribute("allowHeating", true);
        }
    }
}