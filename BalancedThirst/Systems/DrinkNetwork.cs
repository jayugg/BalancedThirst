using BalancedThirst.ModBehavior;
using BalancedThirst.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace BalancedThirst.Systems;
/*
public class DrinkNetwork : ModSystem
{
    public override double ExecuteOrder() => 1.02;
    
    #region Client
    IClientNetworkChannel clientChannel;
    ICoreClientAPI capi;

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;

        clientChannel =
            api.Network.RegisterChannel("playerDrink")
                .RegisterMessageType(typeof(DrinkMessage.Request))
                .RegisterMessageType(typeof(DrinkMessage.Response))
                .SetMessageHandler<DrinkMessage.Request>(OnServerMessage)
            ;
        capi.World.RegisterGameTickListener((dt) => OnClientGameTick(capi, dt), 1000);
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
        }
    }
    
    private void OnServerMessage(DrinkMessage.Request networkMessage)
    {
        capi.ShowChatMessage("Received following message from server: " + networkMessage.message);
        capi.ShowChatMessage("Sending response.");
        clientChannel.SendPacket(new DrinkMessage.Response()
        {
            response = "RE: Hello World!"
        });
    }
    #endregion
    
    
    private void HandleDrinkAction(IServerPlayer player, BlockPos pos)
    {
        BtCore.Logger.Warning("Handling drink action");
        // Get the block at the selected position
        Block block = player.Entity.World.BlockAccessor.GetBlock(pos);

        // Get the hydration properties of the block
        HydrationProperties hydrationProps = block.GetBlockHydrationProperties();
        if (hydrationProps == null) return;

        if (player.Entity.HasBehavior<EntityBehaviorThirst>())
        {
            player.Entity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProps);
        }
        
        DrinkableBehavior.PlayDrinkSound(player.Entity, 2);
        SpawnDrinkParticles(player.Entity);
    }
    
    #region Server
    IServerNetworkChannel serverChannel;
    ICoreServerAPI serverApi;

    public override void StartServerSide(ICoreServerAPI api)
    {
        serverApi = api;

        serverChannel =
            api.Network.RegisterChannel("playerDrink")
                .RegisterMessageType(typeof(DrinkMessage.Request))
                .RegisterMessageType(typeof(DrinkMessage.Response))
                .SetMessageHandler<DrinkMessage.Response>(OnClientMessage)
            ;

        api.ChatCommands.Create("nwtest")
            .WithDescription("Send a test network message")
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(new OnCommandDelegate(OnNwTestCmd));
    }
    
    private TextCommandResult OnNwTestCmd(TextCommandCallingArgs args)
    {
        serverChannel.BroadcastPacket(new DrinkMessage.Request()
        {
            message = "Hello World!",
        });
        return TextCommandResult.Success();
    }
    
    private void OnClientMessage(IPlayer fromPlayer, DrinkMessage.Response networkMessage)
    {
        serverApi.SendMessageToGroup(
            GlobalConstants.GeneralChatGroup,
            "Received following response from " + fromPlayer.PlayerName + ": " + networkMessage.response,
            EnumChatType.Notification
        );
    }
    #endregion

    public static void SpawnDrinkParticles(Entity byEntity)
    {
        Vec3d xyz = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
        xyz.X += byEntity.LocalEyePos.X;
        xyz.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
        xyz.Z += byEntity.LocalEyePos.Z;
        // Get the item from the asset location
        Item item = byEntity.World.GetItem(new AssetLocation(BtCore.Modid, "waterportion-pure"));

        // Create a new ItemStack with the item
        ItemStack itemStack = new ItemStack(item);
        byEntity.World.SpawnCubeParticles(xyz, itemStack, 0.3f, 4, 0.5f, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
    }
    
    private static void GetHydration(
        EntityPlayer entity,
        out float? hydration,
        out float? maxHydration)
    {
        hydration = new float?();
        maxHydration = new float?();
        ITreeAttribute treeAttribute1 = entity.WatchedAttributes.GetTreeAttribute("balancedthirst:thirst");
        if (treeAttribute1 == null) return;
        hydration = treeAttribute1.TryGetFloat("currenthydration");
        maxHydration = treeAttribute1.TryGetFloat("maxhydration");
    }
}*/