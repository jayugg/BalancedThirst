﻿using BalancedThirst.BlockEntities;
using BalancedThirst.Blocks;
using BalancedThirst.Config;
using BalancedThirst.Hud;
using BalancedThirst.Items;
using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
using BalancedThirst.Shader;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace BalancedThirst.Systems;

public class BtCore : ModSystem
{
    public static ILogger Logger;
    public static string Modid;
    private static ICoreAPI _api;
    public static bool IsHoDLoaded => _api?.ModLoader.IsModEnabled("hydrateordiedrate") ?? false;
    public static bool IsXSkillsLoaded => _api?.ModLoader.IsModEnabled("xskills") ?? false;
    
    public override void StartPre(ICoreAPI api)
    {
        _api = api;
        Modid = Mod.Info.ModID;
        Logger = Mod.Logger;
        if (api.ModLoader.IsModEnabled("configlib"))
        {
            _ = new ConfigLibCompat(api);
        }
        ConfigSystem.StartPre(api);
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockClass($"{Modid}.{nameof(BlockLiquidContainerLeaking)}", typeof(BlockLiquidContainerLeaking));
        api.RegisterBlockClass($"{Modid}.{nameof(BlockKettle)}", typeof(BlockKettle));
        api.RegisterBlockClass($"{Modid}.{nameof(BlockLiquidContainerSealable)}", typeof(BlockLiquidContainerSealable));
        api.RegisterBlockClass($"{Modid}.{nameof(BlockGourdMotherplant)}", typeof(BlockGourdMotherplant));
        api.RegisterBlockClass($"{Modid}.{nameof(BlockStain)}", typeof(BlockStain));
        api.RegisterBlockEntityClass($"{Modid}.{nameof(BlockEntityKettle)}", typeof(BlockEntityKettle));
        api.RegisterBlockEntityClass($"{Modid}.{nameof(BlockEntitySealable)}", typeof(BlockEntitySealable));
        api.RegisterBlockEntityClass($"{Modid}.{nameof(BlockEntityGourdVine)}", typeof(BlockEntityGourdVine));
        api.RegisterBlockEntityClass($"{Modid}.{nameof(BlockEntityGourdMotherplant)}", typeof(BlockEntityGourdMotherplant));
        api.RegisterBlockEntityClass($"{Modid}.{nameof(BlockEntityStain)}", typeof(BlockEntityStain));
        api.RegisterItemClass($"{Modid}.{nameof(ItemDowsingRod)}", typeof(ItemDowsingRod));
        api.RegisterCropBehavior($"{Modid}:GourdPumpkin", typeof(GourdCropBehavior));
        api.RegisterBlockBehaviorClass($"{Modid}:GushingLiquid", typeof(BlockBehaviorGushingLiquid));
        api.RegisterBlockBehaviorClass($"{Modid}:PureWater", typeof(BlockBehaviorPureWater));
        api.RegisterEntityBehaviorClass($"{Modid}:thirst", typeof(EntityBehaviorThirst));
        api.RegisterEntityBehaviorClass($"{Modid}:bladder", typeof(EntityBehaviorBladder));
        api.RegisterCollectibleBehaviorClass($"{Modid}:Drinkable", typeof(DrinkableBehavior));
        api.RegisterCollectibleBehaviorClass($"{Modid}:WaterContainer", typeof(WaterContainerBehavior));
        api.RegisterCollectibleBehaviorClass($"{Modid}:HydratingFood", typeof(HydratingFoodBehavior));
        api.RegisterCollectibleBehaviorClass($"{Modid}:DisplayMaterial", typeof(CollectibleBehaviorDisplayMaterial));
        api.RegisterCollectibleBehaviorClass($"{Modid}:HydratingMeal", typeof(CollectibleBehaviorHydratingMeal));
    }

    public override void StartServerSide(ICoreServerAPI sapi)
    {
        sapi.Event.OnEntitySpawn += AddEntityBehaviors;
        sapi.Event.OnEntityLoaded += AddEntityBehaviors;
        sapi.Event.PlayerJoin += (player) => OnPlayerJoin(player.Entity);
        sapi.Event.RegisterEventBusListener(OnConfigReloaded, filterByEventName:EventIds.ConfigReloaded);
        BtCommands.Register(sapi);
        ConfigSystem.StartServerSide(sapi);
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
        capi.Gui.RegisterDialog(new GuiDialog[]
        {
            new ThirstBarHudElement(capi)
        });
        ConfigSystem.StartClientSide(capi);
    }
    
    private void OnPlayerJoin(EntityPlayer player)
    {
        ConfigSystem.ResetModBoosts(player);
    }
    private void AddEntityBehaviors(Entity entity)
    {
        if (entity is not EntityPlayer) return;
        RemoveEntityBehaviors(entity);
        if (ConfigSystem.ConfigServer.EnableThirst)
            entity.AddBehavior(new EntityBehaviorThirst(entity));
        if (ConfigSystem.ConfigServer.EnableBladder)
            entity.AddBehavior(new EntityBehaviorBladder(entity));
    }
    
    private void RemoveEntityBehaviors(Entity entity)
    {
        if (entity is not EntityPlayer) return;
        if (!ConfigSystem.ConfigServer!.EnableThirst && entity.HasBehavior<EntityBehaviorThirst>())
            entity.RemoveBehavior(entity.GetBehavior<EntityBehaviorThirst>());
        if (!ConfigSystem.ConfigServer.EnableBladder && entity.HasBehavior<EntityBehaviorBladder>())
            entity.RemoveBehavior(entity.GetBehavior<EntityBehaviorBladder>());
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
    
    public override void AssetsFinalize(ICoreAPI api)
    {
        if (!api.Side.IsServer()) return;
        EditAssets.AddContainerProps(api);
        if (!ConfigSystem.ConfigServer.EnableThirst) return;
        EditAssets.AddHydrationToCollectibles(api);
    }
}