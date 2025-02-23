using BalancedThirst.Config;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace BalancedThirst.Systems;
public static class ConfigSystem
{
    private static IClientNetworkChannel _clientChannel;
    private static IServerNetworkChannel _serverChannel;
    private const string _channelName = "balancedthirst:config";
    private static ICoreAPI _api;
    
    public static ConfigServer ConfigServer { get; set; }
    public static ConfigClient ConfigClient { get; set; }

    public static void StartPre(ICoreAPI api)
    {
        _api = api;
        if (api.Side.IsClient())
        {
            ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, BtConstants.ConfigClientName);
        }
        else
        {
            ConfigServer = ModConfig.ReadConfig<ConfigServer>(api, BtConstants.ConfigServerName);
        }
    }

    #region Client
    
    public static void ResetModBoosts(EntityPlayer player)
    {
        if (player == null) return;
        player.Attributes?.GetTreeAttribute(BtCore.Modid + ":thirst")?.SetFloat("dehydration", 0);
        if ((player.Api.Side & EnumAppSide.Server) != 0)
        {
            player.GetBehavior<EntityBehaviorThirst>().Dehydration = 0;
        }
        foreach (var stat in ConfigSystem.ConfigServer.ThirstStatMultipliers.Keys)
        {
            player.Stats.Remove(stat, BtCore.Modid + ":thirsty");
        }
        player.Stats.Remove(BtCore.Modid + ":thirstrate", "HoD:cooling");
        player.Stats.Remove(BtCore.Modid + ":thirstrate", "resistheat");
        player.Stats.Remove(BtCore.Modid + ":thirstrate", "dehydration");
        player.Stats.Remove("walkspeed", "bladderfull");
        player.Stats.Remove("walkspeed", "bowelfull");
    }
    #endregion
}