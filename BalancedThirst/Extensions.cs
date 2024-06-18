
using System;
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
    
    [Obsolete ("Use GetHydrationProperties(BlockLiquidContainerBase, ItemStack) instead")]
    public static HydrationProperties GetHydrationProperties(this BlockLiquidContainerBase container, ItemSlot slot)
    {
        BtCore.Logger.Warning($"GetHydrationProperties: {container}, {slot}");
        var content = container?.GetContent(slot.Itemstack);
        if (container == null ||
            !container.HasBehavior<DrinkableBehavior>() ||
            content?.Collectible.HasBehavior<DrinkableBehavior>() != true)
            return null;

        var behavior = container.GetBehavior<DrinkableBehavior>();
        return behavior.GetHydrationProperties(slot.Itemstack);
    }
    
    [Obsolete("Use GetHydrationProperties(CollectibleObject, ItemStack) instead")]
    public static HydrationProperties GetHydrationProperties(this CollectibleObject collObj, ItemSlot slot)
    {
        if (!collObj.HasBehavior<DrinkableBehavior>())
            return null;
        var behavior = collObj.GetBehavior<DrinkableBehavior>();
        return behavior.GetHydrationProperties(slot.Itemstack);
    }
    
    public static HydrationProperties GetHydrationProperties(this BlockLiquidContainerBase container, ItemStack itemStack)
    {
        BtCore.Logger.Warning($"GetHydrationProperties: {container}, {itemStack}");
        var content = container?.GetContent(itemStack);
        if (container == null ||
            !container.HasBehavior<DrinkableBehavior>() ||
            content?.Collectible.HasBehavior<DrinkableBehavior>() != true)
            return null;

        var behavior = container.GetBehavior<DrinkableBehavior>();
        return behavior.GetHydrationProperties(itemStack);
    }
    
    public static HydrationProperties GetHydrationProperties(this CollectibleObject collObj, ItemStack itemStack)
    {
        if (!collObj.HasBehavior<DrinkableBehavior>())
            return null;
        var behavior = collObj.GetBehavior<DrinkableBehavior>();
        return behavior.GetHydrationProperties(itemStack);
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

    [Obsolete("Use ReceiveHydration(HydrationProperties) instead")]
    public static void ReceiveHydration(this Entity entity, float hydration, float hydrationLossDelay = 10f)
    {
        if (!entity.HasBehavior<EntityBehaviorThirst>()) return;
        entity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydration, hydrationLossDelay);
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