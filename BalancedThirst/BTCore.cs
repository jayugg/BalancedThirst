using BalancedThirst.Blocks;
using BalancedThirst.Compatibility.HoDCompat;
using BalancedThirst.Config;
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
using Vintagestory.API.Datastructures;

namespace BalancedThirst;

public class BtCore : ModSystem
{
    public static ILogger Logger;
    public static string Modid;
    private static ICoreAPI _api;

    public static bool IsHoDLoaded => _api.ModLoader.IsModEnabled("hydrateordiedrate");
    public static bool IsXSkillsLoaded => _api.ModLoader.IsModEnabled("xskills");
    public override double ExecuteOrder() => 0.02;
    
    public override void StartPre(ICoreAPI api)
    {
        _api = api;
        Modid = Mod.Info.ModID;
        Logger = Mod.Logger;
        if (api.ModLoader.IsModEnabled("configlib"))
        {
            _ = new ConfigLibCompat(api);
        }
        if (IsHoDLoaded)
        {
            ItemHydrationConfigLoader.GenerateBTHydrationConfig(api);
            BlockHydrationConfigLoader.GenerateBTHydrationConfig(api);
        }
        ConfigSystem.OnConfigLoaded += ConfigSystem_OnConfigLoaded;
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
        sapi.Event.RegisterEventBusListener(OnConfigReloaded, filterByEventName:EventIds.ConfigReloaded);
        BtCommands.Register(sapi);
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
        capi.Gui.RegisterDialog(new GuiDialog[]
        {
            new ThirstBarHudElement(capi)
        });
    }
    
    private void OnPlayerJoin(EntityPlayer player)
    {
        if (ConfigSystem.ConfigServer.EnableBladder) return;
        player.Stats.Remove("walkspeed", "bladderfull");
    }
    private void AddEntityBehaviors(Entity entity)
    {
        if (entity is EntityPlayer)
        {
            if (ConfigSystem.ConfigServer.EnableThirst)
                entity.AddBehavior(new EntityBehaviorThirst(entity));
            if (ConfigSystem.ConfigServer.EnableBladder)
                entity.AddBehavior(new EntityBehaviorBladder(entity));
        }
    }
    
    private void RemoveEntityBehaviors(Entity entity)
    {
        if (entity is EntityPlayer)
        {
            if (!ConfigSystem.ConfigServer.EnableThirst && entity.HasBehavior<EntityBehaviorThirst>())
                entity.RemoveBehavior(entity.GetBehavior<EntityBehaviorThirst>());
            if (!ConfigSystem.ConfigServer.EnableBladder && entity.HasBehavior<EntityBehaviorBladder>())
                entity.RemoveBehavior(entity.GetBehavior<EntityBehaviorBladder>());
        }
    }
    
    private void OnConfigReloaded(string eventname, ref EnumHandling handling, IAttribute data)
    {
        foreach (IPlayer player in _api.World.AllPlayers)
        {
            if (player.Entity == null) continue;
            RemoveEntityBehaviors(player.Entity);
            AddEntityBehaviors(player.Entity);
        }
    }
    
    private void ConfigSystem_OnConfigLoaded()
    {
        if (!_api.Side.IsServer() || EditAssets.Completed) return;
        EditAssets.Completed = true;
        EditAssets.AddContainerProps(_api);
        if (!ConfigSystem.ConfigServer.EnableThirst) return;
        EditAssets.AddHydrationToCollectibles(_api);
    }
    
    public override void AssetsFinalize(ICoreAPI api)
    {
        if (!api.Side.IsServer() || !ConfigSystem.IsLoaded || EditAssets.Completed) return;
        EditAssets.Completed = true;
        EditAssets.AddContainerProps(api);
        if (ConfigSystem.ConfigServer.EnableThirst) return;
        EditAssets.AddHydrationToCollectibles(api);
    }
}