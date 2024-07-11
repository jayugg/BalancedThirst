using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BalancedThirst.Util;

public class BetterDrawWorldInteractionUtil : DrawWorldInteractionUtil
{
    private ICoreClientAPI capi;
    private GuiDialog.DlgComposers Composers;
    public new double ActualWidth;
    private string composerKeyCode;
    public new double UnscaledLineHeight = 30.0;
    public new float FontSize = 20f;
    private GuiComposer composer;
    public new Vec4f Color = ColorUtil.WhiteArgbVec;

    public new GuiComposer Composer => this.Composers[this.composerKeyCode];

    public BetterDrawWorldInteractionUtil(
      ICoreClientAPI capi,
      GuiDialog.DlgComposers composers,
      string composerSuffixCode) : base(capi, composers, composerSuffixCode)
    {
      this.capi = capi;
      this.Composers = composers;
      this.composerKeyCode = "worldInteractionHelp" + composerSuffixCode;
    }

    public new void ComposeBlockWorldInteractionHelp(WorldInteraction[] wis)
    {
      if (wis == null || wis.Length == 0)
      {
        this.Composers.Remove(this.composerKeyCode);
      }
      else
      {
        this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-1");
        ElementBounds elementBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
        if (this.composer == null)
          this.composer = this.capi.Gui.CreateCompo(this.composerKeyCode, elementBounds);
        else
          this.composer.Clear(elementBounds);
        this.Composers[this.composerKeyCode] = this.composer;
        double lineHeight = GuiElement.scaled(this.UnscaledLineHeight);
        int num = 0;
        foreach (WorldInteraction wi1 in wis)
        {
          WorldInteraction wi = wi1;
          ItemStack[] stacks = wi.Itemstacks;
          if (stacks != null && wi.GetMatchingStacks != null)
          {
            stacks = wi.GetMatchingStacks(wi, this.capi.World.Player.CurrentBlockSelection, this.capi.World.Player.CurrentEntitySelection);
            if (stacks == null || stacks.Length == 0)
              continue;
          }
          if (stacks != null || wi.ShouldApply == null || wi.ShouldApply(wi, this.capi.World.Player.CurrentBlockSelection, this.capi.World.Player.CurrentEntitySelection))
          {
            ElementBounds bounds1 = ElementBounds.Fixed(0.0, (double) num * (this.UnscaledLineHeight + 8.0), 600.0, 80.0);
            this.composer.AddIf(stacks != null && stacks.Length != 0).AddCustomRender(bounds1.FlatCopy(), (RenderDelegateWithBounds) ((dt, bounds) =>
            {
              long index = this.capi.World.ElapsedMilliseconds / 1000L % (long) stacks.Length;
              float size = (float) lineHeight * 0.8f;
              this.capi.Render.RenderItemstackToGui((ItemSlot) new DummySlot(stacks[index]), bounds.renderX + lineHeight / 2.0 + 1.0, bounds.renderY + lineHeight / 2.0, 100.0, size, ColorUtil.ColorFromRgba(this.Color));
            })).EndIf().AddStaticCustomDraw(bounds1, (DrawDelegateWithBounds) ((ctx, surface, bounds) => this.drawHelp(ctx, surface, bounds, stacks, lineHeight, wi)));
            ++num;
          }
        }
        this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2");
        if (num == 0)
        {
          this.Composers.Remove(this.composerKeyCode);
        }
        else
        {
          this.composer.Compose();
          this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-3");
        }
      }
    }

