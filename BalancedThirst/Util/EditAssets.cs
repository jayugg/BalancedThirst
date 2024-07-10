using System.Linq;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.Util;

public static class EditAssets
{
    public static void AddHydrationToCollectibles(ICoreAPI api)
    {
        BtCore.Logger.Notification("Adding hydration properties to collectibles");
        foreach (var collectible in api.World.Collectibles.Where(c => c?.Code != null))
        {
            HydrationProperties hydrationProps = ConfigSystem.ConfigServer?.HydratingLiquids.FirstOrDefault(keyVal => collectible.MyWildCardMatch(keyVal.Key)).Value;
            if (hydrationProps != null)
            {
                collectible.AddDrinkableBehavior();
                collectible.SetHydrationProperties(hydrationProps);
            }

            if (collectible.IsWaterPortion(api.Side))
            {
                collectible.SetAttribute("waterportion", true);
            }
            
            hydrationProps = ConfigSystem.ConfigServer?.HydratingBlocks
                .Where(keyVal => collectible.MyWildCardMatch(keyVal.Key))
                .OrderByDescending(keyVal => keyVal.Key.Length)
                .FirstOrDefault()
                .Value;
            
            if (hydrationProps != null)
            {
                collectible.SetHydrationProperties(hydrationProps);
            }
        }
    }
    
    public static void AddContainerProps(ICoreAPI api)
    {
        BtCore.Logger.Notification("Adding container properties to blocks");
        foreach (var block in api.World.Blocks) {
            if (block is not BlockLiquidContainerBase container) continue;
            if (!block.IsWaterContainer(api.Side)) continue;
            var mostSpecificMatch = ConfigSystem.ConfigServer?.WaterContainers
                .Where(keyVal => block.MyWildCardMatch(keyVal.Key))
                .OrderByDescending(keyVal => keyVal.Key.Length)
                .FirstOrDefault()
                .Value;
            container.SetAttribute("waterTransitionMul", mostSpecificMatch ?? 1);
            container.AddContainerBehavior();
        }
    }
    
}