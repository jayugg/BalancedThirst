using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BalancedThirst.Blocks;
public class BlockLiquidContainerLeaking : BlockLiquidContainerTopOpened
{
    /// <summary>
    /// Gets the leakage rate of the item stack. The leakage rate must be a float between 0 and 1.
    /// </summary>
    /// <param name="itemstack">The item stack to get the leakage rate from.</param>
    /// <returns>The leakage rate as a float between 0 and 1. If the leakage rate attribute does not exist or an error occurs, it returns 1.</returns>
    public virtual float GetLeakageRate(ItemStack itemstack)
    {
        try
        {
            JsonObject itemAttribute = itemstack?.ItemAttributes?["leakageRate"];
            if (itemAttribute is { Exists: true })
            {
                float leakageRate = itemAttribute.AsFloat(1);
                if (leakageRate >= 0 && leakageRate <= 1)
                {
                    return leakageRate;
                }
                else
                {
                    BtCore.Logger.Warning("Invalid leakage rate: " + leakageRate + ". Setting leakage rate to 1.");
                    return 1;
                }
            }
            return 1;
        }
        catch (Exception ex)
        {
            BtCore.Logger.Error("Error getting leakageRate: " + ex.Message);
            return 1;
        }
    }
    
    protected virtual float LeakagePerTick => 0.01f;
    
    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
    {
        base.OnHeldIdle(slot, byEntity);
        if (this.api.World.Rand.NextSingle() > 1 - GetLeakageRate(slot.Itemstack))
            this.TryTakeLiquid(slot.Itemstack, LeakagePerTick / (float) slot.Itemstack.StackSize);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(Lang.Get("container-leaky"));
    }
}