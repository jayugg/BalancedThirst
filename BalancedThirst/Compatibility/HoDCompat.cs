using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace BalancedThirst.Compatibility;

public class HoDCompat : ModSystem
{
    private ICoreServerAPI _serverApi;
    private ICoreClientAPI _clientApi;
    private Dictionary<string, float> PlayerThirstLevels = new Dictionary<string, float>();
    
    public override double ExecuteOrder() => 1.03;
    public override bool ShouldLoad(EnumAppSide forSide) => BtCore.IsHoDLoaded;
    public override void StartServerSide(ICoreServerAPI sapi)
    {
        base.Start(sapi);
        sapi.World.RegisterGameTickListener((dt) => OnServerGameTick(sapi, dt), 200);
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
}