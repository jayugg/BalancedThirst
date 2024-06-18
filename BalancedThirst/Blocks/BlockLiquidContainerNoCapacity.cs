using Vintagestory.API.Common;

namespace BalancedThirst.Blocks;

public class BlockLiquidContainerNoCapacity : BlockLiquidContainerLeaking
{
    public override float GetLeakageRate(ItemStack itemstack)
    {
        return 1;
    }
    protected float LeakagePerTick => 0.01f;
}