using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace BalancedThirst.ModBlockBehavior
{
    public class BlockBehaviorRisingWater : BlockBehavior
    {
        private ICoreAPI _api;

        public BlockBehaviorRisingWater(Block block) : base(block)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            _api = api;
            if (api.Side == EnumAppSide.Server)
            {
                api.World.RegisterGameTickListener(OnTick, 1000); // Adjust the tick rate as needed
            }
        }

        private void OnTick(float dt)
        {
            var blockAccessor = _api.World.BlockAccessor;
            TryRise(blockAccessor, this.block.TopMiddlePos.AsVec3i.AsBlockPos);
        }

        private void TryRise(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BlockPos abovePos = pos.UpCopy();
            BlockPos aboveAbovePos = pos.UpCopy(2);

            if (blockAccessor.GetBlock(abovePos).IsReplacableBy(this.block) &&
                IsSurroundedBySolidBlocks(blockAccessor, aboveAbovePos))
            {
                blockAccessor.SetBlock(block.BlockId, aboveAbovePos);
                blockAccessor.SetBlock(0, pos); // Replace the current block with air
            }
        }

        private bool IsSurroundedBySolidBlocks(IBlockAccessor blockAccessor, BlockPos pos)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    BlockPos adjacentPos = new BlockPos(pos.X + dx, pos.Y, pos.Z + dz, 0);
                    if (!blockAccessor.GetBlock(adjacentPos).SideSolid[BlockFacing.UP.Index])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}