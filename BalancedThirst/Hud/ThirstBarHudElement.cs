using System;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Client;

namespace BalancedThirst.Hud
{
    public class ThirstBarHudElement : HudElement
    {
        private GuiElementStatbar _thirstBar;
        private float _lastSlake;
        private float _lastMaxSlake;

        public ThirstBarHudElement(ICoreClientAPI capi) : base(capi)
        {
            capi.Event.RegisterGameTickListener(OnGameTick, 20);
        }

        private void OnGameTick(float dt)
        {
            UpdateThirstBar();
        }

        private void UpdateThirstBar()
        {
            var thirstTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":thirst");
            if (thirstTree == null)
            {
                BtCore.Logger.Warning("Thirst tree is null");
                return;
            }
            if (thirstTree == null || _thirstBar == null) return;
            

            BtCore.Logger.Warning("Thirst tree: " + thirstTree);
            float? currentSlake = thirstTree.TryGetFloat("currentslake");
            float? maxSlake = thirstTree.TryGetFloat("maxslake");
            
            BtCore.Logger.Warning("Current thirst: " + currentSlake + " Max thirst: " + maxSlake);

            if (!currentSlake.HasValue || !maxSlake.HasValue) return;

            bool isSlakeChanged = Math.Abs(_lastSlake - currentSlake.Value) >= 0.1;
            bool isMaxSlakeChanged = Math.Abs(_lastMaxSlake - maxSlake.Value) >= 0.1;

            if (!isSlakeChanged && !isMaxSlakeChanged) return;

            _thirstBar.SetLineInterval(100f);
            _thirstBar.SetValues(currentSlake.Value, 0.0f, maxSlake.Value);

            _lastSlake = currentSlake.Value;
            _lastMaxSlake = maxSlake.Value;
            //BtModSystem.Logger.Warning("Last slake: " + _lastSlake + " Last max slake: " + _lastMaxSlake);
        }

        public override void OnOwnPlayerDataReceived()
        {
            ComposeGuis();
            UpdateThirstBar();
        }

        private void ComposeGuis()
        {
            ElementBounds thirstBarBounds = ElementStdBounds.Statbar(EnumDialogArea.RightBottom, 348.5)
                .WithFixedAlignmentOffset(-220, -45)
                .WithFixedHeight(10.0);

            GuiComposer compo = capi.Gui.CreateCompo("thirstBar", thirstBarBounds.FlatCopy().FixedGrow(0.0, 20.0));

            _thirstBar = new GuiElementStatbar(compo.Api, thirstBarBounds, ModGuiStyle.ThirstBarColor, true, true);

            compo.AddInteractiveElement(_thirstBar, "thirstBar");
            compo.Compose();

            Composers["thirstBar"] = compo;
            TryOpen();
        }

        public override bool TryClose() => false;

        public override bool ShouldReceiveKeyboardEvents() => false;

        public override bool Focusable => false;
    }
}