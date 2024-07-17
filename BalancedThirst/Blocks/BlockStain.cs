using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace BalancedThirst.Blocks;

public class BlockStain : Block
{
    public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
    {
        extra = null;
        return world.BlockAccessor.GetBlock(pos.UpCopy()).BlockMaterial == EnumBlockMaterial.Liquid || world.BlockAccessor.GetDistanceToRainFall(pos, 2) < 2;
    }

    public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
    {
        base.OnServerGameTick(world, pos, extra);
        world.BlockAccessor.SetBlock(0, pos);
    }
    
}