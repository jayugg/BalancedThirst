using BalancedThirst.Blocks;
using BalancedThirst.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.BlockLiquidContainer;

public class BlockLiquidContainerBase_SpillContents_Patch
{
    public static void Postfix(
        BlockLiquidContainerBase __instance,
        bool __result,
        ItemSlot containerSlot,
        EntityAgent byEntity,
        BlockSelection blockSel)
    {
        if (!__result) return;
        var world = byEntity.World;
        world.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face))?.GetBehavior<BEBehaviorBurning>()?.KillFire(false);
        world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorBurning>()?.KillFire(false);
        var voxelPos = new Vec3i();
        for (var index1 = -2; index1 < 2; ++index1)
        {
            for (var index2 = -2; index2 < 2; ++index2)
            {
                for (var index3 = -2; index3 < 2; ++index3)
                {
                    var num3 = (int) (blockSel.HitPosition.X * 16.0);
                    var num4 = (int) (blockSel.HitPosition.Y * 16.0);
                    var num5 = (int) (blockSel.HitPosition.Z * 16.0);
                    if (num3 + index1 >= 0 && num3 + index1 <= 15 && num4 + index2 >= 0 && num4 + index2 <= 15 && num5 + index3 >= 0 && num5 + index3 <= 15)
                    {
                        voxelPos.Set(num3 + index1, num4 + index2, num5 + index3);
                        var subPosition = CollectibleBehaviorArtPigment.BlockSelectionToSubPosition(blockSel.Face, voxelPos);
                        if (world.BlockAccessor.GetDecor(blockSel.Position, subPosition)?.FirstCodePart() == "caveart")
                            world.BlockAccessor.BreakDecor(blockSel.Position, blockSel.Face, new int?(subPosition));
                    }
                }
            }
        }
    }
}