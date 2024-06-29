using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace BalancedThirst.Compatibility.HoDCompat;

public class HoDCompat : ModSystem
{
    private ICoreServerAPI _serverApi;
    private ICoreClientAPI _clientApi;
    
    public override double ExecuteOrder() => 1.03;
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server && BtCore.ConfigServer.UseHoDHydrationValues;
    
    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        ItemHydrationConfigLoader.GenerateBTHydrationConfig(api);
        BlockHydrationConfigLoader.GenerateBTHydrationConfig(api);
    }
    
    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        LoadAndApplyHydrationPatches(api);
        LoadAndApplyBlockHydrationPatches(api);
    }
    
    private void LoadAndApplyHydrationPatches(ICoreAPI api)
    {
        List<JObject> hydrationPatches = ItemHydrationConfigLoader.LoadHydrationPatches(api);
        HoDManager.ApplyHydrationPatches(api, hydrationPatches);
    }
    
    private void LoadAndApplyBlockHydrationPatches(ICoreAPI api)
    {
        List<JObject> blockHydrationPatches = BlockHydrationConfigLoader.LoadBlockHydrationConfig(api);
        HoDManager.ApplyBlockHydrationPatches(api, blockHydrationPatches);
    }

}