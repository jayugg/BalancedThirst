using System;
using BalancedThirst.BlockEntities;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.Blocks;

public class BlockGourdMotherplant : BlockCrop
{
    
    private GourdCropBehavior gourdBehavior;
    private AssetLocation vineBlockLocation;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        this.gourdBehavior = Array.Find(this.CropProps.Behaviors, b => b is GourdCropBehavior) as GourdCropBehavior;
        this.vineBlockLocation = new AssetLocation($"{BtCore.Modid}:gourdpumpkin-vine-1-normal");
    }
    
    public override bool ShouldReceiveServerGameTicks(
        IWorldAccessor world,
        BlockPos pos,
        Random offThreadRandom,
        out object extra)
    {
        return base.ShouldReceiveServerGameTicks(world, pos, offThreadRandom, out extra);
        /*
        BtCore.Logger.Warning("ShouldReceiveServerGameTicks");
        extra = null;
        if (offThreadRandom.NextDouble() >= this.tickGrowthProbability || !this.IsNotOnFarmland(world, pos))
            return false;
         */
        extra = this.GetNextGrowthStageBlock(world, pos);
        return true;
    }

    public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
    {
        /*
        if (extra is not Block block)
            return;
        world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
        TrySpawnVine(world, pos);
        */
    }
    
    private bool TrySpawnVine(
        IWorldAccessor world,
        BlockPos motherplantPos)
    {
        foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
        {
            BlockPos blockPos = motherplantPos.AddCopy(facing);
            if (GourdCropBehavior.CanReplace(world.BlockAccessor.GetBlock(blockPos)) && GourdCropBehavior.CanSupportPumpkin(api, blockPos.DownCopy()))
            {
                this.DoSpawnVine(world, blockPos, motherplantPos, facing);
                return true;
            }
        }
        return false;
    }
    
    private void DoSpawnVine(
        IWorldAccessor world,
        BlockPos vinePos,
        BlockPos motherplantPos,
        BlockFacing facing)
    {
        Block block = api.World.GetBlock(this.vineBlockLocation);
        api.World.BlockAccessor.SetBlock(block.BlockId, vinePos); 
        if (!(api.World is IServerWorldAccessor))
            return;
        BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(vinePos);
        if (!(blockEntity is BlockEntityGourdVine vine))
            return;
        vine.CreatedFromParent(motherplantPos, facing, world.Rand.Next(24, 72));
    }
    
    public override bool TryPlaceBlockForWorldGen(
        IBlockAccessor blockAccessor,
        BlockPos pos,
        BlockFacing onBlockFace,
        IRandom worldGenRand,
        BlockPatchAttributes attributes = null)
    {
        if (blockAccessor.GetBlock(pos.X, pos.Y - 1, pos.Z).Fertility == 0)
            return false;
        blockAccessor.SetBlock(this.BlockId, pos);
        blockAccessor.SpawnBlockEntity(this.EntityClass, pos);
        //DoTill(blockAccessor, pos);
        //gourdBehavior?.OnPlanted(this.api);
        return true;
    }
    
    public void DoTill(IBlockAccessor blockAccessor, BlockPos pos)
    {
        Block block1 = blockAccessor.GetBlock(pos);
        if (!block1.Code.PathStartsWith("soil"))
            return;
        string str = block1.LastCodePart(1);
        Block block2 = blockAccessor.GetBlock(new AssetLocation("farmland-dry-" + str));
        if (block2 == null)
            return;
        blockAccessor.SetBlock(block2.BlockId, pos);
        BlockEntity blockEntity = blockAccessor.GetBlockEntity(pos);
        if (blockEntity is BlockEntityFarmland)
            ((BlockEntityFarmland) blockEntity).OnCreatedFromSoil(block1);
        blockAccessor.MarkBlockDirty(pos);
    }
    
    private Block GetNextGrowthStageBlock(IWorldAccessor world, BlockPos pos)
    {
        int num = this.CurrentStage() + 1;
        if (world.GetBlock(this.CodeWithParts(num.ToString())) == null)
            num = 1;
        return world.GetBlock(this.CodeWithParts(num.ToString()));
    }
    
    private bool IsNotOnFarmland(IWorldAccessor world, BlockPos pos)
    {
        return !world.BlockAccessor.GetBlock(pos.DownCopy()).FirstCodePart().Equals("farmland");
    }
}