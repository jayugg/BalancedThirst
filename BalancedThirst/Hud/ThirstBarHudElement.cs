using System;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

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
            capi.Event.RegisterGameTickListener(this.OnFlashStatbars, 2500);
        }

        private void OnGameTick(float dt)
        {
            UpdateThirstBar();
        }

        private void UpdateThirstBar()
        {
            var thirstTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":thirst");
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
        }
        
        private void OnFlashStatbars(float dt)
        {
            var thirstTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":thirst");
            if (thirstTree != null && this._thirstBar != null) 
            {
                float? nullable2 = thirstTree.TryGetFloat("currenthydration");
                float? nullable1 = thirstTree.TryGetFloat("maxhydration");
                double? nullable3 = nullable2.HasValue & nullable1.HasValue ? nullable2.GetValueOrDefault() / (double) nullable1.GetValueOrDefault() : new double?();
                double num = 0.2;
                if (nullable3.GetValueOrDefault() < num & nullable3.HasValue) 
                    this._thirstBar.ShouldFlash = true;
                var bladderTree  = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":bladder");
                if (bladderTree != null && BtCore.ConfigServer.EnableBladder)
                {
                    float? currentlevel = bladderTree.TryGetFloat("currentlevel");
                    float? capacity = bladderTree.TryGetFloat("capacity");
                    double? ratio = currentlevel.HasValue & capacity.HasValue
                        ? currentlevel.GetValueOrDefault() / (double)capacity.GetValueOrDefault()
                        : new double?();
                    double num2 = 0.8;
                    if (ratio.GetValueOrDefault() > num2 & ratio.HasValue)
                        this._thirstBar.ShouldFlash = true;
                }
            }
        }

        public override void OnOwnPlayerDataReceived()
        {
            ComposeGuis();
            UpdateThirstBar();
        }

        private void ComposeGuis()
        {
            float num = 850f;
            ElementBounds parentBounds = new ElementBounds()
            {
                Alignment = EnumDialogArea.CenterBottom,
                BothSizing = ElementSizing.Fixed,
                fixedWidth = num,
                fixedHeight = 25.0
            }.WithFixedAlignmentOffset(0.0, -55.0);
            
            ElementBounds thirstBarBounds = ElementStdBounds.Statbar(EnumDialogArea.RightBottom, num * 0.41)
                .WithFixedAlignmentOffset(-2.0 + BtCore.ConfigClient.ThirstBarX, 21 + BtCore.ConfigClient.ThirstBarY);
            thirstBarBounds.WithFixedHeight(10.0);

            var compo = capi.Gui.CreateCompo("thirstbar", parentBounds.FlatCopy().FixedGrow(0.0, 20.0));
            
            _thirstBar = new GuiElementStatbar(capi, thirstBarBounds, ModGuiStyle.ThirstBarColor, true, false);

            compo.BeginChildElements(parentBounds)
                .AddInteractiveElement(_thirstBar, "thirstbar")
                .EndChildElements()
                .Compose();
            
            this.Composers["thirstbar"] = compo;
            
            TryOpen();
        }
        
        

        public override bool TryClose() => false;

        public override bool ShouldReceiveKeyboardEvents() => false;

        public override bool Focusable => false;
    }
}