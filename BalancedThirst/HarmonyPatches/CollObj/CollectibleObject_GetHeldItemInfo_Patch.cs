using System;
using System.Text;
using System.Text.RegularExpressions;
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
        
        public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            var itemstack = inSlot.Itemstack;
            var collObj = itemstack.Collectible;
            EntityPlayer entity = world.Side == EnumAppSide.Client ? (world as IClientWorldAccessor)?.Player.Entity : null;
            HydrationProperties hydrationProperties = collObj?.GetHydrationProperties(world, itemstack, entity);
            if (hydrationProperties == null) return;
            float spoilState = collObj.AppendPerishableInfoText(inSlot, new StringBuilder(), world);
            float spoilageFactor = GlobalConstants.FoodSpoilageSatLossMul(spoilState, itemstack, entity);
            var hydration = hydrationProperties.Hydration * spoilageFactor;
            string existingText = dsc.ToString();
            
            string satietyPattern = string.Format(Lang.Get("When eaten: {0} sat"), @"([0-9.]+)");
            string healthPattern = string.Format(Lang.Get("When eaten: {0} sat, {1} hp"), @"([0-9.]+)", @"([0-9.]+)");
            
            Match satietyMatch = Regex.Match(existingText, satietyPattern);
            Match healthMatch = Regex.Match(existingText, healthPattern);
            
            string mySatietyHydrationPattern = string.Format(Lang.Get("When eaten: {0} sat, {1} hyd"), @"([-]?[0-9.]+)", @"([-]?[0-9.]+)");
            string myHydrationPattern = string.Format(Lang.Get("When drank: {0} hyd"), @"([-]?[0-9.]+)");
            string mySatietyHydrationHealthPattern = string.Format(Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp"), @"([-]?[0-9.]+)", @"([-]?[0-9.]+)", @"([-]?[0-9.]+)");
            
            Match mySatietyHydrationMatch = Regex.Match(existingText, mySatietyHydrationPattern);
            Match myHydrationMatch = Regex.Match(existingText, myHydrationPattern);
            Match mySatietyHydrationHealthMatch = Regex.Match(existingText, mySatietyHydrationHealthPattern);

            // If a match is found, replace the hydration value in the matched line with the new hydration value
            if (mySatietyHydrationMatch.Success)
            {
                string existingLine = mySatietyHydrationMatch.Value;
                string updatedLine = Lang.Get("When eaten: {0} sat, {1} hyd",
                    mySatietyHydrationMatch.Groups[1].Value, Math.Round(hydration * spoilageFactor));
                dsc.Replace(existingLine, updatedLine);
            }
            else if (myHydrationMatch.Success)
            {
                string existingLine = myHydrationMatch.Value;
                string updatedLine = Lang.Get("When drank: {0} hyd", Math.Round(hydration * spoilageFactor));
                dsc.Replace(existingLine, updatedLine);
            }
            else if (mySatietyHydrationHealthMatch.Success)
            {
                string existingLine = mySatietyHydrationHealthMatch.Value;
                string updatedLine = Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp",
                    mySatietyHydrationHealthMatch.Groups[1].Value, Math.Round(hydration * spoilageFactor), mySatietyHydrationHealthMatch.Groups[2].Value);
                dsc.Replace(existingLine, updatedLine);
            }
            else if (healthMatch.Success)
            {
                // Access the matched satiety and health values
                string satiety = healthMatch.Groups[1].Value;
                string health = healthMatch.Groups[2].Value;
                
                string existingLine = healthMatch.Value;
                string updatedLine = Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp",
                    Math.Round(double.Parse(satiety)), Math.Round(hydration * spoilageFactor), Math.Round(double.Parse(health)));
                dsc.Replace(existingLine, updatedLine);
            } else if (satietyMatch.Success)
            {
                // Access the matched satiety value
                string satiety = satietyMatch.Groups[1].Value;
                
                if (Math.Round(double.Parse(satiety)) == 0)
                {
                    string existingLine = satietyMatch.Value;
                    string updatedLine = Lang.Get("When drank: {0} hyd", Math.Round(hydration * spoilageFactor));
                    dsc.Replace(existingLine, updatedLine);
                }

                // If the hydration is not zero and the health value was not matched, append the hydration
                if (hydration != 0 && !healthMatch.Success)
                {
                    string existingLine = satietyMatch.Value;
                    string updatedLine = Lang.Get("When eaten: {0} sat, {1} hyd",
                        Math.Round(double.Parse(satiety)), Math.Round(hydration * spoilageFactor));
                    dsc.Replace(existingLine, updatedLine);
                }
            }
            FoodNutritionProperties nutritionProperties = collObj.GetNutritionProperties(world, itemstack, entity);
            if (nutritionProperties != null && nutritionProperties.FoodCategory == EnumFoodCategory.NoNutrition)
            {
                string foodCategoryPattern = string.Format(Lang.Get("Food Category: {0}"), @"(.*)");
                Match foodCategoryMatch = Regex.Match(existingText, foodCategoryPattern);
                
                if (foodCategoryMatch.Success && hydration != 0)
                {
                    string existingLine = foodCategoryMatch.Value;
                    dsc.Replace(existingLine, "");
                }
            }
            if ((hydrationProperties.Purity != EnumPurityLevel.Okay && hydrationProperties.Purity != EnumPurityLevel.Pure) ||
                (hydrationProperties.Purity == EnumPurityLevel.Pure && !itemstack.Collectible.Code.ToString().Contains("pure")))
            {
                if (existingText.Contains(Lang.Get(BtCore.Modid+$":purity-{hydrationProperties.Purity}"))) return;
                string purityTranslation = Lang.Get(BtCore.Modid+":purity{0}");
                BtCore.Logger.Warning(purityTranslation);
                dsc.AppendLine(String.Format(purityTranslation, Lang.Get(BtCore.Modid+$":purity-{hydrationProperties.Purity}")));
            }
        }
}