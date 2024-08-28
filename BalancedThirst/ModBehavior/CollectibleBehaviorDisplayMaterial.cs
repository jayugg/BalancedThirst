using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.Text.RegularExpressions;

namespace BalancedThirst.ModBehavior;

public class CollectibleBehaviorDisplayMaterial : CollectibleBehavior
{
    public CollectibleBehaviorDisplayMaterial(CollectibleObject collObj) : base(collObj)
    {
    }
    
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        var variant = inSlot.Itemstack.Collectible.Attributes["displayvariant"]?.AsString();
        if (variant == null) return;
        var material = inSlot.Itemstack.Collectible?.Variant[variant];
        if (material == null) return;
        material = char.ToUpper(material[0]) + material.Substring(1);

        string description = dsc.ToString();
        string pattern = Regex.Escape(Lang.Get("Material: ")) + ".*";
        string replacement = $"Material: {material}";

        string updatedDescription = Regex.Replace(description, pattern, replacement);
        dsc.Clear();
        dsc.Append(updatedDescription);
    }
}