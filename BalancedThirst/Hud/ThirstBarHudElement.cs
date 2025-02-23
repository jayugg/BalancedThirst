using System;
using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace BalancedThirst.Hud
{
    public class ThirstBarHudElement : HudElement
    {
        private BetterGuiElementStatbar _thirstBar;
        private BetterGuiElementStatbar _bladderBar;
        private float _lastHydration;
        private float _lastMaxHydration;
        
        private float _lastBladderLevel;
        private float _lastBladderCapacity;
        
        bool ShouldShowBladderBar => ConfigSystem.ConfigClient.BladderBarVisible && ConfigSystem.ConfigServer.EnableBladder;
        bool ShouldShowThirstBar => ConfigSystem.ConfigServer.EnableThirst;
        public double[] ThirstBarColor => ModGuiStyle.FromHex(ConfigSystem.ConfigClient.ThirstBarColor);
        public double[] BladderBarColor => ModGuiStyle.FromHex(ConfigSystem.ConfigClient.BladderBarColor);
        public bool FirstComposed { get; private set; }
        
        public ThirstBarHudElement(ICoreClientAPI capi) : base(capi)
        {
            capi.Event.RegisterGameTickListener(OnGameTick, 20);
            capi.Event.RegisterGameTickListener(this.OnFlashStatbars, 2500);
            capi.Event.RegisterEventBusListener(ReloadBars, filterByEventName: EventIds.ConfigReloaded);
        }

        private void ReloadBars(string eventname, ref EnumHandling handling, IAttribute data)
        {
            if (!FirstComposed) return;
            this.ClearComposers();
            this.Dispose();
            this.ComposeGuis();
            if (ShouldShowThirstBar)
                UpdateThirstBar(true);
            if (ShouldShowBladderBar)
                UpdateBladderBar(true);
        }

        private void OnGameTick(float dt)
        {
            if (ShouldShowThirstBar)
                UpdateThirstBar();
            if (ShouldShowBladderBar)
                UpdateBladderBar();
        }
        
        public override void OnOwnPlayerDataReceived()
        {
            ComposeGuis();
            this.OnGameTick(1);
        }
        
        private void UpdateThirstBar(bool forceReload = false)
        {
            var thirstTree = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":thirst");
            if (thirstTree == null || _thirstBar == null) return;

            float? currentHydration = thirstTree.TryGetFloat("currenthydration");
            float? maxHydration = thirstTree.TryGetFloat("maxhydration");

            if (!currentHydration.HasValue || !maxHydration.HasValue) return;

            bool isHydrationChanged = Math.Abs(_lastHydration - currentHydration.Value) >= 0.1;
            bool isMaxHydrationChanged = Math.Abs(_lastMaxHydration - maxHydration.Value) >= 0.1;

            if (!isHydrationChanged && !isMaxHydrationChanged && !forceReload) return;

            _thirstBar.SetLineInterval(100f);
            _thirstBar.SetValues(currentHydration.Value, 0.0f, maxHydration.Value);

            _lastHydration = currentHydration.Value;
            _lastMaxHydration = maxHydration.Value;
        }
        
        private void UpdateBladderBar(bool forceReload = false)
        {
            var bladderTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":bladder");
            if (bladderTree == null || _bladderBar == null) return;

            float? currentLevel = bladderTree.TryGetFloat("currentlevel");
            float? capacity = bladderTree.TryGetFloat("capacity");

            if (!currentLevel.HasValue || !capacity.HasValue) return;

            bool isLevelChanged = Math.Abs(_lastBladderLevel - currentLevel.Value) >= 0.1;
            bool isCapacityChanged = Math.Abs(_lastBladderCapacity - capacity.Value) >= 0.1;

            if (!isLevelChanged && !isCapacityChanged && !forceReload) return;

            _bladderBar.SetLineInterval(100f);
            _bladderBar.SetValues(currentLevel.Value, 0.0f, capacity.Value);

            _lastBladderLevel = currentLevel.Value;
            _lastBladderCapacity = capacity.Value;
        }
        
        private void OnFlashStatbars(float dt)
        {
            var thirstTree = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":thirst");
            var bladderTree  = this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":bladder");
            if (thirstTree != null && this._thirstBar != null) 
            {
                float? nullable2 = thirstTree.TryGetFloat("currenthydration");
                float? nullable1 = thirstTree.TryGetFloat("maxhydration");
                double? nullable3 = nullable2.HasValue & nullable1.HasValue ? nullable2.GetValueOrDefault() / (double) nullable1.GetValueOrDefault() : new double?();
                double num = 0.2;
                if (nullable3.GetValueOrDefault() < num & nullable3.HasValue) 
                    this._thirstBar.ShouldFlash = true;
                if (bladderTree != null && ShouldShowBladderBar)
                {
                    float? currentlevel = bladderTree.TryGetFloat("currentlevel");
                    float? capacity = bladderTree.TryGetFloat("capacity");
                    double? ratio = currentlevel.HasValue & capacity.HasValue
                        ? currentlevel.GetValueOrDefault() / (double)capacity.GetValueOrDefault()
                        : new double?();
                    if (ratio.GetValueOrDefault() > 1 & ratio.HasValue)
                        this._thirstBar.ShouldFlash = true;
                }
            }
            
            if (bladderTree != null && !ShouldShowBladderBar && this._bladderBar != null)
            {
                float? currentlevel = bladderTree.TryGetFloat("currentlevel");
                float? capacity = bladderTree.TryGetFloat("capacity");
                double? ratio = currentlevel.HasValue & capacity.HasValue
                    ? currentlevel.GetValueOrDefault() / (double)capacity.GetValueOrDefault()
                    : new double?();
                if (ratio.GetValueOrDefault() > 1 & ratio.HasValue)
                    this._bladderBar.ShouldFlash = true;
            }
        }
        
        private void ComposeGuis()
        {
            FirstComposed = true;
            var num = 850f;
            ElementBounds parentBounds = GenParentBounds();
            if (ShouldShowThirstBar)
            {
                ElementBounds thirstBarBounds = ElementStdBounds.Statbar(EnumDialogArea.RightBottom, num * 0.41)
                    .WithFixedAlignmentOffset(-2.0 + ConfigSystem.ConfigClient.ThirstBarX,
                        21 + ConfigSystem.ConfigClient.ThirstBarY);
                thirstBarBounds.WithFixedHeight(10.0);
                var compo = capi.Gui.CreateCompo("thirstbar", parentBounds.FlatCopy().FixedGrow(0.0, 20.0));

                _thirstBar =
                    new BetterGuiElementStatbar(capi, thirstBarBounds, ThirstBarColor, true, false);

                compo.BeginChildElements(parentBounds)
                    .AddInteractiveElement(_thirstBar, "thirstbar")
                    .EndChildElements()
                    .Compose();
                
                this._thirstBar.Hide = !ShouldShowThirstBar;
                this.Composers["thirstbar"] = compo;
            }

            if (ShouldShowBladderBar)
            {
                ElementBounds bladderBarBounds = ElementStdBounds.Statbar(EnumDialogArea.RightBottom, num * 0.41)
                    .WithFixedAlignmentOffset(-2.0 + ConfigSystem.ConfigClient.ThirstBarX,
                        10 + ConfigSystem.ConfigClient.ThirstBarY);
                bladderBarBounds.WithFixedHeight(6.0);

                var compo2 = capi.Gui.CreateCompo("bladderbar", parentBounds.FlatCopy().FixedGrow(0.0, 20.0));

                _bladderBar = new BetterGuiElementStatbar(capi, bladderBarBounds, BladderBarColor, true, true);

                compo2.BeginChildElements(parentBounds)
                    .AddInteractiveElement(_bladderBar, "bladderbar")
                    .EndChildElements()
                    .Compose();
                
                this._bladderBar.HideWhenLessThan = ConfigSystem.ConfigClient.HideBladderBarAt;
                this._bladderBar.Hide = !ShouldShowBladderBar;

                this.Composers["bladderbar"] = compo2;
            }
            
            TryOpen();
        }

        private ElementBounds GenParentBounds()
        {
            return new ElementBounds()
            {
                Alignment = EnumDialogArea.CenterBottom,
                BothSizing = ElementSizing.Fixed,
                fixedWidth = 850f,
                fixedHeight = 25.0
            }.WithFixedAlignmentOffset(0.0, -55.0);
        }

        public override bool TryClose() => false;

        public override bool ShouldReceiveKeyboardEvents() => false;

        public override bool Focusable => false;
    }
}