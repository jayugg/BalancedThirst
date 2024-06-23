using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BalancedThirst.Blocks;

public class BlockWaterskin : BlockWaterStorageContainer
{
  protected override float TransitionRateMul => LastVariant == "pelt" ? 0.6f : 0.4f;
  public override float GetTransitionRateMul(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
  {
    var contentStack = inSlot.Itemstack;
    if (contentStack.Collectible.IsWaterPortion()) return TransitionRateMul;
    return base.GetTransitionRateMul(world, inSlot, transType);
  }

  public override int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres)
    {
      if (liquidStack == null || !liquidStack.Collectible.IsWaterPortion())
        return 0;
      WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(liquidStack);
      int num1 = (int) ((double) containableProps.ItemsPerLitre * (double) desiredLitres);
      float stackSize = (float) liquidStack.StackSize;
      float num2 = this.CapacityLitres * containableProps.ItemsPerLitre;
      ItemStack content1 = this.GetContent(pos);
      if (content1 == null)
      {
        if (!containableProps.Containable)
          return 0;
        int val2 = (int) GameMath.Min((float) num1, num2, stackSize);
        int num3 = Math.Min(num1, val2);
        ItemStack content2 = liquidStack.Clone();
        content2.StackSize = num3;
        this.SetContent(pos, content2);
        return num3;
      }
      if (!content1.Equals(this.api.World, liquidStack, GlobalConstants.IgnoredStackAttributes))
        return 0;
      int num4 = Math.Min((int) Math.Min(stackSize, num2 - (float) content1.StackSize), num1);
      content1.StackSize += num4;
      this.api.World.BlockAccessor.GetBlockEntity(pos).MarkDirty(true);
      (this.api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer)?.Inventory[this.GetContainerSlotId(pos)].MarkDirty();
      return num4;
    }
  
}