    public new void drawHelp(
      Context ctx,
      ImageSurface surface,
      ElementBounds currentBounds,
      ItemStack[] stacks,
      double lineheight,
      WorldInteraction wi)
    {
      this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.1");
      double x = 0.0;
      double drawY = currentBounds.drawY;
      double[] numArray = new[]{1.0, 1.0, 1.0, 1.0};
      numArray[0] = Color.X;
      numArray[1] = Color.Y;
      numArray[2] = Color.Z;
      numArray[3] = Color.W;
      CairoFont font = CairoFont.WhiteMediumText().WithColor(numArray).WithFontSize(this.FontSize).WithStroke(GuiStyle.DarkBrownColor, 2.0);
      font.SetupContext(ctx);
      double height = font.GetFontExtents().Height;
      double symbolspacing = 5.0;
      double width = font.GetTextExtents("+").Width;
      if (stacks != null && stacks.Length != 0 || wi.RequireFreeHand)
      {
        GuiElement.RoundRectangle(ctx, x, drawY + 1.0, lineheight, lineheight, 3.5);
        ctx.SetSourceRGBA(numArray);
        ctx.LineWidth = 1.5;
        ctx.StrokePreserve();
        ctx.SetSourceRGBA(new double[4]
        {
          numArray[0],
          numArray[1],
          numArray[2],
          0.5* numArray[3]
        });
        ctx.Fill();
        x += lineheight + symbolspacing + 1.0;
      }
      List<HotKey> hotKeyList = new List<HotKey>();
      if (wi.HotKeyCodes != null)
      {
        foreach (string hotKeyCode in wi.HotKeyCodes)
        {
          HotKey hotKeyByCode = this.capi.Input.GetHotKeyByCode(hotKeyCode);
          if (hotKeyByCode != null)
            hotKeyList.Add(hotKeyByCode);
        }
      }
      else
      {
        HotKey hotKeyByCode = this.capi.Input.GetHotKeyByCode(wi.HotKeyCode);
        if (hotKeyByCode != null)
          hotKeyList.Add(hotKeyByCode);
      }
      foreach (HotKey hk in hotKeyList)
      {
        if (!(hk.Code != "ctrl") || hk.CurrentMapping.Ctrl)
          x = this.DrawHotkey(hk, x, drawY, ctx, font, lineheight, height, width, symbolspacing, numArray);
      }
      foreach (HotKey hk in hotKeyList)
      {
        if (!(hk.Code != "shift") || hk.CurrentMapping.Shift)
          x = this.DrawHotkey(hk, x, drawY, ctx, font, lineheight, height, width, symbolspacing, numArray);
      }
      foreach (HotKey hk in hotKeyList)
      {
        if (!(hk.Code == "shift") && !(hk.Code == "ctrl") && !hk.CurrentMapping.Shift && !hk.CurrentMapping.Ctrl)
          x = this.DrawHotkey(hk, x, drawY, ctx, font, lineheight, height, width, symbolspacing, numArray);
      }
      if (wi.MouseButton == EnumMouseButton.Left)
        x = this.DrawHotkey(this.capi.Input.GetHotKeyByCode("primarymouse"), x, drawY, ctx, font, lineheight, height, width, symbolspacing, numArray);
      if (wi.MouseButton == EnumMouseButton.Right)
        x = this.DrawHotkey(this.capi.Input.GetHotKeyByCode("secondarymouse"), x, drawY, ctx, font, lineheight, height, width, symbolspacing, numArray);
      this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.2");
      string text = ": " + Lang.Get(wi.ActionLangCode);
      this.capi.Gui.Text.DrawTextLine(ctx, font, text, x - 4.0, drawY + (lineheight - height) / 2.0 + 2.0);
      this.ActualWidth = x + font.GetTextExtents(text).Width;
      this.capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.3");
    }

    private double DrawHotkey(
      HotKey hk,
      double x,
      double y,
      Context ctx,
      CairoFont font,
      double lineheight,
      double textHeight,
      double pluswdith,
      double symbolspacing,
      double[] color)
    {
      KeyCombination currentMapping = hk.CurrentMapping;
      if (currentMapping.IsMouseButton(currentMapping.KeyCode))
        return this.DrawMouseButton(currentMapping.KeyCode - 240, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
      if (currentMapping.Ctrl)
        x = HotkeyComponent.DrawHotkey(this.capi, GlKeyNames.ToString(GlKeys.LControl), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
      if (currentMapping.Shift)
        x = HotkeyComponent.DrawHotkey(this.capi, GlKeyNames.ToString(GlKeys.LShift), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
      x = HotkeyComponent.DrawHotkey(this.capi, currentMapping.PrimaryAsString(), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
      return x;
    }

    private double DrawMouseButton(
      int button,
      double x,
      double y,
      Context ctx,
      CairoFont font,
      double lineheight,
      double textHeight,
      double pluswdith,
      double symbolspacing,
      double[] color)
    {
      string keycode;
      switch (button)
      {
        case 0:
        case 2:
          if (x > 0.0)
          {
            this.capi.Gui.Text.DrawTextLine(ctx, font, "+", (double) (int) x + symbolspacing, y + (double) (int) ((lineheight - textHeight) / 2.0) + 2.0);
            x += pluswdith + 2.0 * symbolspacing;
          }
          this.capi.Gui.Icons.DrawIcon(ctx, button == 0 ? "leftmousebutton" : "rightmousebutton", x, y + 1.0, lineheight, lineheight, color);
          return x + lineheight + symbolspacing + 1.0;
        case 1:
          keycode = "mb";
          break;
        default:
          keycode = "b" + button.ToString();
          break;
      }
      return HotkeyComponent.DrawHotkey(this.capi, keycode, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 8.0, color);
    }

    public new void Dispose() => this.composer?.Dispose();
}