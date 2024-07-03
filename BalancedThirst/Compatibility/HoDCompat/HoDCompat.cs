using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace BalancedThirst.Compatibility.HoDCompat;

public class HoDCompat : ModSystem
{
    private ICoreServerAPI _serverApi;
    private ICoreClientAPI _clientApi;
    private Dictionary<string, float> PlayerThirstLevels = new Dictionary<string, float>();
    
    public override double ExecuteOrder() => 1.03;
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server && false && ConfigSystem.ConfigServer.UseHoDHydrationValues;
    
    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        ItemHydrationConfigLoader.GenerateBTHydrationConfig(api);
        BlockHydrationConfigLoader.GenerateBTHydrationConfig(api);
    }

    public override void StartServerSide(ICoreServerAPI sapi)
    {
        base.Start(sapi);
        sapi.World.RegisterGameTickListener((dt) => OnServerGameTick(sapi, dt), 200);
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        LoadAndApplyHydrationPatches(api);
        LoadAndApplyBlockHydrationPatches(api);
    }
    
    private void OnServerGameTick(ICoreAPI api, float dt)
    {
        foreach (var player in api.World.AllPlayers)
        {
            if (!PlayerThirstLevels.ContainsKey(player.PlayerUID))
            {
                PlayerThirstLevels.Add(player.PlayerUID, 0);
            }
            float currentThirst = player.Entity.WatchedAttributes.GetFloat("currentThirst");
            float previousThirst = PlayerThirstLevels[player.PlayerUID];
            if (currentThirst < previousThirst)
            {
                float difference = previousThirst - currentThirst;
                player.Entity.GetBehavior<EntityBehaviorBladder>()?.ReceiveFluid(difference);
            }
            PlayerThirstLevels[player.PlayerUID] = currentThirst;
        }
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