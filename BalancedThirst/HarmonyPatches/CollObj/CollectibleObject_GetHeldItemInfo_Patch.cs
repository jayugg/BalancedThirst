using System;
using System.Text;
using BalancedThirst.ModBehavior;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_GetHeldItemInfo_Patch
{
        public static bool Prefix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ItemStack itemstack = inSlot.Itemstack;
            CollectibleObject collObj = itemstack?.Collectible;
            string itemDescText = collObj?.GetItemDescText();
            int index;
            EntityPlayer entity = world.Side == EnumAppSide.Client ? (world as IClientWorldAccessor)?.Player.Entity : null;
            HydrationProperties hydrationProperties = collObj?.GetHydrationProperties(world, itemstack, entity);
            if (hydrationProperties == null) return true;
            var hydration = hydrationProperties.Hydration;
            if (hydrationProperties.Hydration == 0) return true;
            int maxDurability = collObj?.GetMaxDurability(itemstack) ?? 0;
            if (maxDurability > 1) dsc.AppendLine(Lang.Get("Durability: {0} / {1}", collObj?.GetRemainingDurability(itemstack), maxDurability));
            float spoilState = collObj.AppendPerishableInfoText(inSlot, dsc, world);
            FoodNutritionProperties nutritionProperties = collObj.GetNutritionProperties(world, itemstack, entity);
            float num1 = GlobalConstants.FoodSpoilageSatLossMul(spoilState, itemstack, entity);
            float num2 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, itemstack, entity);
            if (nutritionProperties != null)
            {
                float satiety = nutritionProperties.Satiety;
                float health = nutritionProperties.Health;
                if (Math.Abs(health * num2) > 1.0 / 1000.0)
                {
                    dsc.AppendLine(Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp",
                        Math.Round(satiety * (double)num1), Math.Round(hydration * (double)num1), (float)(health * (double)num2)));
                }
                else
                {
                    dsc.AppendLine(Lang.Get("When eaten: {0} sat, {1} hyd",
                        Math.Round(satiety * (double)num1), Math.Round(hydration * (double)num1)));
                }
                dsc.AppendLine(Lang.Get("Food Category: {0}", Lang.Get("foodcategory-" + nutritionProperties.FoodCategory.ToString().ToLowerInvariant())));
            }
            else
            {
                dsc.AppendLine(Lang.Get("When drank: {0} hyd", Math.Round(hydration * (double)num1)));
            }
            if ((hydrationProperties.Purity != EnumPurityLevel.Okay && hydrationProperties.Purity != EnumPurityLevel.Pure) ||
                (hydrationProperties.Purity == EnumPurityLevel.Pure && itemstack.Collectible.Code.ToString().Contains("pure")))
            {
                dsc.AppendLine(Lang.Get(BtCore.Modid+$":purity-{hydrationProperties.Purity}"));
            }
            if (collObj is BlockLiquidContainerBase && hydration > 0) return false;
            if (collObj.GrindingProps?.GroundStack?.ResolvedItemstack != null)
                dsc.AppendLine(Lang.Get("When ground: Turns into {0}x {1}", collObj.GrindingProps.GroundStack.ResolvedItemstack.StackSize, collObj.GrindingProps.GroundStack.ResolvedItemstack.GetName()));
            if (collObj.CrushingProps != null)
            {
                float num = collObj.CrushingProps.Quantity.avg * collObj.CrushingProps.CrushedStack.ResolvedItemstack.StackSize;
                dsc.AppendLine(Lang.Get("When pulverized: Turns into {0:0.#}x {1}", num, collObj.CrushingProps.CrushedStack.ResolvedItemstack.GetName()));
                dsc.AppendLine(Lang.Get("Requires Pulverizer tier: {0}", collObj.CrushingProps.HardnessTier));
            }
            if (collObj.CombustibleProps != null)
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
            CollectibleBehavior[] collectibleBehaviors = collObj.CollectibleBehaviors;
            for (index = 0; index < collectibleBehaviors.Length; ++index)
                collectibleBehaviors[index].GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            if (itemDescText.Length > 0 && dsc.Length > 0) dsc.Append("\n");
            dsc.Append(itemDescText);
            float temperature = collObj.GetTemperature(world, itemstack);
            if (temperature > 20.0) dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int) temperature));
            if (collObj.IsWaterContainer()) dsc.AppendLine(Lang.Get("Stored water perish speed: {0}", collObj.GetTransitionRateMul(world, inSlot, EnumTransitionType.Perish).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (!(collObj.Code != null) || collObj.Code.Domain == "game") return false;
            Mod mod = world.Api.ModLoader.GetMod(collObj.Code.Domain);
            dsc.AppendLine(Lang.Get("Mod: {0}", mod?.Info.Name ?? collObj.Code.Domain));
            return false;
        }
}