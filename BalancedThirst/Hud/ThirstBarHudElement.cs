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
        private float _lastHydration;
        private float _lastMaxHydration;
        
        private static bool ShouldShowThirstBar => ConfigSystem.ConfigServer.EnableThirst;
        private static double[] ThirstBarColor => ModGuiStyle.FromHex(ConfigSystem.ConfigClient.ThirstBarColor);
        private bool FirstComposed { get; set; }
        
        public ThirstBarHudElement(ICoreClientAPI capi) : base(capi)
        {
            capi.Event.RegisterGameTickListener(OnGameTick, 20);
            capi.Event.RegisterGameTickListener(OnFlashStatbars, 2500);
            capi.Event.RegisterEventBusListener(ReloadBars, filterByEventName: EventIds.ConfigReloaded);
        }

        private void ReloadBars(string eventname, ref EnumHandling handling, IAttribute data)
        {
            if (!FirstComposed) return;
            ClearComposers();
            Dispose();
            ComposeGuis();
            if (ShouldShowThirstBar)
                UpdateThirstBar(true);
        }

        private void OnGameTick(float dt)
        {
            if (ShouldShowThirstBar)
                UpdateThirstBar();
        }
        
        public override void OnOwnPlayerDataReceived()
        {
            ComposeGuis();
            OnGameTick(1);
        }
        
        private void UpdateThirstBar(bool forceReload = false)
        {
            var thirstTree = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":thirst");
            if (thirstTree == null || _thirstBar == null) return;

            var currentHydration = thirstTree.TryGetFloat("currenthydration");
            var maxHydration = thirstTree.TryGetFloat("maxhydration");

            if (!currentHydration.HasValue || !maxHydration.HasValue) return;

            var isHydrationChanged = Math.Abs(_lastHydration - currentHydration.Value) >= 0.1;
            var isMaxHydrationChanged = Math.Abs(_lastMaxHydration - maxHydration.Value) >= 0.1;

            if (!isHydrationChanged && !isMaxHydrationChanged && !forceReload) return;

            _thirstBar.SetLineInterval(100f);
            _thirstBar.SetValues(currentHydration.Value, 0.0f, maxHydration.Value);

            _lastHydration = currentHydration.Value;
            _lastMaxHydration = maxHydration.Value;
        }
        
        private void OnFlashStatbars(float dt)
        {
            var thirstTree = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":thirst");
            if (thirstTree == null || _thirstBar == null) return;
            var nullable2 = thirstTree.TryGetFloat("currenthydration");
            var nullable1 = thirstTree.TryGetFloat("maxhydration");
            var nullable3 = nullable2.HasValue & nullable1.HasValue ? nullable2.GetValueOrDefault() / (double) nullable1.GetValueOrDefault() : new double?();
            var num = 0.2;
            if (nullable3.GetValueOrDefault() < num & nullable3.HasValue) 
                _thirstBar.ShouldFlash = true;
        }
        
        private void ComposeGuis()
        {
            FirstComposed = true;
            var num = 850f;
            var parentBounds = GenParentBounds();
            var thirstBarBounds = ElementStdBounds.Statbar(EnumDialogArea.RightBottom, num * 0.41)
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
            
            _thirstBar.Hide = !ShouldShowThirstBar;
            Composers["thirstbar"] = compo;
            
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