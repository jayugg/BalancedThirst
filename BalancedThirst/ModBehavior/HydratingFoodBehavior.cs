using BalancedThirst.Thirst;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace BalancedThirst.ModBehavior;

public class HydratingFoodBehavior : DrinkableBehavior
{

    internal override HydrationProperties GetHydrationProperties(IWorldAccessor world, ItemStack itemstack, Entity byEntity)
    {
        return base.ExtractNutritionHydrationProperties(world, itemstack, byEntity);
    }
    
    public HydratingFoodBehavior(CollectibleObject collObj) : base(collObj)
    {
    }
}