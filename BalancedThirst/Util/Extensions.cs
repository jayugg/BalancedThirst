using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
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
        if (collObj is BlockMeal meal)
        {
            ItemStack[] contentStacks = meal.GetNonEmptyContents(world, itemStack);
            var quantityServings = meal.GetQuantityServings(world, itemStack);
            return contentStacks?.GetHydrationProperties(world, byEntity) * (quantityServings == 0 ? 1 : quantityServings);
        }
        
        if (collObj is BlockCookedContainer)
        {
            ItemStack[] contentStacks = (itemStack.Collectible as BlockCookedContainer)?.GetNonEmptyContents(world, itemStack);
            return contentStacks?.GetHydrationProperties(world, byEntity);
        }
        
        if (collObj is BlockCrock)
        {
            ItemStack[] contentStacks = (itemStack.Collectible as BlockCrock)?.GetNonEmptyContents(world, itemStack);
            return contentStacks?.GetHydrationProperties(world, byEntity);
        }
        
        if (collObj.HasBehavior<HydratingFoodBehavior>())
        {
            var behavior = collObj.GetBehavior<HydratingFoodBehavior>();
            return behavior?.GetHydrationProperties(world, itemStack, byEntity);
        }
        
        if (collObj.HasBehavior<DrinkableBehavior>())
        {
            var behavior = collObj.GetBehavior<DrinkableBehavior>();
            return behavior?.GetHydrationProperties(world, itemStack, byEntity);
        }

        if (collObj.HasBehavior<WaterContainerBehavior>())
        {
            var behavior = collObj.GetBehavior<WaterContainerBehavior>();
            return behavior?.GetHydrationProperties(world, itemStack, byEntity);
        }

        return null;
    }
    
    public static HydrationProperties GetHydrationProperties(this ItemStack[] contentStacks, IWorldAccessor world, Entity byEntity)
    {
        if (contentStacks == null || contentStacks.Length == 0 || world == null) return null;

        HydrationProperties totalProps = null;
        foreach (ItemStack contentStack in contentStacks)
        {
            if (contentStack == null) continue;
            if (contentStack.Collectible == null)
            {
                continue;
            }

            var currentProps = contentStack.Collectible.GetHydrationProperties(world, contentStack, byEntity);
            if (currentProps == null) continue;
            if (totalProps == null) totalProps = currentProps;
            else totalProps += currentProps;
        }
        return totalProps;
    }
    
    public static HydrationProperties GetHydrationPropsPerLitre(
        this BlockLiquidContainerBase container,
        IWorldAccessor world,
        ItemStack itemStack,
        Entity byEntity)
    {
        if (container.GetContent(itemStack) == null) return null;
        return container.GetContent(itemStack).Collectible.HasBehavior<DrinkableBehavior>() ?
            DrinkableBehavior.GetContentHydrationPropsPerLitre(container, itemStack) :
            HydrationProperties.FromNutrition(container.GetContent(itemStack).Collectible.GetNutritionProperties(world, itemStack, byEntity));
    }
    
    public static HydrationProperties GetBlockHydrationProperties(this Block block)
    {
        block.EnsureAttributesNotNull();
        JToken token = block.Attributes.Token;
        HydrationProperties hydrationProperties = token["hydrationProps"]?.ToObject<HydrationProperties>();
        return hydrationProperties;
    }
    
    public static FoodNutritionProperties GetNutritionProperties(this CollectibleObject collectible)
    {
        collectible.EnsureAttributesNotNull();
        JToken token = collectible.Attributes.Token;
        var waterTightContainerProps = token["waterTightContainerProps"];
        if (waterTightContainerProps == null)
        {
            FoodNutritionProperties nutritionProperties = token["nutritionProps"]?.ToObject<FoodNutritionProperties>();
            if (nutritionProperties == null)
            {
                Dictionary<string, FoodNutritionProperties> nutritionPropsByType = token["nutritionPropsByType"]?.ToObject<Dictionary<string, FoodNutritionProperties>>();
                if (nutritionPropsByType != null)
                {
                    nutritionProperties = nutritionPropsByType.Where(keyVal => collectible.MyWildCardMatch(keyVal.Key))
                        .OrderByDescending(keyVal => keyVal.Key.Length)
                        .FirstOrDefault()
                        .Value;
                }
            }
            return nutritionProperties;
        }
        else
        {
            FoodNutritionProperties nutritionProperties = token["nutritionPropsPerLitre"]?.ToObject<FoodNutritionProperties>();
            return nutritionProperties;
        }
    }

    public static bool IsRiverBlock(this BlockSelection blockSel, IWorldAccessor world)
    {
        var pos = blockSel.Position;
        return pos != null && pos.IsRiverBlock(world);
    }
    
    public static bool IsRiverBlock(this BlockPos pos, IWorldAccessor world)
    {
        IWorldChunk chunk = world.BlockAccessor.GetChunk(pos.X / 32, 0, pos.Z / 32);
        float[] moddata = chunk?.GetModdata<float[]>("flowVectors");
        if (moddata == null) return false;
        var localX = pos.X % 32;
        var localZ = pos.Z % 32;
        return moddata[localZ * 32 + localX] != 0;
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
    
    public static void AddHydratingFoodBehavior(this CollectibleObject collectible)
    {
        var behavior = new HydratingFoodBehavior(collectible);
        collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
    }
    
    public static void AddContainerBehavior(this CollectibleObject collectible)
    {
        var behavior = new WaterContainerBehavior(collectible);
        collectible.CollectibleBehaviors = collectible.CollectibleBehaviors.Append(behavior);
    }
    
    public static void AddBehavior<T>(this CollectibleObject collectible) where T : CollectibleBehavior
    {
        var behavior = (T) Activator.CreateInstance(typeof(T), collectible);
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
        FoodNutritionProperties nutritionProperties = new() { Satiety = 0, FoodCategory = EnumFoodCategory.NoNutrition };
        var waterTightContainerProps = token["waterTightContainerProps"];
        if (waterTightContainerProps == null)
        {
            token["nutritionProps"] ??= JToken.FromObject(nutritionProperties);
        }
        else
        {
            waterTightContainerProps["nutritionPropsPerLitre"] ??= JToken.FromObject(nutritionProperties);
        }
        JsonObject newAttributes = new JsonObject(token);
        collectible.Attributes = newAttributes;
    }

    public static float GetLitres(this ItemStack stack)
    {
        if (stack == null) return 0;
        var collectible = stack.Collectible;
        collectible?.EnsureAttributesNotNull();
        JToken token = collectible?.Attributes.Token;
        if (token == null) return 0;
        var waterTightContainerProps = token["waterTightContainerProps"];
        if (waterTightContainerProps == null)
        {
            BtCore.Logger.Warning("[GetLitres] Cannot get litres for collectible without waterTightContainerProps");
            return 0;
        }

        var itemsPerLitreProps = waterTightContainerProps["itemsPerLitre"];
        var itemsPerLitre = 1f;
        if (itemsPerLitreProps == null)
        {
            BtCore.Logger.Warning("[GetLitres] Cannot get litres for collectible without itemsPerLitre, defaulting to 1");
        }
        else
        {
            itemsPerLitre *= itemsPerLitreProps.Value<float>();
        }
        return stack.StackSize / itemsPerLitre ;
    }
    
    // Should only be used on the server side!
    public static bool IsWaterPortion(this CollectibleObject collectible, EnumAppSide side)
    {
        return ConfigSystem.ConfigServer.WaterPortions.Any(collectible.MyWildCardMatch);
    }

    public static bool IsWaterPortion(this CollectibleObject collectible)
    {
        return DrinkableBehavior.IsWaterPortion(collectible);
    }
    
    // Should only be used on the server side!
    public static bool IsWaterContainer(this CollectibleObject collectible, EnumAppSide side)
    {
        return ConfigSystem.ConfigServer.WaterContainers.Keys.Any(collectible.MyWildCardMatch);
    }
    public static bool IsWaterContainer(this CollectibleObject collectible)
    {
        var mult = WaterContainerBehavior.GetTransitionRateMul(collectible, EnumTransitionType.Perish);
        return Math.Abs(mult - 1) > 0.0001 && mult != 0;
    }
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
        entity.GetBehavior<EntityBehaviorBladder>().ReceiveFluid(capacity);
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

    [Obsolete("Use Raycast.RaycastForFluidBlocks instead")]
    public static BlockSelection GetLookLiquidBlockSelection(this IClientPlayer clientPlayer)
    {
        var api = clientPlayer.Entity?.World.Api;
        if (api is not ICoreClientAPI capi || clientPlayer.Entity == null) return null;
        var game = capi.GetField<ClientMain>("game");
        if (game == null) return null;
        BlockFilter bfilter = (_, block) => block is not { RenderPass: EnumChunkRenderPass.Meta };
        EntityFilter efilter = (entity) => entity.IsInteractable;
        bool liquidSelectable = game.LiquidSelectable;
        game.forceLiquidSelectable = true;
        var blockSel = clientPlayer.Entity.BlockSelection?.Clone();
        var entitySel = clientPlayer.Entity.EntitySelection?.Clone();
        if (!game.MouseGrabbed)
        {
            var pickingRayUtil = game.GetField<PickingRayUtil>("pickingRayUtil");
            if (pickingRayUtil == null) return null;
            Ray mouseCoordinates = pickingRayUtil.GetPickingRayByMouseCoordinates(game);
            if (mouseCoordinates == null)
            {
                game.forceLiquidSelectable = liquidSelectable;
                return null;
            }
            game.RayTraceForSelection(mouseCoordinates, ref blockSel, ref entitySel, bfilter, efilter);
        }
        else
            game.RayTraceForSelection(clientPlayer, ref blockSel, ref entitySel, bfilter, efilter);
        if (blockSel == null) return null;
        game.forceLiquidSelectable = liquidSelectable;
        return blockSel;
    }
    
    public static bool IsLookingAtInteractable(this IClientPlayer clientPlayer)
    {
        if (clientPlayer.CurrentBlockSelection?.Block?.HasBehavior<BlockBehaviorBlockEntityInteract>() ?? 
            clientPlayer.CurrentBlockSelection?.Block is BlockGroundStorage) return true;
        if (clientPlayer.Entity.EntitySelection?.Entity?.IsInteractable ?? false) return true;
        return false;
    }

    public static bool IsLookingAtDrinkableBlock(this IClientPlayer clientPlayer)
    {
        var liquidSel = Raycast.RayCastForFluidBlocks(clientPlayer);
        return liquidSel?.Block?.GetBlockHydrationProperties() != null;
    }
    
    public static string Localize(this string input, params object[] args)
    {
        return Lang.Get(input, args);
    }
    
    public static void IngameError(this IPlayer byPlayer, object sender, string errorCode, string text)
    {
        (byPlayer.Entity.World.Api as ICoreClientAPI)?.TriggerIngameError(sender, errorCode, text);
    }

    public static bool IsBladderOverloaded(this IPlayer player)
    {
        var bladderTree = player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":bladder");
        if (bladderTree == null) return false;

        float? currentLevel = bladderTree.TryGetFloat("currentlevel");
        float? capacity = bladderTree.TryGetFloat("capacity");

        if (!currentLevel.HasValue || !capacity.HasValue) return false;
        return currentLevel > capacity;
    }
    
    // From DanaTweaks
    public static void CoolWithWater(this BlockEntityToolMold mold)
    {
        ItemStack stack = mold.MetalContent;
        if (stack != null)
        {
            // No clue why this doesn't work either
            //BtCore.Logger.Warning("Temperature: " + stack.Collectible.GetTemperature(mold.Api.World, stack));
            float temperature = stack.Collectible.GetTemperature(mold.Api.World, stack);
            stack.Collectible.SetTemperature(mold.Api.World, stack, Math.Max(20f, temperature - ConfigSystem.ConfigServer.UrineDrainRate));
            //BtCore.Logger.Warning("Temperature: " + stack.Collectible.GetTemperature(mold.Api.World, stack));
            mold.Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish2"), mold.Pos.X, mold.Pos.Y, mold.Pos.Z);
        }
    }
    
    // From DanaTweaks
    public static void CoolWithWater(this BlockEntityIngotMold mold)
    {
        ItemStack rightStack = mold.ContentsRight;
        ItemStack leftStack = mold.ContentsLeft;
        if (rightStack != null)
        {
            //BtCore.Logger.Warning("Temperature: " + rightStack.Collectible.GetTemperature(mold.Api.World, rightStack));
            float temperature = rightStack.Collectible.GetTemperature(mold.Api.World, rightStack);
            rightStack.Collectible.SetTemperature(mold.Api.World, rightStack, Math.Max(20f, temperature - ConfigSystem.ConfigServer.UrineDrainRate));
            mold.Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish1"), mold.Pos.X, mold.Pos.Y, mold.Pos.Z);
            //BtCore.Logger.Warning("Temperature: " + rightStack.Collectible.GetTemperature(mold.Api.World, rightStack));
        }
        if (leftStack != null)
        {
            //BtCore.Logger.Warning("Temperature: " + leftStack.Collectible.GetTemperature(mold.Api.World, rightStack));
            float temperature = leftStack.Collectible.GetTemperature(mold.Api.World, leftStack);
            leftStack.Collectible.SetTemperature(mold.Api.World, leftStack, Math.Max(20f, temperature - ConfigSystem.ConfigServer.UrineDrainRate));
            mold.Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish1"), mold.Pos.X, mold.Pos.Y, mold.Pos.Z);
            //BtCore.Logger.Warning("Temperature: " + leftStack.Collectible.GetTemperature(mold.Api.World, rightStack));
        }
    }
    
    public static bool TryGetBeBehavior<T>(this IBlockAccessor blockAccessor, BlockPos pos, out T behavior) where T : BlockEntityBehavior
    {
        behavior = blockAccessor.GetBlockEntity(pos)?.GetBehavior<T>();
        return behavior != null;
    }
    
    public static bool IsHydrationMaxed(this Entity entity)
    {
        var thirstTree = entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":thirst");
        if (thirstTree == null) return false;

        float? currentHydration = thirstTree.TryGetFloat("currenthydration");
        float? maxHydration = thirstTree.TryGetFloat("maxhydration");

        if (!currentHydration.HasValue || !maxHydration.HasValue) return false;
        return currentHydration >= maxHydration;
    }
    
    public static bool IsHeadInWater(this IServerPlayer player)
    {
        var headPos = player.Entity.ServerPos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
        var headBlockPos = new BlockPos((int)headPos.X, (int)headPos.Y, (int)headPos.Z, (int)headPos.Y/32768);
        var block = player.Entity.World.BlockAccessor.GetBlock(headBlockPos);
        return block.BlockMaterial == EnumBlockMaterial.Liquid;
    }

    public static int GetToolMode(this CollectibleObject collObj, ItemSlot itemslot, EntityPlayer player, BlockPos pos)
    {
        return collObj.GetToolMode(itemslot, player.Player, new BlockSelection() { Position = pos });
    }

}