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
            ;
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
        IPlayer dualCallByPlayer = entity.World.PlayerByUid(((EntityPlayer) entity).PlayerUID);
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
            ;
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
        
        WaterParticles = new SimpleParticleProperties(10f, 20f, -1, new Vec3d(), new Vec3d(), new Vec3f(-1.5f, 0.0f, -1.5f), new Vec3f(1.5f, 3f, 1.5f), minSize: 0.33f, maxSize: 1f);
        WaterParticles.AddPos = new Vec3d(1.0 / 16.0, 0.125, 1.0 / 16.0);
        WaterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1f);
        WaterParticles.ClimateColorMap = "climateWaterTint";
        WaterParticles.AddQuantity = 1f;
        
        WaterParticles.MinPos = xyz;
        byEntity.World.SpawnParticles(WaterParticles, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
    }
    
}