using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches;

public class HarmonyPatches : ModSystem
{
    private ICoreAPI _api;
    private Harmony HarmonyInstance;

    public override void Start(ICoreAPI api)
    {
        this._api = api;
        HarmonyInstance = new Harmony(Mod.Info.ModID);
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod("tryEatBegin", BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(CollectibleObject_tryEatBegin_Patch).GetMethod(
                nameof(CollectibleObject_tryEatBegin_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod("tryEatStep", BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(CollectibleObject_tryEatStep_Patch).GetMethod(
                nameof(CollectibleObject_tryEatStep_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod("tryEatStop", BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(CollectibleObject_tryEatStop_Patch).GetMethod(
                nameof(CollectibleObject_tryEatStop_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.GetHeldItemInfo)),
            prefix: typeof(CollectibleObject_GetHeldItemInfo_Patch).GetMethod(
                nameof(CollectibleObject_GetHeldItemInfo_Patch.Prefix)));
        HarmonyInstance.Patch(typeof(BlockLiquidContainerBase).GetMethod("tryEatStop", BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(BlockLiquidContainerBase_tryEatStop_Patch).GetMethod(
                nameof(BlockLiquidContainerBase_tryEatStop_Patch.Postfix)));
    }

    public override void Dispose() {
        HarmonyInstance?.UnpatchAll(Mod.Info.ModID);
    }
    
}