using System.Collections.Generic;
using System.IO;
using BalancedThirst.Hud;
using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace BalancedThirst;

public class BtCore : ModSystem
{
    public static ILogger Logger;
    public static string Modid;
    public Harmony Harmony;
    
    public static class HydrationStore
    {
        public static Dictionary<FoodNutritionProperties, HydrationProperties> hydrationProps = new Dictionary<FoodNutritionProperties, HydrationProperties>();
    }
    
    public override void Start(ICoreAPI api)
    {
        Modid = Mod.Info.ModID;
        Logger = Mod.Logger;
        if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
            Harmony = new Harmony(Mod.Info.ModID);
            Harmony.PatchAll();
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
        foreach (CollectibleObject collectible in api.World.Collectibles)
        {
            if (collectible?.Code == null)
            {
                continue;
            }
            if (collectible.Code.ToString().Contains("drinkitem"))
            {
                Logger.Warning("Adding cDrinkable behavior to collectible: " + collectible.Code);
                var props = new HydrationProperties() { Hydration = 100 };
                var behavior = new CDrinkableBehavior(collectible);
                behavior.Initialize(api, props);
                collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
            }
        }
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
    
    [HarmonyPatch(typeof(CollectibleObject), "tryEatStop")]
    public static class TryEatBeginPatch
    {
        public static bool Prefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            if (slot.Itemstack.Collectible.HasBehavior<CDrinkableBehavior>()) return true;
            HydrationProperties hydrationProperties = slot.Itemstack.Collectible.GetBehavior<CDrinkableBehavior>().HydrationProps;
            if (!(byEntity.World is IServerWorldAccessor) || !byEntity.HasBehavior<EntityBehaviorThirst>() || hydrationProperties == null || (double) secondsUsed < 0.949999988079071)
            return true;
            TransitionState transitionState = slot.Itemstack.Collectible.UpdateAndGetTransitionState(byEntity.Api.World, slot, EnumTransitionType.Perish);
            double spoilState = transitionState != null ? (double) transitionState.TransitionLevel : 0.0;
            float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
            byEntity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProperties.Hydration, hydrationProperties.HydrationLossDelay);
            return true;
        }
    }
    
    [HarmonyPatch(typeof(FoodNutritionProperties), MethodType.Constructor)]
    public static class FoodNutritionPropertiesPatch
    {
        static void Postfix(FoodNutritionProperties __instance)
        {
            // Add a new HydrationProperties for this instance
            HydrationStore.hydrationProps[__instance] = new HydrationProperties();
        }
    }
    
    [HarmonyPatch(typeof(FoodNutritionProperties), "Clone")]
    public static class FoodNutritionPropertiesClonePatch
    {
        static void Postfix(ref FoodNutritionProperties __result, FoodNutritionProperties __instance)
        {
            // Get the original HydrationProperties
            HydrationProperties originalHydrationProps = BtCore.HydrationStore.hydrationProps[__instance];

            // Clone the HydrationProperties
            HydrationProperties clonedHydrationProps = originalHydrationProps.Clone();

            // Add the cloned HydrationProperties to the hydrationProps dictionary for the cloned FoodNutritionProperties
            BtCore.HydrationStore.hydrationProps[__result] = clonedHydrationProps;
        }
    }
    
    
}