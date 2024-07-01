using System.Reflection;
using BalancedThirst.HarmonyPatches.BlockLiquidContainer;
using BalancedThirst.HarmonyPatches.CharExtraDialogs;
using BalancedThirst.HarmonyPatches.CollObj;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.Systems;

public class HarmonyPatches : ModSystem
{
    private ICoreAPI _api;
    private Harmony HarmonyInstance;

    public override double ExecuteOrder() => 1.04;
    public override void Start(ICoreAPI api)
    {
        this._api = api;
        HarmonyInstance = new Harmony(Mod.Info.ModID);
        if (BtCore.ConfigServer.BoilWaterInFirepits)
        {
            HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.CanSmelt)),
                postfix: typeof(CollectibleObject_CanSmelt_Patch).GetMethod(
                    nameof(CollectibleObject_CanSmelt_Patch.Postfix)));
            HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.DoSmelt)),
                prefix: typeof(CollectibleObject_DoSmelt_Patch).GetMethod(
                    nameof(CollectibleObject_DoSmelt_Patch.Prefix)));
            HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.GetMeltingDuration)),
                postfix: typeof(CollectibleObject_GetMeltingDuration_Patch).GetMethod(
                    nameof(CollectibleObject_GetMeltingDuration_Patch.Postfix)));
            HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.GetMeltingPoint)),
                postfix: typeof(CollectibleObject_GetMeltingPoint_Patch).GetMethod(
                    nameof(CollectibleObject_GetMeltingPoint_Patch.Postfix)));
        }
        // Careful with this, it can technically only run on the server
        if (BtCore.ConfigServer?.YieldThirstManagementToHoD ?? true) return;
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod("tryEatBegin", BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(CollectibleObject_tryEatBegin_Patch).GetMethod(
                nameof(CollectibleObject_tryEatBegin_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod("tryEatStep", BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(CollectibleObject_tryEatStep_Patch).GetMethod(
                nameof(CollectibleObject_tryEatStep_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod("tryEatStop", BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: typeof(CollectibleObject_tryEatStop_Patch).GetMethod(
                nameof(CollectibleObject_tryEatStop_Patch.Prefix)),
            postfix: typeof(CollectibleObject_tryEatStop_Patch).GetMethod(
                nameof(CollectibleObject_tryEatStop_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.GetHeldItemInfo)),
            prefix: typeof(CollectibleObject_GetHeldItemInfo_Patch).GetMethod(
                nameof(CollectibleObject_GetHeldItemInfo_Patch.Prefix)));
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.GetTransitionRateMul)),
            postfix: typeof(CollectibleObject_GetTransitionRateMul_Patch).GetMethod(
                nameof(CollectibleObject_GetTransitionRateMul_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(BlockLiquidContainerBase).GetMethod("tryEatStop", BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: typeof(BlockLiquidContainerBase_tryEatStop_Patch).GetMethod(
                nameof(BlockLiquidContainerBase_tryEatStop_Patch.Prefix)),
            postfix: typeof(BlockLiquidContainerBase_tryEatStop_Patch).GetMethod(
                nameof(BlockLiquidContainerBase_tryEatStop_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(CharacterExtraDialogs).GetMethod("Dlg_ComposeExtraGuis",  BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(CharacterExtraDialogs_Dlg_ComposeExtraGuis_Patch).GetMethod(
                nameof(CharacterExtraDialogs_Dlg_ComposeExtraGuis_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(CharacterExtraDialogs).GetMethod("UpdateStats",  BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(CharacterExtraDialogs_UpdateStats_Patch).GetMethod(
                nameof(CharacterExtraDialogs_UpdateStats_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(CharacterExtraDialogs).GetMethod("UpdateStats",  BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(CharacterExtraDialogs_UpdateStatBars_Patch).GetMethod(
                nameof(CharacterExtraDialogs_UpdateStatBars_Patch.Postfix)));
    }

    public override void Dispose() {
        HarmonyInstance?.UnpatchAll(Mod.Info.ModID);
    }
    
}