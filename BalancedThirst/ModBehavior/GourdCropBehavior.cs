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
        this.vineGrowthStage = properties["vineGrowthStage"].AsInt();
        this.vineGrowthQuantityGen = properties["vineGrowthQuantity"].AsObject<NatFloat>();
        this.vineBlockLocation = new AssetLocation($"{BtCore.Modid}:gourdpumpkin-vine-1-normal");
    }
    
    public override void OnPlanted(ICoreAPI api)
    {
        this.vineGrowthQuantity = this.vineGrowthQuantityGen.nextFloat(1f, api.World.Rand);
    }
    
    public override bool TryGrowCrop(
        ICoreAPI api,
        IFarmlandBlockEntity farmland,
        double currentTotalHours,
        int newGrowthStage,
        ref EnumHandling handling)
    {
        if ((double) this.vineGrowthQuantity == 0.0)
        {
            this.vineGrowthQuantity = farmland.CropAttributes.GetFloat("vineGrowthQuantity", this.vineGrowthQuantityGen.nextFloat(1f, api.World.Rand));
            farmland.CropAttributes.SetFloat("vineGrowthQuantity", this.vineGrowthQuantity);
        }
        handling = EnumHandling.PassThrough;
        if (newGrowthStage >= this.vineGrowthStage)
        {
            if (newGrowthStage == 8)
            {
                bool flag = true;
                foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
                {
                    Block block = api.World.BlockAccessor.GetBlock(farmland.Pos.AddCopy(facing).Up());
                    if (block.Code.PathStartsWith("gourdpumpkin-vine"))
                        flag &= block.LastCodePart() == "withered";
                }
                if (!flag)
                    handling = EnumHandling.PreventDefault;
                return false;
            }
            if (api.World.Rand.NextDouble() < (double) this.vineGrowthQuantity)
                return this.TrySpawnVine(api, farmland, currentTotalHours);
        }
        return false;
    }
    
    private bool TrySpawnVine(
        ICoreAPI api,
        IFarmlandBlockEntity farmland,
        double currentTotalHours)
    {
        BlockPos upPos = farmland.UpPos;
        foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
        {
            BlockPos blockPos = upPos.AddCopy(facing);
            if (CanReplace(api.World.BlockAccessor.GetBlock(blockPos)) && PumpkinCropBehavior.CanSupportPumpkin(api, blockPos.DownCopy()))
            {
                this.DoSpawnVine(api, blockPos, upPos, facing, currentTotalHours);
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
        if ((double) this.vineGrowthQuantity == 0.0)
        {
            this.vineGrowthQuantity = motherplant.CropAttributes.GetFloat("vineGrowthQuantity", this.vineGrowthQuantityGen.nextFloat(1f, api.World.Rand));
            motherplant.CropAttributes.SetFloat("vineGrowthQuantity", this.vineGrowthQuantity);
        }
        handling = EnumHandling.PassThrough;
        if (newGrowthStage >= this.vineGrowthStage)
        {
            if (newGrowthStage == 8)
            {
                bool flag = true;
                foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
                {
                    Block block = api.World.BlockAccessor.GetBlock(motherplant.Pos.AddCopy(facing));
                    if (block.Code.PathStartsWith("gourdpumpkin-vine"))
                        flag &= block.LastCodePart() == "withered";
                }
                if (!flag)
                    handling = EnumHandling.PreventDefault;
                return false;
            }
            if (api.World.Rand.NextDouble() < (double) this.vineGrowthQuantity)
                return this.TrySpawnVine(api, motherplant, currentTotalHours);
        }
        return false;
    }
    
    private bool TrySpawnVine(
        ICoreAPI api,
        BlockEntityGourdMotherplant motherplant,
        double currentTotalHours)
    {
        BlockPos motherplantPos = motherplant.Pos;
        foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
        {
            BlockPos blockPos = motherplantPos.AddCopy(facing);
            if (CanReplace(api.World.BlockAccessor.GetBlock(blockPos)) && PumpkinCropBehavior.CanSupportPumpkin(api, blockPos.DownCopy()))
            {
                this.DoSpawnVine(api, blockPos, motherplantPos, facing, currentTotalHours);
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
        Block block = api.World.GetBlock(this.vineBlockLocation);
        api.World.BlockAccessor.SetBlock(block.BlockId, vinePos); 
        if (!(api.World is IServerWorldAccessor))
            return;
        BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(vinePos);
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