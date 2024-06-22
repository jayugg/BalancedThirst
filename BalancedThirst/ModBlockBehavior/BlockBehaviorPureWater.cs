using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace BalancedThirst.ModBlockBehavior;

public class BlockBehaviorPureWater : BlockBehavior
{
    public BlockBehaviorPureWater(Block block) : base(block)
    {
    }
    
    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
    {
        base.OnBlockPlaced(world, blockPos, ref handling);
        if (world is IServerWorldAccessor serverWorld)
        {
            serverWorld.RegisterCallback(Contaminate, blockPos, 100);
        }
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
    {
        base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
        BtCore.Logger.Warning("Gushing water neighbour change");
        if (world is IServerWorldAccessor serverWorld)
        {
            serverWorld.RegisterCallback(Contaminate, pos, 100);
        }
    }

    public void Contaminate(IWorldAccessor world, BlockPos pos, float dt)
    {
        if (IsImpureWaterNearby(world.BlockAccessor, pos))
        {
            world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("water" + "-" + block.Code.SecondCodePart() + "-" + block.Code.CodePartsAfterSecond())).BlockId, pos, BlockLayersAccess.Fluid);
        }
    }
    
    private bool IsImpureWaterNearby(IBlockAccessor blockAccessor, BlockPos pos)
    {
        foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
        {
            BlockPos adjacentPos = pos.AddCopy(facing);
            if (blockAccessor.GetBlock(adjacentPos).IsLiquidSourceBlock() &&
                !blockAccessor.GetBlock(adjacentPos).IsSameLiquid(this.block))
            {
                return true;
            }
        }
        return false;
    }
}