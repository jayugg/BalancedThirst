using System;
using Cairo;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches;

public class CharacterExtraDialogs_ComposeStatsGui_Patch
{
    public static void Postfix(CharacterExtraDialogs __instance)
    {
        BtCore.Logger.Warning("CharacterExtraDialogs.Dlg_ComposeExtraGuis postfix");
        Traverse traverse = Traverse.Create(__instance);
        var api = traverse.Field("capi").GetValue();
        if (api is not ICoreClientAPI capi) return;
        var dlg = traverse.Field("dlg").GetValue() as GuiDialogCharacterBase;
        if (dlg == null) return;
        var composers = dlg.Composers;
        BtCore.Logger.Warning("Composer not null");
        
        // Get the thirst rate
        var entity = capi.World.Player.Entity;
        BtCore.Logger.Warning("Thirst behavior exists");
        float blended = entity.Stats.GetBlended(BtCore.Modid+":thirstrate");
        var num3 = (int) Math.Round(100.0 * blended);
        string text2 = num3 + "%";
        
        float? hydration = new float?();
        float? maxHydration = new float?();
        getHydration(entity, out hydration, out maxHydration);

        ElementBounds bounds1 = composers["playercharacter"].Bounds;
        ElementBounds bounds5 = composers["environment"].Bounds;
        double num1 = bounds5.InnerHeight / (double) RuntimeEnv.GUIScale + 10.0;
        var statsHeight = bounds1.InnerHeight / (double)RuntimeEnv.GUIScale - GuiStyle.ElementToDialogPadding - 20.0 + num1;
        ElementBounds bounds2 = composers["playerstats"].Bounds;
        ElementBounds bounds3 = ElementBounds.Fixed(0.0, 0.0, 235.0, bounds2.InnerHeight).WithFixedPadding(GuiStyle.ElementToDialogPadding);
        ElementBounds bounds4 = bounds3.ForkBoundingParent().WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset((bounds2.renderX + bounds2.OuterWidth + 10.0) / (double) RuntimeEnv.GUIScale, num1 / 2.0).WithFixedOffset(0, 0.45*statsHeight);
        
        ElementBounds elementBounds3 = ElementBounds.Fixed(165.0, 0.0, 120.0, 20.0);
        ElementBounds leftColumnBoundsW = ElementBounds.Fixed(0.0, 0.0, 140.0, 20.0);
        if (hydration.HasValue && maxHydration.HasValue)
        {
            composers["playerstats"]
                .AddStaticText(Lang.Get("Hydration"), CairoFont.WhiteDetailText(),
                    leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText(
                    ((int)hydration.Value).ToString() + " / " + ((int)maxHydration.Value).ToString(),
                    CairoFont.WhiteDetailText(),
                    elementBounds3 = elementBounds3.FlatCopy()
                        .WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY), "hydration");
        }

        var composer = composers["playerstats"].AddStaticText(Lang.Get("Thirst Rate"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText(text2, CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY).WithFixedHeight(30.0), "thirstrate");
        composer.Compose();
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