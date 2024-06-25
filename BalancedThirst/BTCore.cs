using BalancedThirst.Blocks;
using BalancedThirst.Config;
using BalancedThirst.HoDCompat;
using BalancedThirst.Hud;
using BalancedThirst.Items;
using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
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
            ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, BtConstants.ConfigClientName);
        }
        if (api.ModLoader.IsModEnabled("configlib"))
        {
            _ = new ConfigLibCompat(api);
        }
        if (IsHoDLoaded)
        {
            HydrationConfigLoader.GenerateBTHydrationConfig(api);
        }
    }
    
    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockClass(Modid + "." + nameof(BlockLiquidContainerLeaking), typeof(BlockLiquidContainerLeaking));
        api.RegisterItemClass(Modid + "." + nameof(ItemDowsingRod), typeof(ItemDowsingRod));
        api.RegisterBlockBehaviorClass(Modid + ":GushingLiquid", typeof(BlockBehaviorGushingLiquid));
        api.RegisterBlockBehaviorClass(Modid + ":PureWater", typeof(BlockBehaviorPureWater));
        if (ConfigServer.YieldThirstManagementToHoD) return;
        api.RegisterEntityBehaviorClass(Modid + ":thirst", typeof(EntityBehaviorThirst));
        api.RegisterCollectibleBehaviorClass(Modid + ":Drinkable", typeof(DrinkableBehavior));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        if (ConfigServer.YieldThirstManagementToHoD) return;
        api.Event.OnEntitySpawn += AddEntityBehaviors;
        api.Event.OnEntityLoaded += AddEntityBehaviors;
        
        BtCommands.Register(api, ConfigServer);
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
        if (ConfigServer.YieldThirstManagementToHoD) return;
        capi.Gui.RegisterDialog(new GuiDialog[]
        {
            new ThirstBarHudElement(capi)
        });
    }
    
    private void AddEntityBehaviors(Entity entity)
    {
        if (entity is EntityPlayer)
        {
            entity.AddBehavior(new EntityBehaviorThirst(entity));
        }
    }
    
    public override void AssetsFinalize(ICoreAPI api)
    {

        if (!api.Side.IsServer()) return;
        EditAssets.AddContainerProps(api);
        if (ConfigServer.YieldThirstManagementToHoD) return;
        EditAssets.AddHydrationToCollectibles(api);
    }
}