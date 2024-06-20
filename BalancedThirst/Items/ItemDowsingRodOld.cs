using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.Items;

public class ItemDowsingRodOld : Item
{
    private float _baseWaterChance = 0.005f;
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
        if (pos != null &&
            byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak) &&
            byEntity.World.Rand.NextSingle() < CalcWaterChance(byEntity.World, pos))
        {
            Block waterBlock = byEntity.World.GetBlock(new AssetLocation(BtCore.Modid, "purewater-still-7"));
            if (waterBlock == null || !PlaceReservoir(byEntity, pos, waterBlock)) return;
            slot.MarkDirty();
            handling = EnumHandHandling.PreventDefault;
            if (byEntity is EntityPlayer playerEntity)
            {
                // TODO: Add animation
                //playerEntity.AnimManager.StartAnimation("coldidle");
            }

            if (player is IServerPlayer serverPlayer)
            {
                serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup,
                    Lang.GetL(serverPlayer.LanguageCode, "Found water below!"), EnumChatType.Notification);
            }
        }
        this.DamageItem(byEntity.World, byEntity, slot);
    }

    private static bool PlaceReservoir(EntityAgent byEntity, BlockPos pos, Block waterBlock)
    {
        Random rand = new Random();
        int radius = rand.Next(2, 5);
        bool placedAny = false;
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                if (x * x + z * z <= radius * radius)
                {
                    BlockPos newPos = new BlockPos(pos.X + x, pos.Y, pos.Z + z, 0);
                    Block existingBlock = byEntity.World.BlockAccessor.GetBlock(newPos);
                    if (existingBlock.FirstCodePart() == "soil" && !IsExposed(byEntity.World.BlockAccessor, newPos))
                    {
                        byEntity.World.BlockAccessor.SetBlock(waterBlock.BlockId, newPos);
                        placedAny = true;
                    }
                }
            }
        }
        return placedAny;
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

    private float CalcWaterChance(IWorldAccessor world, BlockPos pos)
    {
        ClimateCondition climate = world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues,
            world.Calendar.TotalDays);
        float geo = climate.GeologicActivity;
        float rain = climate.WorldgenRainfall;
        float forest = climate.ForestDensity;
        return _baseWaterChance * (1 + Math.Min(rain*3 - geo*1 - forest*1, 0));
    }

    private static bool IsExposed(IBlockAccessor blockAccessor, BlockPos pos)
    {
        foreach (BlockFacing facing in BlockFacing.ALLFACES)
        {
            BlockPos adjacentPos = pos.AddCopy(facing);
            if (!blockAccessor.GetBlock(adjacentPos).SideSolid[facing.Opposite.Index] &&
                blockAccessor.GetBlock(adjacentPos).FirstCodePart() != "purewater")
            {
                return true;
            }
        }
        return false;
    }
}