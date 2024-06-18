using BalancedThirst.Blocks;
using BalancedThirst.Hud;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.GameContent;


namespace BalancedThirst;

public class BtCore : ModSystem
{
    public static ILogger Logger;
    public static string Modid;
    
    public override void Start(ICoreAPI api)
    {
        Modid = Mod.Info.ModID;
        Logger = Mod.Logger;
        api.RegisterBlockClass(Modid + "." + nameof(BlockLiquidContainerNoCapacity), typeof(BlockLiquidContainerNoCapacity));
        api.RegisterBlockClass(Modid + "." + nameof(BlockLiquidContainerLeaking), typeof(BlockLiquidContainerLeaking));
        //api.RegisterBlockBehaviorClass(Modid + ":BlockDrinkable", typeof(BlockBehaviorDrinkable));
        api.RegisterEntityBehaviorClass(Modid + ":thirst", typeof(EntityBehaviorThirst));
        api.RegisterCollectibleBehaviorClass(Modid + ":Drinkable", typeof(DrinkableBehavior));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Event.OnEntitySpawn += AddEntityBehaviors;
        api.Event.OnEntityLoaded += AddEntityBehaviors;
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
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
        Assets.AddHydrationToCollectibles(api);
    }
}