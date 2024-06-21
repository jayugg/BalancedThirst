using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
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
    
    private static readonly List<string> WaterPortions = new List<string>
    {
        "game:waterportion",
        "game:boilingwaterportion",
        "game:saltwaterportion",
        BtCore.Modid + ":purewaterportion"
    };

    public static bool IsWaterPortion(this CollectibleObject collectible)
    {
        return WaterPortions.Contains(collectible.Code.FirstCodePart());
    }
    
    public static bool IsLiquidSourceBlock(this Block b) => b.LiquidLevel == 7;
    public static bool IsSameLiquid(this Block b, Block o) => b.LiquidCode == o.LiquidCode;
}