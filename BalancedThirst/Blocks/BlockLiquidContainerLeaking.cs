using System;
using System.Text;
using System.Text.RegularExpressions;
using BalancedThirst.Systems;
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
            var itemAttribute = itemstack?.ItemAttributes?["leakageRate"];
            if (itemAttribute is { Exists: true })
            {
                var leakageRate = itemAttribute.AsFloat(1);
                if (leakageRate is >= 0 and <= 1)
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
    
    protected virtual float LeakagePerTick => 0.1f;

    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
    {
        base.OnHeldIdle(slot, byEntity);

        var currentTime = byEntity.World.ElapsedMilliseconds;
        if (slot.Itemstack.Collectible is not BlockLiquidContainerBase container) return;
        if (container.IsEmpty(slot.Itemstack))
        {
            slot.Itemstack.Attributes.RemoveAttribute("nextLeakage");
            return;
        }
        
        if (slot.Itemstack.Attributes.HasAttribute("nextLeakage"))
        {
            var nextLeakage = slot.Itemstack.Attributes.GetLong("nextLeakage");
            if (currentTime < nextLeakage)
            {
                return;
            }
        }
        else
        {
            slot.Itemstack.Attributes.SetLong("nextLeakage", (long) (currentTime + 1000 / GetLeakageRate(slot.Itemstack)));
            return;
        }
        TryTakeLiquid(slot.Itemstack, LeakagePerTick / slot.Itemstack.StackSize);
        slot.Itemstack.Attributes.SetLong("nextLeakage", (long) (currentTime + 1000 / GetLeakageRate(slot.Itemstack)));
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        var description = dsc.ToString();
        var pattern = Lang.Get("Mod: {0}", ".*");
        var newLine = Lang.Get(BtCore.Modid + ":container-leaky");
        if (inSlot.Itemstack.Collectible is BlockLiquidContainerBase container && container.GetContent(inSlot.Itemstack)?.StackSize > 0)
        {
            newLine = $"<font color=\"#1097b4\">{newLine}</font>";
        }

        var lines = description.Split(new[] { '\n' }, StringSplitOptions.None);
        for (var i = 0; i < lines.Length; i++)
        {
            if (Regex.IsMatch(lines[i], pattern))
            {
                lines[i] = newLine + "\n" + lines[i];
                break;
            }
        }

        dsc.Clear();
        dsc.Append(string.Join("\n", lines));
    }
}