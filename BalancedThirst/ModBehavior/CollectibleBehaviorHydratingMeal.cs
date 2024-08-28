using System.Text;
using BalancedThirst.HarmonyPatches.CookedContainer;
using Vintagestory.API.Common;

namespace BalancedThirst.ModBehavior;

public class CollectibleBehaviorHydratingMeal : CollectibleBehavior
{
    public CollectibleBehaviorHydratingMeal(CollectibleObject collObj) : base(collObj)
    {
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        BlockCookedContainer_GetHeldItemInfo_Patch.Postfix(inSlot, dsc, world, withDebugInfo);
    }
}