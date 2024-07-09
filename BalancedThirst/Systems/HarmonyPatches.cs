using System.Reflection;
using BalancedThirst.HarmonyPatches.BlockLiquidContainer;
using BalancedThirst.HarmonyPatches.CharExtraDialogs;
using BalancedThirst.HarmonyPatches.CollObj;
using BalancedThirst.HarmonyPatches.EntityFirepit;
using BalancedThirst.HarmonyPatches.InvSmelting;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BalancedThirst.Systems;

public class HarmonyPatches : ModSystem
{
    private ICoreAPI _api;
    private Harmony HarmonyInstance;

    public override double ExecuteOrder() => 0.03;
    public override void Start(ICoreAPI api)
    {
        this._api = api;
        HarmonyInstance = new Harmony(Mod.Info.ModID);
        var configData = ConfigSystem.SyncedConfigData;
        
        HarmonyInstance.Patch(typeof(BlockEntityFirepit).GetMethod("GetTemp", BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(BlockEntityFirepit_Temp_Patch).GetMethod(
                nameof(BlockEntityFirepit_Temp_Patch.GetTemp)));
        HarmonyInstance.Patch(typeof(BlockEntityFirepit).GetMethod("SetTemp", BindingFlags.NonPublic | BindingFlags.Instance),
            postfix: typeof(BlockEntityFirepit_Temp_Patch).GetMethod(
                nameof(BlockEntityFirepit_Temp_Patch.SetTemp)));
        HarmonyInstance.Patch(typeof(InventorySmelting).GetMethod(nameof(InventorySmelting.GetOutputText)),
            postfix: typeof(InventorySmelting_GetOutputText_Patch).GetMethod(
                nameof(InventorySmelting_GetOutputText_Patch.Postfix)));
        
        if (!configData?.EnableThirst ?? false) return;
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod("tryEatStop", BindingFlags.NonPublic | BindingFlags.Instance),
            prefix: typeof(CollectibleObject_tryEatStop_Patch).GetMethod(
                nameof(CollectibleObject_tryEatStop_Patch.Prefix)),
            postfix: typeof(CollectibleObject_tryEatStop_Patch).GetMethod(
                nameof(CollectibleObject_tryEatStop_Patch.Postfix)));
        HarmonyInstance.Patch(typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.GetHeldItemInfo)),
            postfix: typeof(CollectibleObject_GetHeldItemInfo_Patch).GetMethod(
                nameof(CollectibleObject_GetHeldItemInfo_Patch.Postfix)));
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