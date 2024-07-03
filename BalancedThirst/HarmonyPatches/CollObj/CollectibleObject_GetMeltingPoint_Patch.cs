using BalancedThirst.Systems;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_GetMeltingPoint_Patch
{
    public static bool ShouldSkipPatch => !ConfigSystem.SyncedConfigData.BoilWaterInFirepits;
    public static void Postfix(
        CollectibleObject __instance,
        ref float __result,
        IWorldAccessor world,
        ISlotProvider cookingSlotsProvider,
        ItemSlot inputSlot)
    {
        if (ShouldSkipPatch) return;
        if (__instance is not BlockLiquidContainerBase container) return;
        ItemStack contentStack = container.GetContent(inputSlot.Itemstack);
        if (contentStack == null) return;
        __result = contentStack.Collectible.CombustibleProps?.MeltingPoint ?? 0.0f;
    }
}