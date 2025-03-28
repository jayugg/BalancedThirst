using BalancedThirst.Systems;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CharExtraDialogs;

public class CharacterExtraDialogs_UpdateStatBars_Patch
{
    public static bool ShouldSkipPatch() => !ConfigSystem.ConfigServer.EnableThirst;
    public static void Postfix(CharacterExtraDialogs __instance)
    {
        if (ShouldSkipPatch()) return;
        var traverse = Traverse.Create(__instance);
        var api = traverse.Field("capi").GetValue();
        if (api is not ICoreClientAPI capi) return;
        var dlg = traverse.Field("dlg").GetValue() as GuiDialogCharacterBase;
        if (dlg == null) return;
        var composers = dlg.Composers;
        var entity = capi.World.Player.Entity;
        var composer = composers["modstats"];
        if (composer == null || !traverse.Method("IsOpened").GetValue<bool>())
            return;
        var treeAttribute = entity.WatchedAttributes.GetTreeAttribute("balancedthirst:thirst");
        if (treeAttribute == null)
            return;
        var hydration = treeAttribute.GetFloat("currenthydration");
        var max = treeAttribute.GetFloat("maxhydration");
        var euhydration = treeAttribute.GetFloat("euhydration");
        composer.GetDynamicText("hydration").SetNewText((int) hydration + " / " + max);
        composer.GetStatbar("thirstHealthBar").SetLineInterval(max / 10f);
        composer.GetStatbar("thirstHealthBar").SetValues(euhydration, 0.0f, max);
    }
}