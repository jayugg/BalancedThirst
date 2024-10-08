using System;
using System.Linq;
using System.Text;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BalancedThirst.ModBehavior;

public class WaterContainerBehavior : DrinkableBehavior
{
    public WaterContainerBehavior(CollectibleObject collObj) : base(collObj)
    {
    }

    internal override HydrationProperties GetHydrationProperties(IWorldAccessor world, ItemStack itemstack, Entity byEntity)
    {
        return base.ExtractContainerHydrationProperties(world, itemstack, byEntity);
    }

    public static float GetTransitionRateMul(CollectibleObject collectible, EnumTransitionType transType)
    {
        try
        {
            JsonObject attribute = collectible.Attributes?["waterTransitionMul"];
            return attribute is { Exists: true } ? attribute.AsFloat(1) : 1;
        }
        catch (Exception ex)
        {
            BtCore.Logger.Error("Error getting water transition multiplier: " + ex.Message);
            return 1;
        }
    }

    public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand, ref EnumHandling bhHandling)
    {
        var res = base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand, ref bhHandling);
        if (activeHotbarSlot.Itemstack is not { } stack) return res;
        if (stack.Collectible is not BlockLiquidContainerBase container) return res;
        var code = stack.Collectible.Code.ToString();
        var content = container.GetContent(stack);
        if (content == null) return res;
        if (code.Contains("kettle-clay"))
        {
            BtCore.Logger.Warning($"Transitioning legacy item {code} in inventory");
            var newContainer = forEntity.World.GetBlock(new AssetLocation(code.Replace("-clay-", "-")));
            if (newContainer == null) return res;
            activeHotbarSlot.Itemstack = new ItemStack(newContainer);
            container.SetContent(activeHotbarSlot.Itemstack, content);
        }
        if (code.Contains("waterskin-leather"))
        {
            BtCore.Logger.Warning($"Transitioning legacy item {code} in inventory");
            var newContainer = forEntity.World.GetBlock(new AssetLocation($"{BtCore.Modid}:gourd-large-carved"));
            if (newContainer == null) return res;
            activeHotbarSlot.Itemstack = new ItemStack(newContainer);
            container.SetContent(activeHotbarSlot.Itemstack, content);
        }
        if (code.Contains("waterskin-pelt"))
        {
            BtCore.Logger.Warning($"Transitioning legacy item {code} in inventory");
            var newContainer = forEntity.World.GetBlock(new AssetLocation($"{BtCore.Modid}:gourd-medium-carved"));
            if (newContainer == null) return res;
            activeHotbarSlot.Itemstack = new ItemStack(newContainer);
            container.SetContent(activeHotbarSlot.Itemstack, content);
        }
        if (code.Equals($"{BtCore.Modid}:woodenbowl-raw"))
        {
            BtCore.Logger.Warning($"Transitioning legacy item {code} in inventory");
            var newContainer = forEntity.World.GetBlock(new AssetLocation($"{BtCore.Modid}:woodenbowl-raw-oak"));
            if (newContainer == null) return res;
            activeHotbarSlot.Itemstack = new ItemStack(newContainer);
            container.SetContent(activeHotbarSlot.Itemstack, content);
        }
        if (code.Equals($"{BtCore.Modid}:woodenbowl-waxed"))
        {
            BtCore.Logger.Warning($"Transitioning legacy item {code} in inventory");
            var newContainer = forEntity.World.GetBlock(new AssetLocation($"{BtCore.Modid}:woodenbowl-waxed-pine"));
            if (newContainer == null) return res;
            activeHotbarSlot.Itemstack = new ItemStack(newContainer);
            container.SetContent(activeHotbarSlot.Itemstack, content);
        }
        return res;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        if (inSlot.Itemstack.Collectible.IsWaterContainer())
        {
            dsc.AppendLine(Lang.Get($"{BtCore.Modid}:iteminfo-storedwater", GetTransitionRateMul(collObj, EnumTransitionType.Perish)));
        }
    }
}