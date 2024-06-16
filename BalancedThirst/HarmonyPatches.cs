using System;
using System.Linq;
using System.Text;
using BalancedThirst.ModBehavior;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BalancedThirst;

public class HarmonyPatches
{
    
    [HarmonyPatch(typeof(CollectibleObject), "tryEatBegin")]
    public static class TryEatBeginPatch
    {
        public static void Postfix(
            ItemSlot slot,
            EntityAgent byEntity,
            ref EnumHandHandling handling,
            string eatSound = "eat",
            int eatSoundRepeats = 1)
        {
            BtCore.Logger.Warning("TryEatBeginPatch");
            if (!slot.Itemstack.Collectible.HasBehavior<CDrinkableBehavior>()) return;
            var behavior = slot.Itemstack.Collectible.GetBehavior<CDrinkableBehavior>();
            BtCore.Logger.Warning("TryEatBeginPatch2");
            HydrationProperties hydrationProperties = behavior.GetHydrationProperties(slot.Itemstack);
            BtCore.Logger.Warning(hydrationProperties.Hydration.ToString());
            if (slot.Empty)
                return;
            BtCore.Logger.Warning("TryDrinkBegin");
            byEntity.World.RegisterCallback(_ => behavior.PlayDrinkSound(byEntity, eatSoundRepeats), 500);
            byEntity.AnimManager?.StartAnimation("eat");
            BtCore.Logger.Warning("PreventDefault");
            handling = EnumHandHandling.PreventDefault;
        }
    }
    
    [HarmonyPatch(typeof(CollectibleObject), "tryEatStep")]
    public static class TryEatStepPatch
    {
        public static void Postfix(
            ref bool __result,
            float secondsUsed,
            ItemSlot slot,
            EntityAgent byEntity,
            ItemStack spawnParticleStack = null)
        {
            BtCore.Logger.Warning("TryEatStepPatch");
            if (!slot.Itemstack.Collectible.HasBehavior<CDrinkableBehavior>()) return;
            var behavior = slot.Itemstack.Collectible.GetBehavior<CDrinkableBehavior>();
            HydrationProperties hydrationProperties = behavior.GetHydrationProperties(slot.Itemstack);
            if (hydrationProperties == null) return;
            Vec3d xyz = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
            xyz.X += byEntity.LocalEyePos.X;
            xyz.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
            xyz.Z += byEntity.LocalEyePos.Z;
            if (secondsUsed > 0.5 && (int) (30.0 * secondsUsed) % 7 == 1)
                byEntity.World.SpawnCubeParticles(xyz, spawnParticleStack ?? slot.Itemstack, 0.3f, 4, 0.5f, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
            if (!(byEntity.World is IClientWorldAccessor))
                __result = true;
            ModelTransform modelTransform = new ModelTransform();
            modelTransform.EnsureDefaultValues();
            modelTransform.Origin.Set(0.0f, 0.0f, 0.0f);
            if (secondsUsed > 0.5)
                modelTransform.Translation.Y = Math.Min(0.02f, GameMath.Sin(20f * secondsUsed) / 10f);
            modelTransform.Translation.X -= Math.Min(1f, (float) (secondsUsed * 4.0 * 1.5700000524520874));
            modelTransform.Translation.Y -= Math.Min(0.05f, secondsUsed * 2f);
            modelTransform.Rotation.X += Math.Min(30f, secondsUsed * 350f);
            modelTransform.Rotation.Y += Math.Min(80f, secondsUsed * 350f);
            __result = secondsUsed <= 1.0;
        }
    }
    
