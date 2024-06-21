using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches;

public class CharacterExtraDialogs_UpdateStatBars_Patch
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
        ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute("balancedthirst:thirst");
        if (treeAttribute == null)
            return;
        float hydration = treeAttribute.GetFloat("currenthydration");
        float max = treeAttribute.GetFloat("maxhydration");
        float euhydration = treeAttribute.GetFloat("euhydration");
        BtCore.Logger.Warning(euhydration.ToString());
        composer.GetDynamicText("hydration").SetNewText((int) hydration + " / " + max);
        composer.GetStatbar("thirstHealthBar").SetLineInterval(max / 10f);
        composer.GetStatbar("thirstHealthBar").SetValues(euhydration, 0.0f, max);
    }
}