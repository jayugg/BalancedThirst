using BalancedThirst.BlockEntities;
using BalancedThirst.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.ModBehavior;

public class GourdCropBehavior : PumpkinCropBehavior
{
    private int vineGrowthStage = 3;
    private float vineGrowthQuantity;
    private AssetLocation vineBlockLocation;
    private NatFloat vineGrowthQuantityGen;
    
    public GourdCropBehavior(Block block) : base(block)
    {
    }
    
    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        vineGrowthStage = properties["vineGrowthStage"].AsInt();
        vineGrowthQuantityGen = properties["vineGrowthQuantity"].AsObject<NatFloat>();
        vineBlockLocation = new AssetLocation($"{BtCore.Modid}:gourdpumpkin-vine-1-normal");
    }
    
    public override void OnPlanted(ICoreAPI api)
    {
        vineGrowthQuantity = vineGrowthQuantityGen.nextFloat(1f, api.World.Rand);
    }
    
    public override bool TryGrowCrop(
        ICoreAPI api,
        IFarmlandBlockEntity farmland,
        double currentTotalHours,
        int newGrowthStage,
        ref EnumHandling handling)
    {
        if (vineGrowthQuantity == 0.0)
        {
            vineGrowthQuantity = farmland.CropAttributes.GetFloat("vineGrowthQuantity", vineGrowthQuantityGen.nextFloat(1f, api.World.Rand));
            farmland.CropAttributes.SetFloat("vineGrowthQuantity", vineGrowthQuantity);
        }
        handling = EnumHandling.PassThrough;
        if (newGrowthStage >= vineGrowthStage)
        {
            if (newGrowthStage == 8)
            {
                var flag = true;
                foreach (var facing in BlockFacing.HORIZONTALS)
                {
                    var block = api.World.BlockAccessor.GetBlock(farmland.Pos.AddCopy(facing).Up());
                    if (block.Code.PathStartsWith("gourdpumpkin-vine"))
                        flag &= block.LastCodePart() == "withered";
                }
                if (!flag)
                    handling = EnumHandling.PreventDefault;
                return false;
            }
            if (api.World.Rand.NextDouble() < vineGrowthQuantity)
                return TrySpawnVine(api, farmland, currentTotalHours);
        }
        return false;
    }
    
    private bool TrySpawnVine(
        ICoreAPI api,
        IFarmlandBlockEntity farmland,
        double currentTotalHours)
    {
        var upPos = farmland.UpPos;
        foreach (var facing in BlockFacing.HORIZONTALS)
        {
            var blockPos = upPos.AddCopy(facing);
            if (CanReplace(api.World.BlockAccessor.GetBlock(blockPos)) && PumpkinCropBehavior.CanSupportPumpkin(api, blockPos.DownCopy()))
            {
                DoSpawnVine(api, blockPos, upPos, facing, currentTotalHours);
                return true;
            }
        }
        return false;
    }
    
    public bool TryGrowCrop(
        ICoreAPI api,
        BlockEntityGourdMotherplant motherplant,
        double currentTotalHours,
        int newGrowthStage,
        ref EnumHandling handling)
    {
        if (vineGrowthQuantity == 0.0)
        {
            vineGrowthQuantity = motherplant.CropAttributes.GetFloat("vineGrowthQuantity", vineGrowthQuantityGen.nextFloat(1f, api.World.Rand));
            motherplant.CropAttributes.SetFloat("vineGrowthQuantity", vineGrowthQuantity);
        }
        handling = EnumHandling.PassThrough;
        if (newGrowthStage >= vineGrowthStage)
        {
            if (newGrowthStage == 8)
            {
                var flag = true;
                foreach (var facing in BlockFacing.HORIZONTALS)
                {
                    var block = api.World.BlockAccessor.GetBlock(motherplant.Pos.AddCopy(facing));
                    if (block.Code.PathStartsWith("gourdpumpkin-vine"))
                        flag &= block.LastCodePart() == "withered";
                }
                if (!flag)
                    handling = EnumHandling.PreventDefault;
                return false;
            }
            if (api.World.Rand.NextDouble() < vineGrowthQuantity)
                return TrySpawnVine(api, motherplant, currentTotalHours);
        }
        return false;
    }
    
    private bool TrySpawnVine(
        ICoreAPI api,
        BlockEntityGourdMotherplant motherplant,
        double currentTotalHours)
    {
        var motherplantPos = motherplant.Pos;
        foreach (var facing in BlockFacing.HORIZONTALS)
        {
            var blockPos = motherplantPos.AddCopy(facing);
            if (CanReplace(api.World.BlockAccessor.GetBlock(blockPos)) && PumpkinCropBehavior.CanSupportPumpkin(api, blockPos.DownCopy()))
            {
                DoSpawnVine(api, blockPos, motherplantPos, facing, currentTotalHours);
                return true;
            }
        }
        return false;
    }
    
    private void DoSpawnVine(
        ICoreAPI api,
        BlockPos vinePos,
        BlockPos motherplantPos,
        BlockFacing facing,
        double currentTotalHours)
    {
        var block = api.World.GetBlock(vineBlockLocation);
        api.World.BlockAccessor.SetBlock(block.BlockId, vinePos); 
        if (!(api.World is IServerWorldAccessor))
            return;
        var blockEntity = api.World.BlockAccessor.GetBlockEntity(vinePos);
        if (!(blockEntity is BlockEntityGourdVine vine))
            return;
        vine.CreatedFromParent(motherplantPos, facing, currentTotalHours);
    }

    public static bool CanReplace(Block block)
    {
        if (block == null)
            return true;
        return block.Replaceable >= 6000 && !block.Code.GetName().Contains("pumpkin");
    }

    public static bool CanSupportPumpkin(ICoreAPI api, BlockPos pos)
    {
        return !api.World.BlockAccessor.GetBlock(pos, 2).IsLiquid() && api.World.BlockAccessor.GetBlock(pos).Replaceable <= 5000;
    }
}