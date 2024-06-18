using Vintagestory.API.Common;

namespace BalancedThirst.Blocks;

public class BlockLiquidContainerNoCapacity : BlockLiquidContainerLeaking
{
    public override float GetLeakageRate(ItemStack itemstack)
    {
        return 1;
    }
    protected override float LeakagePerTick => 1;
}