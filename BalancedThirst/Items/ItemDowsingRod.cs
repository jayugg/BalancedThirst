using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace BalancedThirst.Items;

public class ItemDowsingRod : Item
{
    public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handling)
    {
        IPlayer player = (byEntity as EntityPlayer)?.Player;
        if (player == null || slot.Itemstack == null) return;
        BlockPos pos = GetLowestDirtBlockPos(byEntity.Pos.AsBlockPos, byEntity.World);
        if (pos != null)
        {
            Block waterBlock = byEntity.World.GetBlock(new AssetLocation(BtCore.Modid, "purewater-still-7"));
            if (waterBlock != null)
            {
                byEntity.World.BlockAccessor.SetBlock(waterBlock.BlockId, pos);
                slot.MarkDirty();
                handling = EnumHandHandling.PreventDefault;
                slot.Itemstack.ReduceDurability(1);
            }
        }
    }
    
    private BlockPos GetLowestDirtBlockPos(BlockPos startPos, IWorldAccessor world)
    {
        for (int y = Math.Max(0, startPos.Y - 10); y <= startPos.Y; y++)
        {
            BlockPos pos = new BlockPos(startPos.X, y, startPos.Z, 0);
            Block block = world.BlockAccessor.GetBlock(pos);
            if (block.FirstCodePart() == "soil")
            {
                Block blockBelow = world.BlockAccessor.GetBlock(pos.DownCopy());
                return blockBelow.Code.ToString().Contains("water") ? null : pos;
            }
        }
        return null;
    }
}