using System;
using BalancedThirst.Hud;
using BalancedThirst.ModBehavior;
using BalancedThirst.Network;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace BalancedThirst.Systems;

public class DrinkNetwork : ModSystem
{
    public override double ExecuteOrder() => 2.02;

    #region Client

    private IClientNetworkChannel _clientChannel;
    private ICoreClientAPI _capi;

    private static SimpleParticleProperties _waterParticles;
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        _capi = api;
        _clientChannel =
            api.Network.RegisterChannel(BtCore.Modid + ".drink")
                .RegisterMessageType(typeof(DrinkMessage.Request))
                .RegisterMessageType(typeof(DrinkMessage.Response))
                .SetMessageHandler<DrinkMessage.Response>(OnServerDrinkMessage)
                .RegisterMessageType(typeof(DowsingRodMessage))
                .SetMessageHandler<DowsingRodMessage>(OnDowsingRodMessage);

        api.Input.InWorldAction += OnEntityAction;
        api.Event.AfterActiveSlotChanged += _ => OnClientTick(0);
        api.World.RegisterGameTickListener(OnClientTick, 200);
        api.Gui.RegisterDialog(new HudInteractionHelp(api));
    }

    private void OnClientTick(float dt)
    {
        var player = _capi.World.Player;
        if (ConfigSystem.ConfigServer.EnableThirst && !player.IsLookingAtInteractable() &&
            player.IsLookingAtDrinkableBlock() && player.Entity.RightHandItemSlot.Empty && !player.Entity.IsHydrationMaxed())
            _capi.Event.PushEvent(EventIds.Interaction,
                new StringAttribute(BtConstants.InteractionIds.Drink));
    }

    private void OnEntityAction(EnumEntityAction action, bool on, ref EnumHandling handled)
    {
        if (action != EnumEntityAction.InWorldRightMouseDown) return;
        var world = _capi.World;
        var player = world.Player.Entity;
        if (!ConfigSystem.ConfigServer.EnableThirst
            || !player.RightHandItemSlot.Empty
            || player.IsHydrationMaxed()
            || player.Player is not IClientPlayer clientPlayer
            || clientPlayer.IsLookingAtInteractable()
            || !clientPlayer.IsLookingAtDrinkableBlock()) return;
        var blockSel = Raycast.RayCastForFluidBlocks(player.Player);
        var waterPos = blockSel?.Position;
        if (waterPos == null) return;
        if (world.BlockAccessor?.GetBlock(waterPos)?.GetBlockHydrationProperties() == null) return;
        _clientChannel.SendPacket(new DrinkMessage.Request() { Position = waterPos });
        handled = EnumHandling.Handled;
    }
    
    private void OnServerDrinkMessage(DrinkMessage.Response response)
    {
        var entity = _capi.World.Player.Entity;
        var dualCallByPlayer = entity.World.PlayerByUid(entity.PlayerUID);
        entity?.PlayEntitySound("drink", dualCallByPlayer);
        SpawnDrinkParticles(_capi.World.Player.Entity, response.Position);
    }
    #endregion
    
    #region Server

    private IServerNetworkChannel _serverChannel;
    private ICoreServerAPI sapi;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        _serverChannel =
            api.Network.RegisterChannel(BtCore.Modid + ".drink")
                .RegisterMessageType(typeof(DrinkMessage.Request))
                .RegisterMessageType(typeof(DrinkMessage.Response))
                .SetMessageHandler<DrinkMessage.Request>(HandleDrinkAction)
                .RegisterMessageType(typeof(DowsingRodMessage));
    }
    
    private void HandleDrinkAction(IServerPlayer player, DrinkMessage.Request request)
    {
        var pos = request.Position;
        var blockSel = Raycast.RayCastForFluidBlocks(player);
        if (pos == null) return;
        var block = player?.Entity?.World?.BlockAccessor?.GetBlock(pos, BlockLayersAccess.Fluid);
        var hydrationProps = block?.GetBlockHydrationProperties();
        if (hydrationProps == null) return;
        if (pos.IsRiverBlock(player.Entity.World)) hydrationProps.Purity = EnumPurityLevel.Potable;
        if (!(player.Entity?.HasBehavior<EntityBehaviorThirst>() ?? false)) return;
        player.Entity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProps/5);
        DrinkableBehavior.PlayDrinkSound(player.Entity, 2);
        SpawnDrinkParticles(player.Entity,blockSel.HitPosition);
        _serverChannel.SendPacket(new DrinkMessage.Response() { Position = blockSel.HitPosition }, player);
    }
    
    #endregion

    private static void SpawnDrinkParticles(Entity byEntity, Vec3d pos)
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
    
    private void OnDowsingRodMessage(DowsingRodMessage message)
    {
        var player = _capi.World.Player;
        var direction = message.Position.ToVec3d().Sub(player.Entity.Pos.XYZ).Normalize().ClampY();
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
        
        var directionParticles = CreateParticleProperties(currentPosition);
        _capi.World.SpawnParticles(directionParticles, _capi.World.Player);
        var nextPosition = currentPosition.AddCopy(0.2*direction);
        if (remainingCalls % 10 == 0)
        {
            var
                soundPath = new AssetLocation("sounds/environment/smallsplash");
            world.PlaySoundAt(soundPath, currentPosition.X, currentPosition.Y, currentPosition.Z, _capi.World.Player,
                true, 16f);
        }
        Action<float> spawnParticlesAction = _ =>
            SpawnDowsingRodParticles(world, direction, nextPosition, targetPosition, remainingCalls - 1, maxDistance, dt);
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
        var vectorToTarget = targetPosition.SubCopy(currentPosition);
        var dotProduct = vectorToTarget.Dot(direction);
        return dotProduct < 0;
    }
    
}