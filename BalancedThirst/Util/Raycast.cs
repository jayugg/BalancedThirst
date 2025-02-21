using System;
using BalancedThirst.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace BalancedThirst.Util;

public static class Raycast
{
    public static BlockSelection RayCastForFluidBlocks(IPlayer player)
    {
        var fromPos = player.Entity.SidedPos.Copy().XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
        var toPos = fromPos.AheadCopy(5, player.Entity.SidedPos.Copy().Pitch, player.Entity.SidedPos.Copy().Yaw);
        var step = toPos.Sub(fromPos).Normalize().Mul(0.5);
        var currentPos = fromPos.Clone();
        
        while (currentPos.SquareDistanceTo(fromPos) <= 25)
        {
            var blockPos = new BlockPos((int)currentPos.X, (int)currentPos.Y, (int)currentPos.Z);
            var block = player.Entity.World.BlockAccessor.GetBlock(blockPos, BlockLayersAccess.FluidOrSolid);
            
            if (block is { BlockMaterial: EnumBlockMaterial.Liquid })
            {
                return new BlockSelection { Position = blockPos, HitPosition = currentPos.Clone(), Block = block};
            }
            if (block != null && block.BlockMaterial != EnumBlockMaterial.Air)
            {
                return null;
            }
            currentPos.Add(step);
        }
        return null;
    }
    
    private static BlockFacing GetFacingSide(Vec3d diff)
    {
        if (Math.Abs(diff.X) > Math.Abs(diff.Y) && Math.Abs(diff.X) > Math.Abs(diff.Z))
        {
            return diff.X > 0 ? BlockFacing.WEST : BlockFacing.EAST;
        }
        else if (Math.Abs(diff.Y) > Math.Abs(diff.X) && Math.Abs(diff.Y) > Math.Abs(diff.Z))
        {
            return diff.Y > 0 ? BlockFacing.DOWN : BlockFacing.UP;
        }
        else
        {
            return diff.Z > 0 ? BlockFacing.NORTH : BlockFacing.SOUTH;
        }
    }
}