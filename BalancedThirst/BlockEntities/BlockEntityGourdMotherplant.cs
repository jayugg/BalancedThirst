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

    public ITreeAttribute CropAttributes => cropAttrs;
    
    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        if (api is ICoreServerAPI)
        {
            listenerId = RegisterGameTickListener(new Action<float>(Update),  3300 + rand.Next(400));
        }
        tickGrowthProbability = Block.Attributes?["tickGrowthProbability"]?.AsFloat(defaultGrowthProbability) ?? defaultGrowthProbability;
    }

    private void Update(float obj)
    {
        if (rand.NextDouble() >= tickGrowthProbability || !IsNotOnFarmland(Api.World, Pos))
            return;
        if (!((ICoreServerAPI)Api).World.IsFullyLoadedChunk(Pos))
            return;
        TryGrowCrop(Api.World.Calendar.TotalHours);
        if (GetCropStage(GetCrop()) == 8)
        {
            Api.World.UnregisterGameTickListener(listenerId);
        }
    }

    public bool TryGrowCrop(double currentTotalHours)
    {
        var crop = GetCrop();
        if (crop == null)
            return false;
        var cropStage = GetCropStage(crop);
        if (cropStage >= crop.CropProps.GrowthStages)
            return false;
        var newGrowthStage = cropStage + 1;
        var block = Api.World.GetBlock(crop.CodeWithParts(newGrowthStage.ToString() ?? ""));
        if (block == null)
            return false;
        if (crop.CropProps.Behaviors != null)
        {
            var handling = EnumHandling.PassThrough;
            var flag = false;
            var behavior = GetGourdBehavior();
            flag = behavior.TryGrowCrop(Api, this, currentTotalHours, newGrowthStage, ref handling);
            if (handling == EnumHandling.PreventSubsequent)
                return flag;
            if (handling == EnumHandling.PreventDefault)
                return flag;
        }
        if (Api.World.BlockAccessor.GetBlockEntity(Pos) == null)
            Api.World.BlockAccessor.SetBlock(block.BlockId, Pos);
        else
            Api.World.BlockAccessor.ExchangeBlock(block.BlockId, Pos);
        return true;
    }
    
    internal Block GetCrop()
    {
        var block = Api.World.BlockAccessor.GetBlock(Pos);
        return block == null || block.CropProps == null ? null : block;
    }
    
    internal GourdCropBehavior GetGourdBehavior()
    {
        GourdCropBehavior gourdCropBehavior = null;
        var cropPropsBehaviors = (Block as BlockCrop)?.CropProps.Behaviors;
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