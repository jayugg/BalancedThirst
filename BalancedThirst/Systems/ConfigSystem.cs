using BalancedThirst.Config;
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
    public static SyncedConfig SyncedConfig { get; set; } = new();
    public static SyncedConfig SyncedConfigData => ConfigServer ?? SyncedConfig;

    public static void StartPre(ICoreAPI api)
    {
        _api = api;
        SyncedConfig = ModConfig.ReadConfig<SyncedConfig>(api, BtConstants.SyncedConfigName);
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
    
    public static void StartClientSide(ICoreClientAPI api)
    {
        _clientChannel = api.Network.RegisterChannel(_channelName)
            .RegisterMessageType<SyncedConfig>()
            .SetMessageHandler<SyncedConfig>(ReloadSyncedConfig);
        api.Event.RegisterEventBusListener(AdminSendSyncedConfig, filterByEventName: EventIds.AdminSetConfig);
    }

    private static void AdminSendSyncedConfig(string eventname, ref EnumHandling handling, IAttribute data)
    {
        _clientChannel?.SendPacket(ModConfig.ReadConfig<SyncedConfig>(_api, BtConstants.SyncedConfigName));
    }
    
    public static void ResetModBoosts(EntityPlayer player)
    {
        if (player == null) return;
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
    
    private static void ReloadSyncedConfig(SyncedConfig packet)
    {
        BtCore.Logger.Warning("Reloading synced config");
        ModConfig.WriteConfig(_api, BtConstants.SyncedConfigName, packet);
        SyncedConfig = packet.Clone();
        if (SyncedConfig.ResetModBoosts)
        {
            ResetModBoosts((_api as ICoreClientAPI)?.World?.Player?.Entity);
            SyncedConfig.ResetModBoosts = false;
            ModConfig.WriteConfig(_api, BtConstants.SyncedConfigName, SyncedConfig);
        }
        _api?.Event.PushEvent(EventIds.ConfigReloaded);
    }
    #endregion

    #region Server
    
    public static void StartServerSide(ICoreServerAPI api)
    {
        _serverChannel = api.Network.RegisterChannel(_channelName)
            .RegisterMessageType<SyncedConfig>()
            .SetMessageHandler<SyncedConfig>(ForceConfigFromAdmin);
        
        api.Event.PlayerJoin += SendSyncedConfig;
        api.Event.RegisterEventBusListener(SendSyncedConfig, filterByEventName: EventIds.ConfigReloaded);
    }

    private static void ForceConfigFromAdmin(IServerPlayer fromplayer, SyncedConfig packet)
    {
        if (fromplayer.HasPrivilege("controlserver"))
        {
            BtCore.Logger.Warning("Forcing config from admin");
            ModConfig.WriteConfig(_api, BtConstants.SyncedConfigName, packet.Clone());
            SyncedConfig = packet;
            _api?.Event.PushEvent(EventIds.ConfigReloaded);
        }
    }
    
    private static void SendSyncedConfig(string eventname, ref EnumHandling handling, IAttribute data)
    {
        BtCore.Logger.Warning("Config reloaded, sending to all players");
        if (_api?.World == null) return;
        foreach (var player in _api.World.AllPlayers)
        {
            if (player is not IServerPlayer serverPlayer) continue;
            SendSyncedConfig(serverPlayer);
        }
    }

    private static void SendSyncedConfig(IServerPlayer byplayer)
    {
        BtCore.Logger.Warning("Sending config to player: {0}", byplayer.PlayerName);
        _serverChannel?.SendPacket(ModConfig.ReadConfig<SyncedConfig>(_api, BtConstants.SyncedConfigName), byplayer);
    }
    #endregion
}