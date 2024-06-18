using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

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
        BtCore.Logger.Warning("Getting hydration properties for " + collObj.Code.Path);
        if (!collObj.HasBehavior<DrinkableBehavior>())
        {
            BtCore.Logger.Warning("No DrinkableBehavior found for " + collObj.Code.Path + "trying NutritionProperties");
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
    
    public static void ReceiveHydration(this Entity entity, HydrationProperties hydrationProperties)
    {
        if (!entity.HasBehavior<EntityBehaviorThirst>()) return;
        entity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProperties);
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
}