using System.Collections.Generic;
using BalancedThirst.Hud;
using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;


namespace BalancedThirst;

public class BtCore : ModSystem
{
    public static ILogger Logger;
    public static string Modid;
    public Harmony Harmony;
    
    public override void Start(ICoreAPI api)
    {
        Modid = Mod.Info.ModID;
        Logger = Mod.Logger;
        if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
            Harmony = new Harmony(Mod.Info.ModID);
            Harmony.PatchAll(typeof(HarmonyPatches).Assembly);
        }
        api.RegisterBlockBehaviorClass(Modid + ":Drinkable", typeof(BlockBehaviorDrinkable));
        api.RegisterEntityBehaviorClass(Modid + ":thirst", typeof(EntityBehaviorThirst));
        api.RegisterCollectibleBehaviorClass(Modid + ":cDrinkable", typeof(CDrinkableBehavior));
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
    
    public override void Dispose() {
        Harmony?.UnpatchAll(Mod.Info.ModID);
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
                Logger.Warning("Adding cDrinkable behavior to collectible: " + collectible.Code);
                var behavior = new CDrinkableBehavior(collectible);
                collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
                
                HydrationProperties hydrationProperties = new HydrationProperties()
                {
                    Hydration = 100, Contamination = 0.1f
                };
                
                collectible.EnsureAttributesNotNull();
                JToken token = collectible.Attributes.Token;
                token["hydrationprops"] = JToken.FromObject(hydrationProperties);

                // Convert the JToken back to a JsonObject
                JsonObject newAttributes = new JsonObject(token);
                // Assign the new JsonObject back to the collectible
                collectible.Attributes = newAttributes;
            }
        }
        foreach (Block block in api.World.Blocks)
        {
            if (block?.Code == null)
            {
                continue;
            }
            if (block.Code.ToString().Contains("water"))
            {
                //Logger.Warning("Adding drinkable behavior to block: " + block.Code);
                //block.BlockBehaviors = block.BlockBehaviors.Append(new BlockBehaviorDrinkable(block));
            }
        }
    }
    
}