    [HarmonyPatch(typeof(CollectibleObject), "tryEatStop")]
    public static class TryEatStopPatch
    {
        public static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            BtCore.Logger.Warning("TryEatStopPatch");
            if (!slot.Itemstack.Collectible.HasBehavior<CDrinkableBehavior>()) return;
            var behavior = slot.Itemstack.Collectible.GetBehavior<CDrinkableBehavior>();
            HydrationProperties hydrationProperties = behavior.GetHydrationProperties(slot.Itemstack);
            if (!(byEntity.World is IServerWorldAccessor) || !byEntity.HasBehavior<EntityBehaviorThirst>() || hydrationProperties == null || secondsUsed < 0.949999988079071)
                return;
            TransitionState transitionState = slot.Itemstack.Collectible.UpdateAndGetTransitionState(byEntity.Api.World, slot, EnumTransitionType.Perish);
            double spoilState = transitionState?.TransitionLevel ?? 0.0;
            float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
            byEntity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProperties.Hydration*num1, hydrationProperties.HydrationLossDelay*num1);
        }
    }
    
    [HarmonyPatch(typeof(BlockLiquidContainerBase), "tryEatStop")]
    public static class ContainerTryEastStopPatch
    {
        public static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            var container = (BlockLiquidContainerBase) slot.Itemstack.Collectible;
            if (!container.GetContent(slot.Itemstack).Collectible.HasBehavior<CDrinkableBehavior>() || !byEntity.HasBehavior<EntityBehaviorThirst>()) return;
            var behavior = container.GetBehavior<CDrinkableBehavior>();
            HydrationProperties hydrationProperties = behavior.GetHydrationProperties(slot.Itemstack);
            if (!(byEntity.World is IServerWorldAccessor) || hydrationProperties == null || secondsUsed < 0.949999988079071) return;
            var hydration = hydrationProperties.Hydration;
            var hydrationLossDelay = hydrationProperties.HydrationLossDelay;
            float val1 = 1f;
            float currentLitres = container.GetCurrentLitres(slot.Itemstack);
            float val2 = currentLitres * slot.StackSize;
            if (currentLitres > (double) val1)
            {
                hydration /= currentLitres;
                hydrationLossDelay /= currentLitres;
            }
            TransitionState transitionState = container.UpdateAndGetTransitionState(byEntity.World, slot, EnumTransitionType.Perish);
            double spoilState = transitionState?.TransitionLevel ?? 0.0;
            float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
            byEntity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydration * num1, hydrationLossDelay * num1);
            IPlayer player = null;
            if (byEntity is EntityPlayer entityPlayer) player = entityPlayer.World.PlayerByUid(entityPlayer.PlayerUID);
            float num3 = Math.Min(val1, val2);
            container.TryTakeLiquid(slot.Itemstack, num3 / slot.Itemstack.StackSize);
            slot.MarkDirty();
            player?.InventoryManager.BroadcastHotbarSlot();
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
                CDrinkableBehavior behavior = collObj?.GetBehavior<CDrinkableBehavior>();
                if (behavior == null) return true;
                HydrationProperties hydrationProperties = behavior.GetHydrationProperties(itemstack);
                if (hydrationProperties == null) return true;
                var hydration = hydrationProperties.Hydration;
                BtCore.Logger.Warning($"HydrationProps in GetHeldItemInfo for {itemstack.Collectible.Code}: {hydration}");
                if (hydrationProperties.Hydration == 0) return true;
                string itemDescText = collObj?.GetItemDescText();
                int index;
                int maxDurability = collObj?.GetMaxDurability(itemstack) ?? 0;
                if (maxDurability > 1) dsc.AppendLine(Lang.Get("Durability: {0} / {1}", collObj?.GetRemainingDurability(itemstack), maxDurability));
                EntityPlayer entity = world.Side == EnumAppSide.Client ? (world as IClientWorldAccessor)?.Player.Entity : null;
                float spoilState = collObj?.AppendPerishableInfoText(inSlot, dsc, world) ?? 0;
                FoodNutritionProperties nutritionProperties = collObj?.GetNutritionProperties(world, itemstack, entity);
                float num1 = GlobalConstants.FoodSpoilageSatLossMul(spoilState, itemstack, entity);
                float num2 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, itemstack, entity);
                if (nutritionProperties != null)
                {
                    float satiety = nutritionProperties.Satiety;
                    float health = nutritionProperties.Health;
                    if (Math.Abs(health * num2) > 1.0 / 1000.0)
                    {
                        dsc.AppendLine(Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp",
                            Math.Round(satiety * (double)num1), hydration * (double)num1, (float)(health * (double)num2)));
                    }
                    else
                    {
                        dsc.AppendLine(Lang.Get("When eaten: {0} sat, {1} hyd",
                            Math.Round(satiety * (double)num1), hydration * (double)num1));
                    }
                    dsc.AppendLine(Lang.Get("Food Category: {0}", Lang.Get("foodcategory-" + nutritionProperties.FoodCategory.ToString().ToLowerInvariant())));
                }
                else
                {
                    dsc.AppendLine(Lang.Get("When drank: {0} hyd", hydration * (double)num1));
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