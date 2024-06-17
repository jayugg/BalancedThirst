using BalancedThirst.Hud;
using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
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
        api.RegisterBlockBehaviorClass(Modid + ":BlockDrinkable", typeof(BlockBehaviorDrinkable));
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
        foreach (Block block in api.World.Blocks)
        {
            if (block?.Code == null)
            {
                continue;
            }
            if (block.Code.ToString().Contains("game:water") || block.Code.ToString().Contains("game:soil"))
            {
                Logger.Warning("Adding drinkable behavior to block: " + block.Code);
                block.BlockBehaviors = block.BlockBehaviors.Append(new BlockBehaviorDrinkable(block));
                var hydrationProperties = new HydrationProperties()
                {
                    Hydration = 100, Contamination = 0.1f
                };
                block.SetHydrationProperties(hydrationProperties);
            }
        }
        if (!api.Side.IsServer()) return;
        foreach (CollectibleObject collectible in api.World.Collectibles)
        {
            if (collectible?.Code == null)
            {
                continue;
            }
            
            if (collectible.Code.ToString().Contains("drinkitem")
                || collectible.Code.ToString().Contains("waterportion")
                || collectible is BlockLiquidContainerBase
                || collectible.Code.ToString().Contains("juice"))
            {
                Logger.Warning("Adding drinkable behavior to collectible: " + collectible.Code);
                var behavior = new DrinkableBehavior(collectible);
                collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
                var hydrationProperties = new HydrationProperties()
                {
                    Hydration = 100, Contamination = 0.1f
                };
                collectible.SetHydrationProperties(hydrationProperties);
            }
        }
    }
}