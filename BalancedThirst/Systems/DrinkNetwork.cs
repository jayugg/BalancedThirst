using System;
using BalancedThirst.ModBehavior;
using BalancedThirst.Network;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.Systems;

public class DrinkNetwork : ModSystem
{
    public override double ExecuteOrder() => 2.02;

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return !BtCore.ConfigServer.YieldThirstManagementToHoD;
    }

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
                .SetMessageHandler<DrinkMessage.Response>(OnServerDrinkMessage)
                .RegisterMessageType(typeof(DowsingRodMessage))
                .SetMessageHandler<DowsingRodMessage>(OnDowsingRodMessage)
                .RegisterMessageType(typeof(PeeMessage.Request))
                .RegisterMessageType(typeof(PeeMessage.Response))
                .SetMessageHandler<PeeMessage.Response>(OnServerPeeMessage);
        capi.World.RegisterGameTickListener((dt) => OnClientGameTick(capi, dt), 200);
    }
    
    public void OnClientGameTick(ICoreClientAPI capi, float dt)
    {
        var world = capi.World;
        EntityPlayer player = world.Player.Entity;
        if (player.Controls.RightMouseDown && player.RightHandItemSlot.Empty && !player.Controls.Sprint)
        {
            var selPos = player.BlockSelection?.Position;
            var selFace = player.BlockSelection?.Face;
            var waterPos = selPos?.AddCopy(selFace);
            clientChannel.SendPacket(new DrinkMessage.Request() {Position = waterPos});
        }

        if (
            (player.Controls.Sprint &&
            player.Controls.RightMouseDown &&
            (player.RightHandItemSlot.Empty || player.LeftHandItemSlot.Empty) &&
            BtCore.ConfigClient.PeeMode.IsStanding()) ||
            (player.Controls.RightMouseDown && 
            player.Controls.FloorSitting &&
            BtCore.ConfigClient.PeeMode.IsSitting())
            )
        {
            clientChannel.SendPacket(new PeeMessage.Request() {Position = player.BlockSelection?.Position});
        }
    }
    
    private void OnServerDrinkMessage(DrinkMessage.Response response)
    {
        var entity = capi.World.Player.Entity;
        IPlayer dualCallByPlayer = entity.World.PlayerByUid(entity.PlayerUID);
        entity?.PlayEntitySound("drink", dualCallByPlayer);
        SpawnDrinkParticles(capi.World.Player.Entity, response.Position.ToVec3d());
    }
    
    private void OnServerPeeMessage(PeeMessage.Response response)
    {
        var entity = capi.World.Player.Entity;
        SpawnPeeParticles(entity, response.Position, entity.BlockSelection?.HitPosition);
        EntityBehaviorBladder.PlayPeeSound(entity);
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
                .RegisterMessageType(typeof(DowsingRodMessage))
                .RegisterMessageType(typeof(PeeMessage.Request))
                .RegisterMessageType(typeof(PeeMessage.Response))
                .SetMessageHandler<PeeMessage.Request>(HandlePeeAction);
    }
    
    private void HandleDrinkAction(IServerPlayer player, DrinkMessage.Request request)
    {
        BlockSelection blockSel = RayCastForFluidBlocks(player);
        var pos = blockSel != null ? blockSel.Position : request?.Position;
        Block block = player?.Entity?.World?.BlockAccessor?.GetBlock(pos);
        HydrationProperties hydrationProps = block?.GetBlockHydrationProperties();
        if (hydrationProps == null) return;
        if (player.Entity?.HasBehavior<EntityBehaviorThirst>() ?? false)
        {
            player.Entity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProps/5);
            DrinkableBehavior.PlayDrinkSound(player.Entity, 2);
            SpawnDrinkParticles(player.Entity, blockSel != null? blockSel.HitPosition : request.Position.ToVec3d());
            serverChannel.SendPacket(new DrinkMessage.Response() { Position = pos }, player);
        }
    }
    
    private BlockSelection RayCastForFluidBlocks(IServerPlayer player, float range = 5)
    {
        var fromPos = player.Entity.ServerPos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
        var toPos = fromPos.AheadCopy(range, player.Entity.ServerPos.Pitch, player.Entity.ServerPos.Yaw);
        var step = toPos.Sub(fromPos).Normalize().Mul(0.1);
        var currentPos = fromPos.Clone();
        int dimensionId = (int)(player.Entity.ServerPos.Y / BlockPos.DimensionBoundary);

        while (currentPos.SquareDistanceTo(fromPos) <= range * range)
        {
            var blockPos = new BlockPos((int)currentPos.X, (int)currentPos.Y, (int)currentPos.Z, dimensionId);
            var block = player.Entity.World.BlockAccessor.GetBlock(blockPos);

            if (block.BlockMaterial == EnumBlockMaterial.Liquid)
            {
                return new BlockSelection { Position = blockPos, HitPosition = currentPos.Clone() };
            }
            else if (block.BlockMaterial != EnumBlockMaterial.Air)
            {
                return null;
            }
            currentPos.Add(step);
        }
        return null;
    }

    private void HandlePeeAction(IServerPlayer player, PeeMessage.Request request)
    {
        if (!player.Entity.HasBehavior<EntityBehaviorBladder>()) return;
        var bh = player.Entity.GetBehavior<EntityBehaviorBladder>();
        if (!bh.Drain(BtCore.ConfigServer.UrineDrainRate)) return;
        EntityBehaviorBladder.PlayPeeSound(player.Entity);
        SpawnPeeParticles(player.Entity, request?.Position, player.CurrentBlockSelection?.HitPosition);
        
        var world = player.Entity.World;
        var block = world.BlockAccessor.GetBlock(request?.Position);
        serverChannel.SendPacket(new PeeMessage.Response() { Position = request?.Position }, player);
        if (block is BlockFarmland)
        {
            FertiliseFarmland(world, request?.Position);
        } 
        else if (world.BlockAccessor.GetBlock(request?.Position.DownCopy()) is BlockFarmland)
        {
            FertiliseFarmland(world, request?.Position.DownCopy());
        }
        else if (block is BlockLiquidContainerBase container )
        {
            var waterStack = new ItemStack(world.GetItem(new AssetLocation(BtCore.Modid+":urineportion")));
            container.TryPutLiquid(request?.Position, waterStack, 0.1f);
        }
    }
    
    private void FertiliseFarmland(IWorldAccessor world, BlockPos position)
    {
        if (position == null) return;
        var be = world.BlockAccessor.GetBlockEntity(position) as BlockEntityFarmland; 
        be?.WaterFarmland(0.05f);
        if (BtCore.ConfigServer.UrineNutrientChance > world.Rand.NextDouble())
        {
            be.IncreaseNutrients(BtCore.ConfigServer.UrineNutrientLevels);
        }
    }
    
    #endregion

    public static void SpawnDrinkParticles(Entity byEntity, Vec3d pos)
    {
        Vec3d posVec = pos.AddCopy(0.5, 0.3, 0.5);
        Vec3d entityPos = byEntity.Pos.XYZ.AddCopy(byEntity.LocalEyePos);
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

        WaterParticles = new SimpleParticleProperties(1f, 1f, -1, xyz, new Vec3d(), velocity.AddCopy(0.2f*one), velocity.AddCopy(-0.2f*one), minSize: 0.33f, maxSize: 0.75f)
        {
            AddPos = new Vec3d(),
            SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1f),
            ClimateColorMap = "climateWaterTint",
            AddQuantity = 5f,
            GravityEffect = 0.6f,
            ShouldDieInLiquid = true
        };
        byEntity.World.SpawnParticles(WaterParticles, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
    }
    
    private void OnDowsingRodMessage(DowsingRodMessage message)
    {
        IClientPlayer player = capi.World.Player;
        Vec3d direction = message.Position.ToVec3d().Sub(player.Entity.Pos.XYZ).Normalize().ClampY();
        SpawnDowsingRodParticles(player.Entity.World, direction,
            player.Entity.Pos.XYZ.AddCopy(player.Entity.LocalEyePos).Sub(0, 0.3, 0).Add(direction*1.5f), message.Position.ToVec3d().ClampY(),
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
                soundPath = new AssetLocation("sounds/environment/smallsplash");
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