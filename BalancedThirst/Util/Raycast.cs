using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace BalancedThirst.Util;

public static class Raycast
{
    public static BlockSelection RayCastForFluidBlocks(IPlayer player, float range = 4)
    {
        var fromPos = player.Entity.ServerPos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
        var toPos = fromPos.AheadCopy(range, player.Entity.ServerPos.Pitch, player.Entity.ServerPos.Yaw);
        var step = toPos.Sub(fromPos).Normalize().Mul(0.1);
        var currentPos = fromPos.Clone();
        int dimensionId = (int)(player.Entity.ServerPos.Y / BlockPos.DimensionBoundary);

        while (currentPos.SquareDistanceTo(fromPos) <= range * range)
        {
            var blockPos = new BlockPos((int)currentPos.X, (int)currentPos.Y, (int)currentPos.Z, dimensionId);
            var block = player.Entity.World.BlockAccessor.GetBlock(blockPos);
            var diff = currentPos.SubCopy(fromPos);

            if (block.BlockMaterial == EnumBlockMaterial.Liquid)
            {
                return new BlockSelection { Position = blockPos, HitPosition = currentPos.Clone() };
            }
            else if (block.SideIsSolid(blockPos, GetFacingSide(diff).Index))
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