using System;
using BalancedThirst.Hud;
using BalancedThirst.ModBehavior;
using BalancedThirst.Network;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.Systems;

public class DrinkNetwork : ModSystem
{
    public override double ExecuteOrder() => 2.02;

    #region Client
    IClientNetworkChannel _clientChannel;
    ICoreClientAPI _capi;
    
    static SimpleParticleProperties _waterParticles;
    
    long _lastPeeTime;
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        _capi = api;
        _clientChannel =
            api.Network.RegisterChannel(BtCore.Modid + ".drink")
                .RegisterMessageType(typeof(DrinkMessage.Request))
                .RegisterMessageType(typeof(DrinkMessage.Response))
                .SetMessageHandler<DrinkMessage.Response>(OnServerDrinkMessage)
                .RegisterMessageType(typeof(DowsingRodMessage))
                .SetMessageHandler<DowsingRodMessage>(OnDowsingRodMessage)
                .RegisterMessageType(typeof(PeeMessage.Request))
                .RegisterMessageType(typeof(PeeMessage.Response))
                .SetMessageHandler<PeeMessage.Response>(OnServerPeeMessage);

        api.Input.InWorldAction += OnEntityAction;
        api.Event.AfterActiveSlotChanged += (slot) => this.OnClientTick(0);
        api.World.RegisterGameTickListener(OnClientTick, 200);
        api.Gui.RegisterDialog(new HudInteractionHelp(api));
    }

    private void OnClientTick(float dt)
    {
        var player = _capi.World.Player;
        if (ConfigSystem.SyncedConfigData.EnableThirst && !player.IsLookingAtInteractable() &&
            player.IsLookingAtDrinkableBlock() && player.Entity.RightHandItemSlot.Empty)
            _capi.Event.PushEvent(EventIds.Interaction,
                new StringAttribute(BtConstants.InteractionIds.Drink));
        
        if (!ConfigSystem.SyncedConfigData.EnableBladder) return;
        if (!(player.IsBladderOverloaded() || _capi.World.ElapsedMilliseconds - _lastPeeTime < 2000) || !player.Entity.RightHandItemSlot.Empty) return;
        if (ConfigSystem.ConfigClient.PeeMode.IsSitting())
            _capi.Event.PushEvent(EventIds.Interaction,
                new StringAttribute(player.Entity.Controls.FloorSitting ?
                    BtConstants.InteractionIds.Pee : BtConstants.InteractionIds.PeeSit));

        if (ConfigSystem.ConfigClient.PeeMode.IsStanding())
            _capi.Event.PushEvent(EventIds.Interaction,
                new StringAttribute(!player.Entity.Controls.TriesToMove && player.Entity.Controls.CtrlKey ?
                    BtConstants.InteractionIds.Pee : BtConstants.InteractionIds.PeeStand));

        if (ConfigSystem.ConfigClient.PeeMode == EnumPeeMode.None)
        {
            player.IngameError(player, "peemodenotset", Lang.Get(BtCore.Modid+":peemodenotset") );
        }
    }
    public void OnEntityAction(EnumEntityAction action, bool on, ref EnumHandling handled)
    {
        if (action != EnumEntityAction.InWorldRightMouseDown)
        {
            handled = EnumHandling.PassThrough;
            return;
        }
        var world = _capi.World;
        EntityPlayer player = world.Player.Entity;
        if (ConfigSystem.SyncedConfigData.EnableThirst
            && player.RightHandItemSlot.Empty
            && player.Player is IClientPlayer clientPlayer &&
            !clientPlayer.IsLookingAtDrinkableBlock())
        {
            var blockSel = clientPlayer.GetLookLiquidBlockSelection();
            var waterPos = blockSel?.Position;
            if (waterPos == null) return;
            if (world.BlockAccessor?.GetBlock(waterPos)?.GetBlockHydrationProperties() != null)
            {
                _clientChannel.SendPacket(new DrinkMessage.Request() {Position = waterPos});
                handled = EnumHandling.Handled;
                return;
            }
        }
        
        if (ConfigSystem.SyncedConfigData.EnableBladder &&
            (player.Player.IsBladderOverloaded() || world.ElapsedMilliseconds - _lastPeeTime < 2000) && 
            (!player.Controls.TriesToMove && player.Controls.CtrlKey &&
            player.RightHandItemSlot.Empty && 
            ConfigSystem.ConfigClient.PeeMode.IsStanding() ||
            (player.Controls.FloorSitting &&
            ConfigSystem.ConfigClient.PeeMode.IsSitting())))
        {
            _lastPeeTime = world.ElapsedMilliseconds;
            _clientChannel.SendPacket(new PeeMessage.Request() {Position = player.BlockSelection?.Position});
            handled = EnumHandling.Handled;
        }
    }
    
    private void OnServerDrinkMessage(DrinkMessage.Response response)
    {
        var entity = _capi.World.Player.Entity;
        IPlayer dualCallByPlayer = entity.World.PlayerByUid(entity.PlayerUID);
        entity?.PlayEntitySound("drink", dualCallByPlayer);
        SpawnDrinkParticles(_capi.World.Player.Entity, response.Position);
    }
    
    private void OnServerPeeMessage(PeeMessage.Response response)
    {
        var entity = _capi.World.Player.Entity;
        SpawnPeeParticles(entity, response.Position, entity.BlockSelection?.HitPosition);
        EntityBehaviorBladder.PlayPeeSound(entity);
    }
    #endregion
    
    #region Server
    IServerNetworkChannel _serverChannel;
    ICoreServerAPI sapi;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        _serverChannel =
            api.Network.RegisterChannel(BtCore.Modid + ".drink")
                .RegisterMessageType(typeof(DrinkMessage.Request))
                .RegisterMessageType(typeof(DrinkMessage.Response))
                .SetMessageHandler<DrinkMessage.Request>(HandleDrinkAction)
                .RegisterMessageType(typeof(DowsingRodMessage))
                .RegisterMessageType(typeof(PeeMessage.Request))
                .RegisterMessageType(typeof(PeeMessage.Response))
                .SetMessageHandler<PeeMessage.Request>(HandlePeeAction);
    }
    
    private void HandleDrinkAction(IServerPlayer player, DrinkMessage.Request request)
    {
        BlockSelection blockSel = Raycast.RayCastForFluidBlocks(player);
        var pos = blockSel != null ? blockSel.Position : request?.Position;
        if (pos == null) return;
        Block block = player?.Entity?.World?.BlockAccessor?.GetBlock(pos);
        HydrationProperties hydrationProps = block?.GetBlockHydrationProperties();
        if (hydrationProps == null) return;
        if (block.IsRiverBlock(player.Entity.World, pos)) hydrationProps.Purity = EnumPurityLevel.Potable;
        if (player.Entity?.HasBehavior<EntityBehaviorThirst>() ?? false)
        {
            player.Entity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProps/5);
            DrinkableBehavior.PlayDrinkSound(player.Entity, 2);
            SpawnDrinkParticles(player.Entity, blockSel != null? blockSel.HitPosition : request.Position.ToVec3d());
            _serverChannel.SendPacket(new DrinkMessage.Response() { Position = blockSel?.HitPosition }, player);
        }
    }

    private void HandlePeeAction(IServerPlayer player, PeeMessage.Request request)
    {
        if (!player.Entity.HasBehavior<EntityBehaviorBladder>()) return;
        if (request.Position == null) return;
        var bh = player.Entity.GetBehavior<EntityBehaviorBladder>();
        if (!bh.Drain(ConfigSystem.ConfigServer.UrineDrainRate)) return;
        EntityBehaviorBladder.PlayPeeSound(player.Entity);
        SpawnPeeParticles(player.Entity, request.Position, player.CurrentBlockSelection?.HitPosition);
        
        var world = player.Entity.World;
        var block = world.BlockAccessor.GetBlock(request.Position);
        _serverChannel.SendPacket(new PeeMessage.Response() { Position = request.Position }, player);
        if (block is BlockFarmland)
        {
            FertiliseFarmland(world, request.Position);
        } 
        else if (world.BlockAccessor.GetBlock(request.Position.DownCopy()) is BlockFarmland)
        {
            FertiliseFarmland(world, request.Position.DownCopy());
        }
        else if (block is BlockLiquidContainerBase container )
        {
            var waterStack = new ItemStack(world.GetItem(new AssetLocation(BtCore.Modid+":urineportion")));
            container.TryPutLiquid(request.Position, waterStack, 0.1f);
            container.DoLiquidMovedEffects(player, waterStack, waterStack.StackSize, BlockLiquidContainerBase.EnumLiquidDirection.Fill);
        }
        else if (block is BlockToolMold)
        {
            var be = world.BlockAccessor.GetBlockEntity(request.Position) as BlockEntityToolMold; 
            be?.CoolWithWater();
        }
        else if (block is BlockIngotMold)
        {
            var be = world.BlockAccessor.GetBlockEntity(request.Position) as BlockEntityIngotMold; 
            be?.CoolWithWater();
        }
    }
    
    private void FertiliseFarmland(IWorldAccessor world, BlockPos position)
    {
        if (position == null) return;
        var be = world.BlockAccessor.GetBlockEntity(position) as BlockEntityFarmland; 
        be?.WaterFarmland(0.05f);
        if (ConfigSystem.ConfigServer.UrineNutrientChance > world.Rand.NextDouble())
        {
            be?.IncreaseNutrients(ConfigSystem.ConfigServer.UrineNutrientLevels);
        }
    }
    
    #endregion

    public static void SpawnDrinkParticles(Entity byEntity, Vec3d pos)
    {
        if (pos == null) return;
        _waterParticles = new SimpleParticleProperties(10f, 20f, -1, new Vec3d(), new Vec3d(), new Vec3f(-1.5f, 0.0f, -1.5f), new Vec3f(1.5f, 3f, 1.5f), minSize: 0.33f, maxSize: 1f)
            {
                AddPos = new Vec3d(1.0 / 16.0, 0.125, 1.0 / 16.0),
                SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1f),
                ClimateColorMap = "climateWaterTint",
                AddQuantity = 1f,
                MinPos = pos,
                ShouldDieInLiquid = true
            };

        byEntity.World.SpawnParticles(_waterParticles, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
    }

    public static void SpawnPeeParticles(Entity byEntity, BlockPos pos, Vec3d hitPos)
    {
        if (hitPos == null || pos == null) return;
        Vec3d entityPos = byEntity.Pos.XYZ.AddCopy(byEntity.LocalEyePos.SubCopy(0, 0.2, 0));
        Vec3d posVec = pos.ToVec3d().AddCopy(hitPos);
        Vec3d dist = (posVec - entityPos);
        var addVertical = new Vec3f(0, (float)(0.5f*Math.Sqrt(dist.NoY().LengthSq())), 0);
        var velocity = 2.5f * dist.ToVec3f().AddCopy(addVertical).Normalize();
        var xyz = entityPos.AddCopy(0.5 * dist.Normalize());
        var one = new Vec3f(1, 1, 1);

        _waterParticles = new SimpleParticleProperties(1f, 1f, -1, xyz, new Vec3d(), velocity.AddCopy(0.2f*one), velocity.AddCopy(-0.2f*one), minSize: 0.33f, maxSize: 0.75f)
        {
            AddPos = new Vec3d(),
            SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1f),
            ClimateColorMap = "climateWaterTint",
            AddQuantity = 5f,
            GravityEffect = 0.6f,
            ShouldDieInLiquid = true
        };
        byEntity.World.SpawnParticles(_waterParticles, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
    }
    
    private void OnDowsingRodMessage(DowsingRodMessage message)
    {
        IClientPlayer player = _capi.World.Player;
        Vec3d direction = message.Position.ToVec3d().Sub(player.Entity.Pos.XYZ).Normalize().ClampY();
        SpawnDowsingRodParticles(player.Entity.World, direction,
            player.Entity.Pos.XYZ.AddCopy(player.Entity.LocalEyePos).Sub(0, 0.3, 0).Add(direction*1.5f), message.Position.ToVec3d().ClampY(),
            40, 10, 0);
    }
    
    private void SpawnDowsingRodParticles(IWorldAccessor world, Vec3d direction, Vec3d currentPosition, Vec3d targetPosition, int remainingCalls, double maxDistance, float dt)
    {
        if (remainingCalls <= 0 ||
            currentPosition.DistanceTo(_capi.World.Player.Entity.Pos.XYZ) > maxDistance ||
            HasReachedTarget(currentPosition, targetPosition, direction))
        {
            return;
        }
        
        SimpleParticleProperties directionParticles = CreateParticleProperties(currentPosition);
        _capi.World.SpawnParticles(directionParticles, _capi.World.Player);
        Vec3d nextPosition = currentPosition.AddCopy(0.2*direction);
        if (remainingCalls % 10 == 0)
        {
            AssetLocation
                soundPath = new AssetLocation("sounds/environment/smallsplash");
            world.PlaySoundAt(soundPath, currentPosition.X, currentPosition.Y, currentPosition.Z, _capi.World.Player,
                true, 16f);
        }
        Action<float> spawnParticlesAction = (dt) =>
        {
            SpawnDowsingRodParticles(world, direction, nextPosition, targetPosition, remainingCalls - 1, maxDistance, dt);
        };
        world.RegisterCallback(spawnParticlesAction, 50);
    }

    private SimpleParticleProperties CreateParticleProperties(Vec3d currentPosition)
    {
        return new SimpleParticleProperties(10f, 20f, -1, new Vec3d(), new Vec3d(), new Vec3f(-0.5f, -0.5f, -0.5f), new Vec3f(0.5f, 0.5f, 0.5f), minSize: 0.33f, maxSize: 1f)
        {
            AddPos = new Vec3d(1.0 / 16.0, 0.125, 1.0 / 16.0),
            ClimateColorMap = "climateWaterTint",
            AddQuantity = 2f,
            GravityEffect = 0,
            SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1f),
            OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -200),
            MinPos = currentPosition
        };
    }
    
    private bool HasReachedTarget(Vec3d currentPosition, Vec3d targetPosition, Vec3d direction)
    {
        Vec3d vectorToTarget = targetPosition.SubCopy(currentPosition);
        double dotProduct = vectorToTarget.Dot(direction);
        return dotProduct < 0;
    }
    
}