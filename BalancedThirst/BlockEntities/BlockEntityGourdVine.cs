using System;
using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace BalancedThirst.BlockEntities;

public class BlockEntityGourdVine : BlockEntity
{
  
    public static readonly float pumpkinHoursToGrow = 12f;
    public static readonly float vineHoursToGrow = 12f;
    public static readonly float vineHoursToGrowStage2 = 6f;
    public static readonly float bloomProbability = 0.6f;
    public static readonly float debloomProbability = 0.4f;
    public static readonly float vineSpawnProbability = 0.5f;
    public static readonly float preferredGrowthDirProbability = 0.75f;
    public static readonly int maxAllowedPumpkinGrowthTries = 4;
    public long growListenerId;
    public Block stage1VineBlock;
    public Block pumpkinBlock;
    public double totalHoursForNextStage;
    public bool canBloom;
    public int pumpkinGrowthTries;
    public Dictionary<BlockFacing, double> pumpkinTotalHoursForNextStage = new Dictionary<BlockFacing, double>();
    public Dictionary<BlockFacing, int> pumpkinGrowthPotential = new Dictionary<BlockFacing, int>();
    public BlockPos parentPlantPos;
    public BlockFacing preferredGrowthDir;
    public int internalStage;

    public BlockEntityGourdVine()
    {
      foreach (BlockFacing key in BlockFacing.HORIZONTALS)
      {
        this.pumpkinTotalHoursForNextStage.Add(key, 0.0);
      }
    }
    
    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        this.stage1VineBlock = api.World.GetBlock(new AssetLocation($"{BtCore.Modid}:gourdpumpkin-vine-1-normal"));
        this.pumpkinBlock = api.World.GetBlock(new AssetLocation($"{BtCore.Modid}:gourdpumpkin-fruit-1"));
        if (!(api is ICoreServerAPI))
            return;
        this.growListenerId = this.RegisterGameTickListener(this.TryGrow, 2000);
        var prob = this.Block.Attributes["largeFruitChance"].AsFloat(0.1f);
        foreach (BlockFacing key in BlockFacing.HORIZONTALS)
        {
          this.pumpkinGrowthPotential.Add(key, api.World.Rand.NextDouble() < prob ? 4 : 3 );
        }
    }
    
    private void TryGrow(float dt)
    {
        if (this.DieIfParentDead())
            return;
        for (; this.Api.World.Calendar.TotalHours > this.totalHoursForNextStage; this.totalHoursForNextStage +=  vineHoursToGrow)
            this.GrowVine();
        this.TryGrowPumpkins();
    }
    
    private void TryGrowPumpkins()
    {
      foreach (BlockFacing blockFacing in BlockFacing.HORIZONTALS)
      {
        double hoursToGrow = this.pumpkinTotalHoursForNextStage[blockFacing];
        while (hoursToGrow > 0.0 && this.Api.World.Calendar.TotalHours > hoursToGrow)
        {
          BlockPos blockPos = this.Pos.AddCopy(blockFacing);
          Block block = this.Api.World.BlockAccessor.GetBlock(blockPos);
          if (this.IsPumpkin(block))
          {
            int currentPumpkinStage = this.CurrentPumpkinStage(block);
            int growthPotential = this.pumpkinGrowthPotential[blockFacing];
            if (currentPumpkinStage == growthPotential)
            {
              hoursToGrow = 0.0;
            }
            else
            {
              this.SetPumpkinStage(block, blockPos, currentPumpkinStage + 1);
              hoursToGrow += pumpkinHoursToGrow;
            }
          }
          else
            hoursToGrow = 0.0;
          this.pumpkinTotalHoursForNextStage[blockFacing] = hoursToGrow;
        }
      }
    }

    private void GrowVine()
    {
      //BtCore.Logger.Warning("Growing vine");
      ++this.internalStage;
      Block block = this.Api.World.BlockAccessor.GetBlock(this.Pos);
      int num = this.CurrentVineStage(block);
      //BtCore.Logger.Warning($"{num}");
      if (this.internalStage > 6)
        this.SetVineStage(block, num + 1);
      if (this.IsBlooming())
      {
        if (this.pumpkinGrowthTries >= maxAllowedPumpkinGrowthTries || this.Api.World.Rand.NextDouble() <  debloomProbability)
        {
          this.pumpkinGrowthTries = 0;
          this.SetVineStage(block, 3);
        }
        else
        {
          ++this.pumpkinGrowthTries;
          this.TrySpawnPumpkin(this.totalHoursForNextStage -  vineHoursToGrow);
        }
      }
      if (num == 3)
      {
        if (this.canBloom && this.Api.World.Rand.NextDouble() <  bloomProbability)
          this.SetBloomingStage(block);
        this.canBloom = false;
      }
      if (num == 2)
      {
        if (this.Api.World.Rand.NextDouble() <  vineSpawnProbability)
          this.TrySpawnNewVine();
        this.totalHoursForNextStage +=  vineHoursToGrowStage2;
        this.canBloom = true;
        this.SetVineStage(block, num + 1);
      }
      if (num >= 2)
        return;
      this.SetVineStage(block, num + 1);
    }
    
    private void TrySpawnPumpkin(double curTotalHours)
    {
      foreach (BlockFacing blockFacing in BlockFacing.HORIZONTALS)
      {
        BlockPos pos = this.Pos.AddCopy(blockFacing);
        if (this.CanReplace(this.Api.World.BlockAccessor.GetBlock(pos)) && GourdCropBehavior.CanSupportPumpkin(this.Api, pos.DownCopy()))
        {
          this.Api.World.BlockAccessor.SetBlock(this.pumpkinBlock.BlockId, pos);
          this.pumpkinTotalHoursForNextStage[blockFacing] = curTotalHours +  pumpkinHoursToGrow;
          break;
        }
      }
    }

    private bool IsPumpkin(Block block)
    {
      return block != null && block.Code.GetName().StartsWithOrdinal("gourdpumpkin-fruit");
    }

    private bool DieIfParentDead()
    {
      if (this.parentPlantPos == null)
      {
        //BtCore.Logger.Warning("Vine died with no parent (null)");
        this.Die();
        return true;
      }
      if (this.IsValidParentBlock(this.Api.World.BlockAccessor.GetBlock(this.parentPlantPos)) || this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.parentPlantPos) == null)
        return false;
      //BtCore.Logger.Warning("Vine died with no parent (parentPos)");
      this.Die();
      return true;
    }

    private void Die()
    {
      //BtCore.Logger.Debug("Vine died");
      this.Api.Event.UnregisterGameTickListener(this.growListenerId);
      this.growListenerId = 0L;
      this.Api.World.BlockAccessor.SetBlock(0, this.Pos);
    }
    
    private bool IsValidParentBlock(Block parentBlock)
    {
      if (parentBlock != null)
      {
        string name = parentBlock.Code.GetName();
        if (name.StartsWithOrdinal("crop-gourdpumpkin") || name.StartsWithOrdinal("gourdpumpkin-vine"))
          return true;
      }
      return false;
    }

    public bool IsBlooming()
    {
      Block block = this.Api.World.BlockAccessor.GetBlock(this.Pos);
      block.LastCodePart();
      return block.LastCodePart() == "blooming";
    }

    private bool CanReplace(Block block)
    {
      if (block == null)
        return true;
      return block.Replaceable >= 6000 && !block.Code.GetName().Contains("pumpkin");
    }

    private void SetVineStage(Block block, int toStage)
    {
      //BtCore.Logger.Warning("Setting vine stage to {0}", (object) toStage);
      try
      {
        this.ReplaceSelf(block.CodeWithParts(toStage.ToString(), toStage == 4 ? "withered" : "normal"));
      }
      catch (Exception ex)
      {
        this.Api.World.BlockAccessor.SetBlock(0, this.Pos);
      }
    }

    private void SetPumpkinStage(Block pumpkinBlock, BlockPos pumpkinPos, int toStage)
    {
      Block block = this.Api.World.GetBlock(pumpkinBlock.CodeWithParts(toStage.ToString()));
      if (block == null)
        return;
      this.Api.World.BlockAccessor.ExchangeBlock(block.BlockId, pumpkinPos);
    }

    private void SetBloomingStage(Block block) => this.ReplaceSelf(block.CodeWithParts("blooming"));

    private void ReplaceSelf(AssetLocation blockCode)
    {
      Block block = this.Api.World.GetBlock(blockCode);
      if (block == null)
        return;
      this.Api.World.BlockAccessor.ExchangeBlock(block.BlockId, this.Pos);
    }

    private void TrySpawnNewVine()
    {
      BlockFacing vineSpawnDirection = this.GetVineSpawnDirection();
      BlockPos blockPos = this.Pos.AddCopy(vineSpawnDirection);
      if (!this.IsReplaceable(this.Api.World.BlockAccessor.GetBlock(blockPos)))
      {
        BlockPos abovePos = blockPos.UpCopy();
        if (!this.IsReplaceable(this.Api.World.BlockAccessor.GetBlock(abovePos)) || !this.CanGrowOn(this.Api, blockPos))
        {
          return;
        }
        blockPos = abovePos;
      }
      --blockPos.Y;
      if (!this.CanGrowOn(this.Api, blockPos))
      {
        if (this.CanGrowOn(this.Api, blockPos.DownCopy()))
          blockPos = blockPos.DownCopy();
        else
          return;
      }
      ++blockPos.Y;
      this.Api.World.BlockAccessor.SetBlock(this.stage1VineBlock.BlockId, blockPos);
      if (!(this.Api.World.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityGourdVine blockEntity))
        return;
      blockEntity.CreatedFromParent(this.Pos, vineSpawnDirection, this.totalHoursForNextStage);
    }
    
    public void CreatedFromParent(
      BlockPos parentPlantPos,
      BlockFacing preferredGrowthDir,
      double currentTotalHours)
    {
      //BtCore.Logger.Warning("Vine created from parent at {0} with preferred growth direction {1}", (object) parentPlantPos, (object) preferredGrowthDir);
      this.totalHoursForNextStage = currentTotalHours + vineHoursToGrow;
      this.parentPlantPos = parentPlantPos;
      this.preferredGrowthDir = preferredGrowthDir;
    }

    private bool CanGrowOn(ICoreAPI api, BlockPos pos)
    {
      return api.World.BlockAccessor.GetMostSolidBlock(pos.X, pos.Y, pos.Z).CanAttachBlockAt(api.World.BlockAccessor, this.stage1VineBlock, pos, BlockFacing.UP);
    }

    private bool IsReplaceable(Block block) => block == null || block.Replaceable >= 6000;

    private BlockFacing GetVineSpawnDirection()
    {
      return this.Api.World.Rand.NextDouble() <  preferredGrowthDirProbability ? this.preferredGrowthDir : this.DirectionAdjacentToPreferred();
    }

    private BlockFacing DirectionAdjacentToPreferred()
    {
      return BlockFacing.NORTH == this.preferredGrowthDir || BlockFacing.SOUTH == this.preferredGrowthDir ? (this.Api.World.Rand.NextDouble() >= 0.5 ? BlockFacing.WEST : BlockFacing.EAST) : (this.Api.World.Rand.NextDouble() >= 0.5 ? BlockFacing.SOUTH : BlockFacing.NORTH);
    }

    private int CurrentVineStage(Block block)
    {
      int.TryParse(block.LastCodePart(1), out var result);
      return result;
    }

    private int CurrentPumpkinStage(Block block)
    {
      int.TryParse(block.LastCodePart(), out var result);
      return result;
    }
}