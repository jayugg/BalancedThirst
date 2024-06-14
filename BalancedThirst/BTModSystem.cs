using BalancedThirst.Hud;
using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace BalancedThirst;

public class BtModSystem : ModSystem
{
    public static ILogger Logger;
    public static string Modid;
    
    public override void Start(ICoreAPI api)
    {
        Modid = Mod.Info.ModID;
        Logger = Mod.Logger;
        api.RegisterBlockBehaviorClass(Modid + ":Drinkable", typeof(BlockBehaviorDrinkable));
        api.RegisterEntityBehaviorClass(Modid + ":thirst", typeof(EntityBehaviorThirst));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Event.OnEntitySpawn += AddEntityBehaviors;
        api.Event.OnEntityLoaded += AddEntityBehaviors;
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
        capi.Gui.RegisterDialog(new GuiDialog[1]
        {
            (GuiDialog) new ThirstBarHudElement(capi)
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
        foreach (Block block in api.World.Blocks)
        {
            if (block?.Code == null)
            {
                continue;
            }
            if (block.Code.ToString().Contains("water"))
            {
                Logger.Warning("Adding drinkable behavior to block: " + block.Code);
                block.BlockBehaviors = block.BlockBehaviors.Append(new BlockBehaviorDrinkable(block));
            }
        }
    }
}