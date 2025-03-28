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
      foreach (var key in BlockFacing.HORIZONTALS)
      {
        pumpkinTotalHoursForNextStage.Add(key, 0.0);
      }
    }
    
    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        stage1VineBlock = api.World.GetBlock(new AssetLocation($"{BtCore.Modid}:gourdpumpkin-vine-1-normal"));
        pumpkinBlock = api.World.GetBlock(new AssetLocation($"{BtCore.Modid}:gourdpumpkin-fruit-1"));
        if (!(api is ICoreServerAPI))
            return;
        growListenerId = RegisterGameTickListener(TryGrow, 2000);
        var prob = Block.Attributes["largeFruitChance"].AsFloat(0.1f);
        foreach (var key in BlockFacing.HORIZONTALS)
        {
          pumpkinGrowthPotential.Add(key, api.World.Rand.NextDouble() < prob ? 4 : 3 );
        }
    }
    
    private void TryGrow(float dt)
    {
        if (DieIfParentDead())
            return;
        for (; Api.World.Calendar.TotalHours > totalHoursForNextStage; totalHoursForNextStage +=  vineHoursToGrow)
            GrowVine();
        TryGrowPumpkins();
    }
    
    private void TryGrowPumpkins()
    {
      foreach (var blockFacing in BlockFacing.HORIZONTALS)
      {
        var hoursToGrow = pumpkinTotalHoursForNextStage[blockFacing];
        while (hoursToGrow > 0.0 && Api.World.Calendar.TotalHours > hoursToGrow)
        {
          var blockPos = Pos.AddCopy(blockFacing);
          var block = Api.World.BlockAccessor.GetBlock(blockPos);
          if (IsPumpkin(block))
          {
            var currentPumpkinStage = CurrentPumpkinStage(block);
            var growthPotential = pumpkinGrowthPotential[blockFacing];
            if (currentPumpkinStage == growthPotential)
            {
              hoursToGrow = 0.0;
            }
            else
            {
              SetPumpkinStage(block, blockPos, currentPumpkinStage + 1);
              hoursToGrow += pumpkinHoursToGrow;
            }
          }
          else
            hoursToGrow = 0.0;
          pumpkinTotalHoursForNextStage[blockFacing] = hoursToGrow;
        }
      }
    }

    private void GrowVine()
    {
      //BtCore.Logger.Warning("Growing vine");
      ++internalStage;
      var block = Api.World.BlockAccessor.GetBlock(Pos);
      var num = CurrentVineStage(block);
      //BtCore.Logger.Warning($"{num}");
      if (internalStage > 6)
        SetVineStage(block, num + 1);
      if (IsBlooming())
      {
        if (pumpkinGrowthTries >= maxAllowedPumpkinGrowthTries || Api.World.Rand.NextDouble() <  debloomProbability)
        {
          pumpkinGrowthTries = 0;
          SetVineStage(block, 3);
        }
        else
        {
          ++pumpkinGrowthTries;
          TrySpawnPumpkin(totalHoursForNextStage -  vineHoursToGrow);
        }
      }
      if (num == 3)
      {
        if (canBloom && Api.World.Rand.NextDouble() <  bloomProbability)
          SetBloomingStage(block);
        canBloom = false;
      }
      if (num == 2)
      {
        if (Api.World.Rand.NextDouble() <  vineSpawnProbability)
          TrySpawnNewVine();
        totalHoursForNextStage +=  vineHoursToGrowStage2;
        canBloom = true;
        SetVineStage(block, num + 1);
      }
      if (num >= 2)
        return;
      SetVineStage(block, num + 1);
    }
    
    private void TrySpawnPumpkin(double curTotalHours)
    {
      foreach (var blockFacing in BlockFacing.HORIZONTALS)
      {
        var pos = Pos.AddCopy(blockFacing);
        if (CanReplace(Api.World.BlockAccessor.GetBlock(pos)) && GourdCropBehavior.CanSupportPumpkin(Api, pos.DownCopy()))
        {
          Api.World.BlockAccessor.SetBlock(pumpkinBlock.BlockId, pos);
          pumpkinTotalHoursForNextStage[blockFacing] = curTotalHours +  pumpkinHoursToGrow;
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
      if (parentPlantPos == null)
      {
        //BtCore.Logger.Warning("Vine died with no parent (null)");
        Die();
        return true;
      }
      if (IsValidParentBlock(Api.World.BlockAccessor.GetBlock(parentPlantPos)) || Api.World.BlockAccessor.GetChunkAtBlockPos(parentPlantPos) == null)
        return false;
      //BtCore.Logger.Warning("Vine died with no parent (parentPos)");
      Die();
      return true;
    }

    private void Die()
    {
      //BtCore.Logger.Debug("Vine died");
      Api.Event.UnregisterGameTickListener(growListenerId);
      growListenerId = 0L;
      Api.World.BlockAccessor.SetBlock(0, Pos);
    }
    
    private bool IsValidParentBlock(Block parentBlock)
    {
      if (parentBlock != null)
      {
        var name = parentBlock.Code.GetName();
        if (name.StartsWithOrdinal("crop-gourdpumpkin") || name.StartsWithOrdinal("gourdpumpkin-vine"))
          return true;
      }
      return false;
    }

    public bool IsBlooming()
    {
      var block = Api.World.BlockAccessor.GetBlock(Pos);
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
        ReplaceSelf(block.CodeWithParts(toStage.ToString(), toStage == 4 ? "withered" : "normal"));
      }
      catch (Exception ex)
      {
        Api.World.BlockAccessor.SetBlock(0, Pos);
      }
    }

    private void SetPumpkinStage(Block pumpkinBlock, BlockPos pumpkinPos, int toStage)
    {
      var block = Api.World.GetBlock(pumpkinBlock.CodeWithParts(toStage.ToString()));
      if (block == null)
        return;
      Api.World.BlockAccessor.ExchangeBlock(block.BlockId, pumpkinPos);
    }

    private void SetBloomingStage(Block block) => ReplaceSelf(block.CodeWithParts("blooming"));

    private void ReplaceSelf(AssetLocation blockCode)
    {
      var block = Api.World.GetBlock(blockCode);
      if (block == null)
        return;
      Api.World.BlockAccessor.ExchangeBlock(block.BlockId, Pos);
    }

    private void TrySpawnNewVine()
    {
      var vineSpawnDirection = GetVineSpawnDirection();
      var blockPos = Pos.AddCopy(vineSpawnDirection);
      if (!IsReplaceable(Api.World.BlockAccessor.GetBlock(blockPos)))
      {
        var abovePos = blockPos.UpCopy();
        if (!IsReplaceable(Api.World.BlockAccessor.GetBlock(abovePos)) || !CanGrowOn(Api, blockPos))
        {
          return;
        }
        blockPos = abovePos;
      }
      --blockPos.Y;
      if (!CanGrowOn(Api, blockPos))
      {
        if (CanGrowOn(Api, blockPos.DownCopy()))
          blockPos = blockPos.DownCopy();
        else
          return;
      }
      ++blockPos.Y;
      Api.World.BlockAccessor.SetBlock(stage1VineBlock.BlockId, blockPos);
      if (!(Api.World.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityGourdVine blockEntity))
        return;
      blockEntity.CreatedFromParent(Pos, vineSpawnDirection, totalHoursForNextStage);
    }
    
    public void CreatedFromParent(
      BlockPos parentPlantPos,
      BlockFacing preferredGrowthDir,
      double currentTotalHours)
    {
      //BtCore.Logger.Warning("Vine created from parent at {0} with preferred growth direction {1}", (object) parentPlantPos, (object) preferredGrowthDir);
      totalHoursForNextStage = currentTotalHours + vineHoursToGrow;
      this.parentPlantPos = parentPlantPos;
      this.preferredGrowthDir = preferredGrowthDir;
    }

    private bool CanGrowOn(ICoreAPI api, BlockPos pos)
    {
      return api.World.BlockAccessor.GetMostSolidBlock(pos.X, pos.Y, pos.Z).CanAttachBlockAt(api.World.BlockAccessor, stage1VineBlock, pos, BlockFacing.UP);
    }

    private bool IsReplaceable(Block block) => block == null || block.Replaceable >= 6000;

    private BlockFacing GetVineSpawnDirection()
    {
      return Api.World.Rand.NextDouble() <  preferredGrowthDirProbability ? preferredGrowthDir : DirectionAdjacentToPreferred();
    }

    private BlockFacing DirectionAdjacentToPreferred()
    {
      return BlockFacing.NORTH == preferredGrowthDir || BlockFacing.SOUTH == preferredGrowthDir ? (Api.World.Rand.NextDouble() >= 0.5 ? BlockFacing.WEST : BlockFacing.EAST) : (Api.World.Rand.NextDouble() >= 0.5 ? BlockFacing.SOUTH : BlockFacing.NORTH);
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