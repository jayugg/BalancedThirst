using System;
using System.Text;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

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

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        if (inSlot.Itemstack.Collectible.IsWaterContainer())
        {
            dsc.AppendLine(Lang.Get($"{BtCore.Modid}:iteminfo-storedwater", GetTransitionRateMul(collObj, EnumTransitionType.Perish)));
        }
    }
}