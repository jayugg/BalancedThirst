using System;
using BalancedThirst.Hud;
using BalancedThirst.Systems;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CharExtraDialogs;

public class CharacterExtraDialogs_Dlg_ComposeExtraGuis_Patch
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
        
        var blended = entity.Stats.GetBlended(BtCore.Modid+":thirstrate");
        var num3 = (int) Math.Round(100.0 * blended);
        var text2 = num3 + "%";
        
        var bounds1 = composers["playercharacter"].Bounds;
        var bounds2 = composers["environment"].Bounds;
        var elementBounds1 = ElementBounds.Fixed(0.0, 25.0, 90.0, 20.0);
        var elementBounds2 = ElementBounds.Fixed(120.0, 30.0, 120.0, 8.0);
        var leftColumnBoundsW = ElementBounds.Fixed(0.0, 0.0, 140.0, 20.0);
        var elementBounds3 = ElementBounds.Fixed(165.0, 0.0, 120.0, 20.0);

        var num1 = bounds2.InnerHeight / RuntimeEnv.GUIScale + 10.0;
        var statsHeight = bounds1.InnerHeight / RuntimeEnv.GUIScale - GuiStyle.ElementToDialogPadding - 20.0 + num1;
        var bounds3 = ElementBounds.Fixed(0.0, 3.0, 235.0, 0.15*statsHeight).WithFixedPadding(GuiStyle.ElementToDialogPadding);
        var bounds4 = bounds3.ForkBoundingParent().WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset((bounds1.renderX + bounds1.OuterWidth + 10.0) / RuntimeEnv.GUIScale, num1 / 2.0).WithFixedOffset(0, 0.42*statsHeight);
        
        float? hydration;
        float? maxHydration;
        getHydration(entity, out hydration, out maxHydration);
        
        composers["modstats"] = capi.Gui.CreateCompo("modstats", bounds4)
            .AddShadedDialogBG(bounds3)
            .AddDialogTitleBar(Lang.Get("Thirst Stats"), () => dlg.OnTitleBarClose())
            .BeginChildElements(bounds3);

        if (hydration.HasValue)
        {
            ElementBounds refBounds;
            composers["modstats"].AddStaticText(Lang.Get(BtCore.Modid+":playerinfo-hydration-boost"), CairoFont.WhiteDetailText(), elementBounds1.WithFixedWidth(90.0)).AddStatbar(refBounds = elementBounds2.WithFixedOffset(0, -5), ModGuiStyle.ThirstBarColor, "thirstHealthBar");
            leftColumnBoundsW = leftColumnBoundsW.FixedUnder(refBounds, -5.0);
        }
        
        if (hydration.HasValue && maxHydration.HasValue)
            composers["modstats"].AddStaticText(Lang.Get(BtCore.Modid+":playerinfo-hydration"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText(((int) hydration.Value) + " / " + ((int) maxHydration.Value), CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY), "hydration");
        
        var composer = composers["modstats"].AddStaticText(Lang.Get("Thirst Rate"), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy()).AddDynamicText(text2, CairoFont.WhiteDetailText(), elementBounds3 = elementBounds3.FlatCopy().WithFixedPosition(elementBounds3.fixedX, leftColumnBoundsW.fixedY).WithFixedHeight(30.0), "thirstrate");
        composer.Compose();
    }

    private static void getHydration(
        EntityPlayer entity,
        out float? hydration,
        out float? maxHydration)
    {
        hydration = new float?();
        maxHydration = new float?();
        var treeAttribute1 = entity.WatchedAttributes.GetTreeAttribute("balancedthirst:thirst");
        if (treeAttribute1 != null)
        {
            hydration = treeAttribute1.TryGetFloat("currenthydration");
            maxHydration = treeAttribute1.TryGetFloat("maxhydration");
        }
        if (hydration.HasValue)
            new float?((int) hydration.Value);
    }
}