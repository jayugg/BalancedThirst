using System;
using System.Linq;
using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BalancedThirst;

public static class Extensions
{
    public static void EnsureAttributesNotNull(this CollectibleObject obj) => obj.Attributes ??= new JsonObject(new JObject());
    
    public static HydrationProperties GetHydrationProperties(
        this CollectibleObject collObj,
        IWorldAccessor world,
        ItemStack itemStack,
        Entity byEntity)
    {
        if (!collObj.HasBehavior<DrinkableBehavior>())
        {
            if (collObj is BlockLiquidContainerBase container &&
                container.GetContent(itemStack) != null &&
                container.GetContent(itemStack).Collectible.HasBehavior<DrinkableBehavior>())
            {
                return DrinkableBehavior.GetContainerHydrationProperties(container, itemStack);
            }
            return HydrationProperties.FromNutrition(collObj.GetNutritionProperties(world, itemStack, byEntity));
        }
        var behavior = collObj.GetBehavior<DrinkableBehavior>();
        return behavior.GetHydrationProperties(itemStack);
    }

    public static HydrationProperties GetHydrationProperties(
        this CollectibleObject collObj,
        ItemStack itemStack,
        Entity byEntity)
    {
        return GetHydrationProperties(collObj, byEntity.World, itemStack, byEntity);
    }

    public static HydrationProperties GetHydrationProperties(this Block block, IWorldAccessor world, Entity byEntity)
    {
        if (block != null && world != null)
        {
            var drinkableBehavior = block.GetBehavior<BlockBehaviorDrinkable>();
            if (drinkableBehavior != null)
            {
                return drinkableBehavior.GetHydrationProperties(world, byEntity);
            }
        }
        return null;
    }
    
    public static HydrationProperties GetBlockHydrationProperties(this Block block)
    {
        block.EnsureAttributesNotNull();
        JToken token = block.Attributes.Token;
        HydrationProperties hydrationProperties = token["hydrationProps"]?.ToObject<HydrationProperties>();
        BtCore.Logger.Warning($"Block {block.Code} hydration properties: {hydrationProperties}");
        return hydrationProperties;
    }
    
    public static void ReceiveHydration(this Entity entity, HydrationProperties hydrationProperties)
    {
        if (!entity.HasBehavior<EntityBehaviorThirst>()) return;
        entity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProperties);
    }
    
    public static void SetAttribute(this CollectibleObject collectible, string name, object obj)
    {
        collectible.EnsureAttributesNotNull();
        JToken token = collectible.Attributes.Token;
        token[name] = JToken.FromObject(obj);
        // Convert the JToken back to a JsonObject
        JsonObject newAttributes = new JsonObject(token);
        // Assign the new JsonObject back to the collectible
        collectible.Attributes = newAttributes;
    }
    
    public static void SetHydrationProperties(this CollectibleObject collectible, HydrationProperties hydrationProperties)
    {
        collectible.EnsureAttributesNotNull();
        JToken token = collectible.Attributes.Token;
        token["hydrationProps"] = JToken.FromObject(hydrationProperties);
        // Convert the JToken back to a JsonObject
        JsonObject newAttributes = new JsonObject(token);
        // Assign the new JsonObject back to the collectible
        collectible.Attributes = newAttributes;
    }
    
    public static bool IsHeatableLiquidContainer(this CollectibleObject collectible)
    {
        return BtCore.ConfigServer.HeatableLiquidContainers.Any(collectible.MyWildCardMatch);
    }

    public static bool IsWaterPortion(this CollectibleObject collectible) { return BtCore.ConfigServer.WaterPortions.Any(collectible.MyWildCardMatch); }
    public static bool IsWaterContainer(this CollectibleObject collectible) { return BtCore.ConfigServer.WaterContainers.Keys.Any(collectible.MyWildCardMatch); }
    public static bool IsLiquidSourceBlock(this Block b) => b.LiquidLevel == 7;
    public static bool IsSameLiquid(this Block b, Block o) => b.LiquidCode == o.LiquidCode;
    public static Vec3d NoY(this Vec3d vec) => new Vec3d(vec.X, 0, vec.Z);
    public static Vec3d ClampY(this Vec3d vec, int value = 1) => new Vec3d(vec.X, Math.Clamp(vec.Y, -value, value), vec.Z);
    public static bool MyWildCardMatch(this CollectibleObject collectible, string regex)
    {
        return WildcardUtil.Match(regex, collectible.Code.ToString());
    }
    
}