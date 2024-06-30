using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BalancedThirst.ModBehavior;
using BalancedThirst.ModBlockBehavior;
using BalancedThirst.Thirst;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace BalancedThirst.Util;

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
    
    public static void AddDrinkableBehavior(this CollectibleObject collectible)
    {
        var behavior = new DrinkableBehavior(collectible);
        collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
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
    
    public static void ReceiveCapacity(this Entity entity, float capacity)
    {
        if (!entity.HasBehavior<EntityBehaviorBladder>()) return;
        entity.GetBehavior<EntityBehaviorBladder>().ReceiveCapacity(capacity);
    }
    
    public static void IncreaseNutrients(this BlockEntityFarmland be, Dictionary<EnumSoilNutrient, float> addNutrients)
    {
        var nutrientInfo = typeof(BlockEntityFarmland).GetField("nutrients",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (nutrientInfo?.GetValue(be) is float[] nutrients)
        {
            foreach (var pair in addNutrients)
            {
                switch (pair.Key)
                {
                    case EnumSoilNutrient.N:
                        nutrients[0] += pair.Value;
                        break;
                    case EnumSoilNutrient.P:
                        nutrients[1] += pair.Value;
                        break;
                    case EnumSoilNutrient.K:
                        nutrients[2] += pair.Value;
                        break;
                }
            }
            be.MarkDirty(true);
        }
    }

    public static bool IsLookingAtDrinkableBlock(this IClientPlayer clientPlayer)
    {
        var player = clientPlayer.Entity;
        var world = player.World;
        var blockSel = player.BlockSelection;
        var selPos = blockSel?.Position;
        var selFace = player.BlockSelection?.Face;
        var waterPos = selPos?.AddCopy(selFace);
        if (blockSel == null)
        {
            blockSel = Raycast.RayCastForFluidBlocks(player.Player);
            waterPos = blockSel?.Position;
            if (waterPos == null)
            {
                return false;
            }
        }
        return world.BlockAccessor?.GetBlock(waterPos)?.GetBlockHydrationProperties() != null;
    }

    public static bool IsBladderAlmostFull(this IClientPlayer clientPlayer)
    {
        var bladderTree = clientPlayer.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":bladder");
        if (bladderTree == null) return false;

        float? currentLevel = bladderTree.TryGetFloat("currentlevel");
        float? capacity = bladderTree.TryGetFloat("capacity");

        if (!currentLevel.HasValue || !capacity.HasValue) return false;
        return currentLevel > capacity * 0.8;
    }
    
    public static string Localize(this string input, params object[] args)
    {
        return Lang.Get(input, args);
    }
    
}