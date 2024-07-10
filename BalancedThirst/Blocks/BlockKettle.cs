using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using System.Linq;
using BalancedThirst.Systems;
using BalancedThirst.Util;

namespace BalancedThirst.Blocks;
public class BlockKettle : BlockLiquidContainerSealable, IInFirepitRendererSupplier
{
    public override bool CanDrinkFrom => true;
    public override bool IsTopOpened => true;
    public override bool AllowHeldLiquidTransfer => true;
    public AssetLocation liquidFillSoundLocation => new AssetLocation("game:sounds/effect/water-fill");

    public IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
    {
        return new KettleInFirepitRenderer(api as ICoreClientAPI, stack, firepit.Pos, forOutputSlot);
    }

    public EnumFirepitModel GetDesiredFirepitModel(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
    {
        return EnumFirepitModel.Wide;
    }
    
    public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
    {
        if (outputStack != null || inputStack == null) return false;
        foreach (var slot in cookingSlotsProvider.Slots)
        {
            if (slot.Empty) continue;
            if (!slot.Itemstack.Collectible.IsWaterPortion())
            {
                return false;
            }
        }
        var litres = GetTotalLitres(cookingSlotsProvider, inputStack);
        return litres > 0 && litres <= (Attributes["capacityLitres"]?.AsFloat() ?? 12f);
    }
    
    private float GetTotalLitres(ISlotProvider cookingSlotsProvider, ItemStack inputStack)
    {
        var totalLitres = cookingSlotsProvider.Slots.Sum(slot => slot.Itemstack.GetLitres());
        if (inputStack.Collectible is BlockKettle kettle && kettle.GetContent(inputStack) != null)
            totalLitres += kettle.GetContent(inputStack).GetLitres();
        return totalLitres;
    }

    public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
    {
        if (!CanSmelt(world, cookingSlotsProvider, inputSlot.Itemstack, outputSlot.Itemstack)) return;
        ItemStack product = new ItemStack(world.GetItem(new AssetLocation("boilingwaterportion")));
        product.StackSize = 0;

        product.StackSize += (int) (GetTotalLitres(cookingSlotsProvider, inputSlot.Itemstack) * 100f);
        if (product.StackSize == 0) return;
        
        foreach (var t in cookingSlotsProvider.Slots)
        {
            t.Itemstack = null;
        }
        if (inputSlot.Itemstack.Collectible is BlockKettle kettle)
            kettle.TryTakeLiquid(inputSlot.Itemstack, kettle.GetContent(inputSlot.Itemstack).GetLitres());
        outputSlot.Itemstack = inputSlot.TakeOut(1);
        (outputSlot.Itemstack.Collectible as BlockKettle)?.TryPutLiquid(outputSlot.Itemstack, product, product.StackSize);
    }

    public override float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
    {
        if (!CanSmelt(world, cookingSlotsProvider, inputSlot?.Itemstack, null)) return float.MaxValue;
        float speed = 10f;
        float litres = GetTotalLitres(cookingSlotsProvider, inputSlot?.Itemstack);
        return 30 * litres / speed;
    }

    public override float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
    {
        if (!CanSmelt(world, cookingSlotsProvider, inputSlot.Itemstack, null)) return float.MaxValue;
        float temp = 100f;
        return temp;
    }
   
    public string GetOutputText(IWorldAccessor world, InventorySmelting inv)
    {
        ItemStack product = new ItemStack(world.GetItem(new AssetLocation("boilingwaterportion")));
        var litres = GetTotalLitres(inv, inv[1]?.Itemstack);
        return litres == 0f ? "" : Lang.Get("firepit-gui-willcreate", litres, Lang.Get($"{BtCore.Modid}:firepit-gui-litres", product.GetName()));
    }
}