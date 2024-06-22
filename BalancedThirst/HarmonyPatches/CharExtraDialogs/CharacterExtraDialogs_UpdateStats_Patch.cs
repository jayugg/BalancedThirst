using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CharExtraDialogs;

public class CharacterExtraDialogs_UpdateStats_Patch
{
    public static void Postfix(CharacterExtraDialogs __instance)
    { 
        Traverse traverse = Traverse.Create(__instance);
        var api = traverse.Field("capi").GetValue();
        if (api is not ICoreClientAPI capi) return;
        var dlg = traverse.Field("dlg").GetValue() as GuiDialogCharacterBase;
        if (dlg == null) return;
        var composers = dlg.Composers;
        var entity = capi.World.Player.Entity;
        GuiComposer composer = composers["modstats"];
        if (composer == null || !traverse.Method("IsOpened").GetValue<bool>())
            return;
        BtCore.Logger.Warning("Updating thirst stats");
        float? hydration;
        float? maxHydration;
        getHydration(entity, out hydration, out maxHydration);
        float blended = entity.Stats.GetBlended(BtCore.Modid+":thirstrate");
        BtCore.Logger.Warning("Blended thirst rate: " + blended);
        BtCore.Logger.Warning("Hydration: " + hydration);
        if (hydration.HasValue && maxHydration.HasValue)
            composer.GetDynamicText("hydration").SetNewText(((int) hydration.Value).ToString() + " / " + ((int) maxHydration.Value).ToString());
        GuiElementDynamicText dynamicText = composer.GetDynamicText("thirstrate");
        if (dynamicText != null)
        {
            var num = (int) Math.Round(100.0 * blended);
            dynamicText.SetNewText(num + "%");
        }
    }
    
    private static void getHydration(
        EntityPlayer entity,
        out float? hydration,
        out float? maxHydration)
    {
        hydration = new float?();
        maxHydration = new float?();
        ITreeAttribute treeAttribute1 = entity.WatchedAttributes.GetTreeAttribute("balancedthirst:thirst");
        if (treeAttribute1 != null)
        {
            hydration = treeAttribute1.TryGetFloat("currenthydration");
            maxHydration = treeAttribute1.TryGetFloat("maxhydration");
        }
        if (hydration.HasValue)
            new float?((float) (int) hydration.Value);
    }
}