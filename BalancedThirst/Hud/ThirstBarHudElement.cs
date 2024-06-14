using System;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Client;

namespace BalancedThirst.Hud
{
    public class ThirstBarHudElement : HudElement
    {
        private GuiElementStatbar _thirstBar;
        private float _lastSaturation;
        private float _lastMaxSaturation;

        private EntityBehaviorThirst Behavior => capi.World?.Player?.Entity?.GetBehavior<EntityBehaviorThirst>();

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
            //BtModSystem.Logger.Warning("Updating thirst bar");
            var thirstTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtModSystem.Modid+":thirst");
            if (thirstTree == null || _thirstBar == null) return;

            float? currentSaturation = thirstTree.TryGetFloat("currentsaturation");
            float? maxSaturation = thirstTree.TryGetFloat("maxsaturation");

            if (!currentSaturation.HasValue || !maxSaturation.HasValue) return;

            bool isSaturationChanged = Math.Abs(_lastSaturation - currentSaturation.Value) >= 0.1;
            bool isMaxSaturationChanged = Math.Abs(_lastMaxSaturation - maxSaturation.Value) >= 0.1;

            if (!isSaturationChanged && !isMaxSaturationChanged) return;

            _thirstBar.SetLineInterval(100f);
            _thirstBar.SetValues(currentSaturation.Value, 0.0f, maxSaturation.Value);

            _lastSaturation = currentSaturation.Value;
            _lastMaxSaturation = maxSaturation.Value;
            //BtModSystem.Logger.Warning("Last saturation: " + _lastSaturation + " Last max saturation: " + _lastMaxSaturation);
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