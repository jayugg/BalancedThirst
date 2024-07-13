using System;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.BlockEntities;

public class BlockEntityGourdMotherplant : BlockEntity
{
    private ITreeAttribute cropAttrs = new TreeAttribute();
    protected static Random rand = new();
    private long listenerId;
    private float tickGrowthProbability;
    private readonly float defaultGrowthProbability = 0.1f;

    public ITreeAttribute CropAttributes => this.cropAttrs;
    
    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        if (api is ICoreServerAPI)
        {
            this.listenerId = this.RegisterGameTickListener(new Action<float>(this.Update),  3300 + rand.Next(400));
        }
        this.tickGrowthProbability = this.Block.Attributes?["tickGrowthProbability"]?.AsFloat(defaultGrowthProbability) ?? defaultGrowthProbability;
    }

    private void Update(float obj)
    {
        if (rand.NextDouble() >= (double) tickGrowthProbability || !IsNotOnFarmland(this.Api.World, this.Pos))
            return;
        if (!((ICoreServerAPI)this.Api).World.IsFullyLoadedChunk(this.Pos))
            return;
        this.TryGrowCrop(Api.World.Calendar.TotalHours);
        if (this.GetCropStage(this.GetCrop()) == 8)
        {
            this.Api.World.UnregisterGameTickListener(this.listenerId);
        }
    }

    public bool TryGrowCrop(double currentTotalHours)
    {
        Block crop = this.GetCrop();
        if (crop == null)
            return false;
        int cropStage = this.GetCropStage(crop);
        if (cropStage >= crop.CropProps.GrowthStages)
            return false;
        int newGrowthStage = cropStage + 1;
        Block block = this.Api.World.GetBlock(crop.CodeWithParts(newGrowthStage.ToString() ?? ""));
        if (block == null)
            return false;
        if (crop.CropProps.Behaviors != null)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            bool flag = false;
            var behavior = GetGourdBehavior();
            flag = behavior.TryGrowCrop(this.Api, this, currentTotalHours, newGrowthStage, ref handling);
            if (handling == EnumHandling.PreventSubsequent)
                return flag;
            if (handling == EnumHandling.PreventDefault)
                return flag;
        }
        if (this.Api.World.BlockAccessor.GetBlockEntity(this.Pos) == null)
            this.Api.World.BlockAccessor.SetBlock(block.BlockId, this.Pos);
        else
            this.Api.World.BlockAccessor.ExchangeBlock(block.BlockId, this.Pos);
        return true;
    }
    
    internal Block GetCrop()
    {
        Block block = this.Api.World.BlockAccessor.GetBlock(this.Pos);
        return block == null || block.CropProps == null ? null : block;
    }
    
    internal GourdCropBehavior GetGourdBehavior()
    {
        GourdCropBehavior gourdCropBehavior = null;
        var cropPropsBehaviors = (this.Block as BlockCrop)?.CropProps.Behaviors;
        if (cropPropsBehaviors != null)
            gourdCropBehavior =
                Array.Find(cropPropsBehaviors, b => b is GourdCropBehavior) as
                    GourdCropBehavior;
        return gourdCropBehavior;
    }

    internal int GetCropStage(Block block)
    {
        int result;
        int.TryParse(block.LastCodePart(), out result);
        return result;
    }
    
    private bool IsNotOnFarmland(IWorldAccessor world, BlockPos pos)
    {
        return !world.BlockAccessor.GetBlock(pos.DownCopy()).FirstCodePart().Equals("farmland");
    }
}