using System;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Client;

namespace BalancedThirst.Hud
{
    public class ThirstBarHudElement : HudElement
    {
        private GuiElementStatbar _thirstBar;
        private float _lastHydration;
        private float _lastMaxHydration;
        
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
            BtModSystem.Logger.Warning("Updating thirst bar");
            var thirstTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtModSystem.Modid+":thirst");
            if (thirstTree == null || _thirstBar == null) return;

            float? currentHydration = thirstTree.TryGetFloat("currenthydration");
            float? maxHydration = thirstTree.TryGetFloat("maxhydration");

            if (!currentHydration.HasValue || !maxHydration.HasValue) return;

            bool isHydrationChanged = Math.Abs(_lastHydration - currentHydration.Value) >= 0.1;
            bool isMaxHydrationChanged = Math.Abs(_lastMaxHydration - maxHydration.Value) >= 0.1;

            if (!isHydrationChanged && !isMaxHydrationChanged) return;

            _thirstBar.SetLineInterval(100f);
            _thirstBar.SetValues(currentHydration.Value, 0.0f, maxHydration.Value);

            _lastHydration = currentHydration.Value;
            _lastMaxHydration = maxHydration.Value;
            BtModSystem.Logger.Warning("Last hydration: " + _lastHydration + " Last max hydration: " + _lastMaxHydration);
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