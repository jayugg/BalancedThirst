using System.Text;
using BalancedThirst.HarmonyPatches.CookedContainer;
using Vintagestory.API.Common;

namespace BalancedThirst.HarmonyPatches.Crock;

public class BlockCrock_GetHeldItemInfo_Patch
{
    public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        BlockCookedContainer_GetHeldItemInfo_Patch.Postfix(inSlot, dsc, world, withDebugInfo);
    }
}