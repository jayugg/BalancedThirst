using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BalancedThirst.Systems

{
    public class LoadColorMaps : ModSystem
    {
        private ICoreAPI api;

        public override double ExecuteOrder() => 0.3;

        public override void Start(ICoreAPI api) => this.api = api;

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreClientAPI))
                return;
            this.loadColorMaps();
        }
        
        private void loadColorMaps()
        {
            try
            {
                IAsset asset = this.api.Assets.Get("config/colormaps.json");
                if (asset == null)
                {
                    BtCore.Logger.Error("Failed loading config/colormaps.json. Will skip");
                    return;
                }
                foreach (ColorMap map in asset.ToObject<ColorMap[]>())
                    this.api.RegisterColorMap(map);
            }
            catch (Exception ex)
            {
                BtCore.Logger.Error("Failed loading config/colormaps.json. Will skip");
                BtCore.Logger.Error(ex);
            }
        }
    }
}