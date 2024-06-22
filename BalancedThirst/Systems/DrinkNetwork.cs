using System;
using BalancedThirst.Items;
using BalancedThirst.ModBehavior;
using BalancedThirst.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace BalancedThirst.Systems;

public class DrinkNetwork : ModSystem
{
    public override double ExecuteOrder() => 1.02;
    
    #region Client
    IClientNetworkChannel clientChannel;
    ICoreClientAPI capi;
    
    static SimpleParticleProperties WaterParticles;
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        clientChannel =
            api.Network.RegisterChannel(BtCore.Modid + ".drink")
                .RegisterMessageType(typeof(DrinkMessage.Request))
                .RegisterMessageType(typeof(DrinkMessage.Response))
                .SetMessageHandler<DrinkMessage.Response>(OnServerMessage)
                .RegisterMessageType(typeof(DowsingRodMessage))
                .SetMessageHandler<DowsingRodMessage>(OnDowsingRodMessage);
        capi.World.RegisterGameTickListener((dt) => OnClientGameTick(capi, dt), 200);
    }
    
    public void OnClientGameTick(ICoreClientAPI capi, float dt)
    {
        var world = capi.World;
        EntityPlayer player = world.Player.Entity;
        if (player.Controls.RightMouseDown && player.RightHandItemSlot.Empty)
        {
            var selPos = player.BlockSelection?.Position;
            var selFace = player.BlockSelection?.Face;
            var waterPos = selPos?.AddCopy(selFace);
            if (waterPos == null) return;
            clientChannel.SendPacket(new DrinkMessage.Request() {Position = waterPos});
        }
    }
    
    private void OnServerMessage(DrinkMessage.Response response)
    {
        var entity = capi.World.Player.Entity;
        IPlayer dualCallByPlayer = entity.World.PlayerByUid(entity.PlayerUID);
        entity?.PlayEntitySound("drink", dualCallByPlayer);
        SpawnDrinkParticles(capi.World.Player.Entity, response.Position);
    }
    #endregion
    
    #region Server
    IServerNetworkChannel serverChannel;
    ICoreServerAPI sapi;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        serverChannel =
            api.Network.RegisterChannel(BtCore.Modid + ".drink")
                .RegisterMessageType(typeof(DrinkMessage.Request))
                .RegisterMessageType(typeof(DrinkMessage.Response))
                .SetMessageHandler<DrinkMessage.Request>(HandleDrinkAction)
                .RegisterMessageType(typeof(DowsingRodMessage));
    }
    
    private void HandleDrinkAction(IServerPlayer player, DrinkMessage.Request request)
    {
        BtCore.Logger.Warning("Handling drink action");
        // Get the block at the selected position
        Block block = player.Entity.World.BlockAccessor.GetBlock(request.Position);

        // Get the hydration properties of the block
        HydrationProperties hydrationProps = block.GetBlockHydrationProperties();
        if (hydrationProps == null) return;
        if (player.Entity.HasBehavior<EntityBehaviorThirst>())
        {
            player.Entity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProps/5);
            DrinkableBehavior.PlayDrinkSound(player.Entity, 2);
            SpawnDrinkParticles(player.Entity, request.Position);
            serverChannel.SendPacket(new DrinkMessage.Response() { Position = request.Position }, player);
        }
    }
    #endregion

    public static void SpawnDrinkParticles(Entity byEntity, BlockPos pos)
    {
        Vec3d posVec = pos.ToVec3d().AddCopy(0.5, 0.3, 0.5);
        Vec3d entityPos = byEntity.Pos.XYZ + byEntity.LocalEyePos;
        Vec3d dist = (posVec - entityPos);
        Vec3d xyz = entityPos + 1.5*dist.Normalize();
        
        WaterParticles = new SimpleParticleProperties(10f, 20f, -1, new Vec3d(), new Vec3d(), new Vec3f(-1.5f, 0.0f, -1.5f), new Vec3f(1.5f, 3f, 1.5f), minSize: 0.33f, maxSize: 1f)
            {
                AddPos = new Vec3d(1.0 / 16.0, 0.125, 1.0 / 16.0),
                SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1f),
                ClimateColorMap = "climateWaterTint",
                AddQuantity = 1f,
                MinPos = xyz,
                ShouldDieInLiquid = true
            };

        byEntity.World.SpawnParticles(WaterParticles, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
    }
    
    private void OnDowsingRodMessage(DowsingRodMessage message)
    {
        BtCore.Logger.Warning("Moving player towards: Position " + message.Position);
        IClientPlayer player = capi.World.Player;
        Vec3d direction = message.Position.ToVec3d().Sub(player.Entity.Pos.XYZ).Normalize();
        direction.Y = 0;
        SpawnDowsingRodParticles(player.Entity.World, direction,
            player.Entity.Pos.XYZ.AddCopy(player.Entity.LocalEyePos).Sub(0, 0.3, 0).Add(direction*1.5f), message.Position.ToVec3d().NoY(),
            40, 10, 0);
    }
    
    private void SpawnDowsingRodParticles(IWorldAccessor world, Vec3d direction, Vec3d currentPosition, Vec3d targetPosition, int remainingCalls, double maxDistance, float dt)
    {
        if (remainingCalls <= 0 ||
            currentPosition.DistanceTo(capi.World.Player.Entity.Pos.XYZ) > maxDistance ||
            HasReachedTarget(currentPosition, targetPosition, direction))
        {
            return;
        }
        
        SimpleParticleProperties directionParticles = CreateParticleProperties(currentPosition);
        capi.World.SpawnParticles(directionParticles, capi.World.Player);
        Vec3d nextPosition = currentPosition.AddCopy(0.2*direction);
        if (remainingCalls % 10 == 0)
        {
            AssetLocation
                soundPath = new AssetLocation("sounds/environment/smallsplash"); // Replace with the correct sound path
            world.PlaySoundAt(soundPath, currentPosition.X, currentPosition.Y, currentPosition.Z, capi.World.Player,
                true, 16f, 1f);
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