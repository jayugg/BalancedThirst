using System;
using Vintagestory.API.Client;
namespace BalancedThirst.Hud
{
    public class ThirstBarHudElement : HudElement
    {
        private GuiElementStatbar _thirstBar;
        private MyGuiElementStatbar _bladderBar;
        private float _lastHydration;
        private float _lastMaxHydration;
        
        private float _lastBladderLevel;
        private float _lastBladderCapacity;
        
        public ThirstBarHudElement(ICoreClientAPI capi) : base(capi)
        {
            capi.Event.RegisterGameTickListener(OnGameTick, 20);
            capi.Event.RegisterGameTickListener(this.OnFlashStatbars, 2500);
        }

        private void OnGameTick(float dt)
        {
            UpdateThirstBar();
            if (!BtCore.ConfigClient.BladderBarVisible) return;
            UpdateBladderBar();
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
        
        private void UpdateBladderBar()
        {
            var bladderTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":bladder");
            if (bladderTree == null || _bladderBar == null) return;

            float? currentLevel = bladderTree.TryGetFloat("currentlevel");
            float? capacity = bladderTree.TryGetFloat("capacity");

            if (!currentLevel.HasValue || !capacity.HasValue) return;

            bool isLevelChanged = Math.Abs(_lastBladderLevel - currentLevel.Value) >= 0.1;
            bool isCapacityChanged = Math.Abs(_lastBladderCapacity - capacity.Value) >= 0.1;

            if (!isLevelChanged && !isCapacityChanged) return;

            _bladderBar.SetLineInterval(100f);
            _bladderBar.SetValues(currentLevel.Value, 0.0f, capacity.Value);

            _lastBladderLevel = currentLevel.Value;
            _lastBladderCapacity = capacity.Value;
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
                if (bladderTree != null && BtCore.ConfigServer.EnableBladder && !BtCore.ConfigClient.BladderBarVisible)
                {
                    float? currentlevel = bladderTree.TryGetFloat("currentlevel");
                    float? capacity = bladderTree.TryGetFloat("capacity");
                    double? ratio = currentlevel.HasValue & capacity.HasValue
                        ? currentlevel.GetValueOrDefault() / (double)capacity.GetValueOrDefault()
                        : new double?();
                    double num2 = BtCore.ConfigClient.HideBladderBarAt;
                    if (ratio.GetValueOrDefault() > num2 & ratio.HasValue)
                        this._thirstBar.ShouldFlash = true;
                }
            }
        }

        public override void OnOwnPlayerDataReceived()
        {
            ComposeGuis();
            UpdateThirstBar();
            if (!BtCore.ConfigClient.BladderBarVisible) return;
            UpdateBladderBar();
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
            
            if (BtCore.ConfigClient.BladderBarVisible)
            {
                ElementBounds bladderBarBounds = ElementStdBounds.Statbar(EnumDialogArea.RightBottom, num * 0.41)
                    .WithFixedAlignmentOffset(-2.0 + BtCore.ConfigClient.ThirstBarX,
                        10 + BtCore.ConfigClient.ThirstBarY);
                bladderBarBounds.WithFixedHeight(6.0);

                var compo2 = capi.Gui.CreateCompo("bladderbar", parentBounds.FlatCopy().FixedGrow(0.0, 20.0));

                _bladderBar = new MyGuiElementStatbar(capi, bladderBarBounds, ModGuiStyle.BladderBarColor, true, true);

                compo2.BeginChildElements(parentBounds)
                    .AddInteractiveElement(_bladderBar, "bladderbar")
                    .EndChildElements()
                    .Compose();

                this._bladderBar.HideWhenLessThan = BtCore.ConfigClient.HideBladderBarAt;

                this.Composers["bladderbar"] = compo2;
            }

            TryOpen();
        }
        
        public override bool TryClose() => false;

        public override bool ShouldReceiveKeyboardEvents() => false;

        public override bool Focusable => false;
    }
}