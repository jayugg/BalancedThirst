using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.BlockLiquidContainer;

public class BlockLiquidContainerBase_TryTakeContent_Patch
{
    public static void Postfix(BlockLiquidContainerBase __instance, BlockPos pos, int quantityItem)
    {
        /*
        FieldInfo apiField = typeof(BlockLiquidContainerBase).GetField("api", BindingFlags.NonPublic | BindingFlags.Instance);
        ICoreAPI api = (ICoreAPI)apiField?.GetValue(__instance);
        IBlockAccessor blockAccessor = api?.World.BlockAccessor;
        Block block = blockAccessor?.GetBlock(pos);
        if (block.GetHydrationProperties()
        */
    }
}