using System.Linq;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.Util;

public static class EditAssets
{
    public static void AddHydrationToCollectibles(ICoreAPI api)
    {
        foreach (var collectible in api.World.Collectibles.Where(c => c?.Code != null))
        {
            HydrationProperties hydrationProps = BtCore.ConfigServer.HydratingLiquids.FirstOrDefault(keyVal => collectible.MyWildCardMatch(keyVal.Key)).Value;
            if (hydrationProps != null)
            {
                collectible.AddDrinkableBehavior();
                collectible.SetHydrationProperties(hydrationProps);
            }
        }
        foreach (var block in api.World.Blocks.Where(b => b?.Code != null))
        {
            HydrationProperties hydrationProps = BtCore.ConfigServer.HydratingBlocks.FirstOrDefault(keyVal => block.MyWildCardMatch(keyVal.Key)).Value;
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