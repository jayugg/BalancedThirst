using BalancedThirst.Blocks;
using BalancedThirst.Compatibility.HoDCompat;
using BalancedThirst.Config;
using BalancedThirst.Hud;
using BalancedThirst.Items;
using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
using BalancedThirst.Network;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;


namespace BalancedThirst;

public class BtCore : ModSystem
{
    public static ILogger Logger;
    public static string Modid;
    private static ICoreAPI _api;

    public static bool IsHoDLoaded => _api.ModLoader.IsModEnabled("hydrateordiedrate");
    public static bool IsXSkillsLoaded => _api.ModLoader.IsModEnabled("xskills");
    
    public static ConfigServer ConfigServer { get; set; }
    public static ConfigClient ConfigClient { get; set; }
    
    public override void StartPre(ICoreAPI api)
    {
        _api = api;
        Modid = Mod.Info.ModID;
        Logger = Mod.Logger;
        if (api.Side.IsServer())
        {
            ConfigServer = ModConfig.ReadConfig<ConfigServer>(api, BtConstants.ConfigServerName);
        }
        if (api.Side.IsClient())
        {
            ConfigServer = ModConfig.ReadConfig<ConfigServer>(api, BtConstants.ConfigServerName);
            ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, BtConstants.ConfigClientName);
        }
        if (api.ModLoader.IsModEnabled("configlib"))
        {
            _ = new ConfigLibCompat(api);
        }
        if (IsHoDLoaded)
        {
            ItemHydrationConfigLoader.GenerateBTHydrationConfig(api);
            BlockHydrationConfigLoader.GenerateBTHydrationConfig(api);
        }
    }
    
    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockClass(Modid + "." + nameof(BlockLiquidContainerLeaking), typeof(BlockLiquidContainerLeaking));
        api.RegisterItemClass(Modid + "." + nameof(ItemDowsingRod), typeof(ItemDowsingRod));
        api.RegisterBlockBehaviorClass(Modid + ":GushingLiquid", typeof(BlockBehaviorGushingLiquid));
        api.RegisterBlockBehaviorClass(Modid + ":PureWater", typeof(BlockBehaviorPureWater));
        api.RegisterEntityBehaviorClass(Modid + ":thirst", typeof(EntityBehaviorThirst));
        api.RegisterCollectibleBehaviorClass(Modid + ":Drinkable", typeof(DrinkableBehavior));
        api.RegisterEntityBehaviorClass(Modid + ":bladder", typeof(EntityBehaviorBladder));
    }

    public override void StartServerSide(ICoreServerAPI sapi)
    {
        sapi.Event.OnEntitySpawn += AddEntityBehaviors;
        sapi.Event.OnEntityLoaded += AddEntityBehaviors;
        sapi.Event.PlayerJoin += (player) => OnPlayerJoin(player.Entity);

        BtCommands.Register(sapi, ConfigServer);
    }
    
    public override void StartClientSide(ICoreClientAPI capi)
    {
        if (ConfigServer.YieldThirstManagementToHoD) return;
        capi.Gui.RegisterDialog(new GuiDialog[]
        {
            new ThirstBarHudElement(capi)
        });
    }
    
    private void OnPlayerJoin(EntityPlayer player)
    {
        if (ConfigServer.EnableBladder) return;
        player.Stats.Remove("walkspeed", "bladderfull");
    }
    private void AddEntityBehaviors(Entity entity)
    {
        if (entity is EntityPlayer)
        {
            // Careful with this, it can only run on the server
            if (ConfigServer?.YieldThirstManagementToHoD ?? false)
                entity.AddBehavior(new EntityBehaviorThirst(entity));
            if (ConfigServer?.EnableBladder ?? false)
                entity.AddBehavior(new EntityBehaviorBladder(entity));
        }
    }
    
    public override void AssetsFinalize(ICoreAPI api)
    {

        if (!api.Side.IsServer()) return;
        EditAssets.AddContainerProps(api);
        // Careful, can only run on server side
        if (ConfigServer.YieldThirstManagementToHoD) return;
        EditAssets.AddHydrationToCollectibles(api);
    }
}