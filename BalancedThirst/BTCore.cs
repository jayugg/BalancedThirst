using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace BalancedThirst;

public class BtCore : ModSystem
{
    public static ILogger Logger;
    public static string Modid;
    public Harmony Harmony;
    
    public static class HydrationStore
    {
        public static Dictionary<AssetLocation, HydrationProperties> hydrationPropDict = new Dictionary<AssetLocation, HydrationProperties>();
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
                HydrationStore.hydrationPropDict.Add(collectible.Code, props);
                Logger.Warning($"HydrationProps for {collectible.Code}: {props.Hydration}");
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
            if (!slot.Itemstack.Collectible.HasBehavior<CDrinkableBehavior>()) return true;
            HydrationProperties hydrationProperties = slot.Itemstack.Collectible.GetBehavior<CDrinkableBehavior>().HydrationProps;
            if (!(byEntity.World is IServerWorldAccessor) || !byEntity.HasBehavior<EntityBehaviorThirst>() || hydrationProperties == null || secondsUsed < 0.949999988079071)
                return true;
            TransitionState transitionState = slot.Itemstack.Collectible.UpdateAndGetTransitionState(byEntity.Api.World, slot, EnumTransitionType.Perish);
            double spoilState = transitionState?.TransitionLevel ?? 0.0;
            float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
            byEntity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProperties.Hydration*num1, hydrationProperties.HydrationLossDelay*num1);
            return true;
        }
    }
    
    // Patch Tooltip
    
    [HarmonyPatch(typeof(CollectibleObject), "GetHeldItemInfo")]
    public static class GetHeldItemInfoPatch
    {
        public static bool Prefix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ItemStack itemstack = inSlot.Itemstack;
            CollectibleObject collObj = itemstack?.Collectible;
            if (collObj?.HasBehavior<CDrinkableBehavior>() ?? false)
            { 
                CDrinkableBehavior drinkableBehavior = collObj?.GetBehavior<CDrinkableBehavior>();
                float hydration = HydrationStore.hydrationPropDict.Get(collObj.Code).Hydration;
                if (drinkableBehavior is { HydrationProps: not null })
                {
                    hydration = drinkableBehavior.HydrationProps.Hydration;
                }
                Logger.Warning($"HydrationProps in GetHeldItemInfo for {itemstack.Collectible.Code}: {hydration}");
                if (hydration == 0) return false;
                string itemDescText = collObj?.GetItemDescText();
                int index;
                int maxDurability = collObj?.GetMaxDurability(itemstack) ?? 0;
                if (maxDurability > 1) dsc.AppendLine(Lang.Get("Durability: {0} / {1}", collObj?.GetRemainingDurability(itemstack), maxDurability));
                EntityPlayer entity = world.Side == EnumAppSide.Client ? (world as IClientWorldAccessor)?.Player.Entity : null;
                float spoilState = collObj?.AppendPerishableInfoText(inSlot, dsc, world) ?? 0;
                FoodNutritionProperties nutritionProperties = collObj?.GetNutritionProperties(world, itemstack, entity);
                if (nutritionProperties != null)
                {
                    float num1 = GlobalConstants.FoodSpoilageSatLossMul(spoilState, itemstack, entity);
                    float num2 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, itemstack, entity);
                    float satiety = nutritionProperties.Satiety;
                    float health = nutritionProperties.Health;
                    if (Math.Abs(health * num2) > 1.0 / 1000.0)
                    {
                        dsc.AppendLine(satiety > 0
                            ? Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp",
                                Math.Round(satiety * (double)num1), hydration * (double)num1, (float)(health * (double)num2))
                            : Lang.Get("When drank: {0} hyd, {1} hp",
                                Math.Round(hydration * (double)num1), (float)(health * (double)num2)));
                    }
                    else
                    {
                        dsc.AppendLine(satiety > 0
                            ? Lang.Get("When eaten: {0} sat, {1} hyd",
                                Math.Round(satiety * (double)num1), hydration * (double)num1)
                            : Lang.Get("When drank: {0} hyd",
                                Math.Round(hydration * (double)num1)));
                    }
                    dsc.AppendLine(Lang.Get("Food Category: {0}", Lang.Get("foodcategory-" + nutritionProperties.FoodCategory.ToString().ToLowerInvariant())));
                }
                if (collObj?.GrindingProps?.GroundStack?.ResolvedItemstack != null)
                    dsc.AppendLine(Lang.Get("When ground: Turns into {0}x {1}", collObj.GrindingProps.GroundStack.ResolvedItemstack.StackSize, collObj.GrindingProps.GroundStack.ResolvedItemstack.GetName()));
                if (collObj?.CrushingProps != null)
                {
                    float num = collObj.CrushingProps.Quantity.avg * collObj.CrushingProps.CrushedStack.ResolvedItemstack.StackSize;
                    dsc.AppendLine(Lang.Get("When pulverized: Turns into {0:0.#}x {1}", num, collObj.CrushingProps.CrushedStack.ResolvedItemstack.GetName()));
                    dsc.AppendLine(Lang.Get("Requires Pulverizer tier: {0}", collObj.CrushingProps.HardnessTier));
                }
                if (collObj?.CombustibleProps != null)
                {
                    string lowerInvariant = collObj.CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
                    if (lowerInvariant == "fire")
                    {
                        dsc.AppendLine(Lang.Get("itemdesc-fireinkiln"));
                    }
                    else
                    {
                        if (collObj.CombustibleProps.BurnTemperature > 0)
                        {
                            dsc.AppendLine(Lang.Get("Burn temperature: {0}°C", collObj.CombustibleProps.BurnTemperature));
                            dsc.AppendLine(Lang.Get("Burn duration: {0}s", collObj.CombustibleProps.BurnDuration));
                        }
                        if (collObj.CombustibleProps.MeltingPoint > 0)
                            dsc.AppendLine(Lang.Get("game:smeltpoint-" + lowerInvariant, collObj.CombustibleProps.MeltingPoint));
                    }
                    if (collObj.CombustibleProps.SmeltedStack?.ResolvedItemstack != null)
                    {
                        int smeltedRatio = collObj.CombustibleProps.SmeltedRatio;
                        int stackSize = collObj.CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize;
                        string str1;
                        str1 = smeltedRatio != 1 ? Lang.Get("game:smeltdesc-" + lowerInvariant + "-plural", smeltedRatio, stackSize, collObj.CombustibleProps.SmeltedStack.ResolvedItemstack.GetName()) : Lang.Get("game:smeltdesc-" + lowerInvariant + "-singular", stackSize, collObj.CombustibleProps.SmeltedStack.ResolvedItemstack.GetName());
                        string str2 = str1;
                        dsc.AppendLine(str2); 
                    }
                }
                CollectibleBehavior[] collectibleBehaviors = collObj?.CollectibleBehaviors;
                for (index = 0; index < collectibleBehaviors.Length; ++index)
                    collectibleBehaviors[index].GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
                if (itemDescText.Length > 0 && dsc.Length > 0) dsc.Append("\n");
                dsc.Append(itemDescText);
                float temperature = collObj.GetTemperature(world, itemstack);
                if (temperature > 20.0) dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int) temperature));
                if (!(collObj.Code != null) || collObj.Code.Domain == "game") return false;
                Mod mod = world.Api.ModLoader.GetMod(collObj.Code.Domain);
                dsc.AppendLine(Lang.Get("Mod: {0}", mod?.Info.Name ?? collObj.Code.Domain));
                return false;
            }
            return true;
        }
    }
    
}