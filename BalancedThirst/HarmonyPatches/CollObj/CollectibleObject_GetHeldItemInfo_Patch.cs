using System;
using System.Text;
using System.Text.RegularExpressions;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_GetHeldItemInfo_Patch
{
    public static bool ShouldSkipPatch => !ConfigSystem.SyncedConfigData.EnableThirst;
    
    public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (ShouldSkipPatch) return;
        var itemstack = inSlot.Itemstack;
        var collObj = itemstack.Collectible;
        EntityPlayer entity = world.Side == EnumAppSide.Client ? (world as IClientWorldAccessor)?.Player.Entity : null;
        HydrationProperties hydrationProperties = collObj?.GetHydrationProperties(world, itemstack, entity);
        if (hydrationProperties == null) return;
        float spoilState = collObj.AppendPerishableInfoText(inSlot, new StringBuilder(), world);
        float spoilageFactor = GlobalConstants.FoodSpoilageSatLossMul(spoilState, itemstack, entity);
        var hydration = hydrationProperties.Hydration * spoilageFactor;
        string existingText = dsc.ToString();
        
        string satietyPattern = Lang.Get("When eaten: {0} sat", @"([-]?[0-9.]+)");
        string healthPattern = Lang.Get("When eaten: {0} sat, {1} hp", @"([-]?[0-9.]+)", @"([-]?[0-9.]+)");
        
        Match satietyMatch = Regex.Match(existingText, satietyPattern);
        Match healthMatch = Regex.Match(existingText, healthPattern);
        
        string mySatietyHydrationPattern = Lang.Get("When eaten: {0} sat, {1} hyd", @"([-]?[0-9.]+)", @"([-]?[0-9.]+)");
        string myHydrationPattern = Lang.Get("When drank: {0} hyd", @"([-]?[0-9.]+)");
        string mySatietyHydrationHealthPattern = Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp", @"([-]?[0-9.]+)", @"([-]?[0-9.]+)", @"([-]?[0-9.]+)");
        
        Match mySatietyHydrationMatch = Regex.Match(existingText, mySatietyHydrationPattern);
        Match myHydrationMatch = Regex.Match(existingText, myHydrationPattern);
        Match mySatietyHydrationHealthMatch = Regex.Match(existingText, mySatietyHydrationHealthPattern);

        // If a match is found, replace the hydration value in the matched line with the new hydration value
        if (mySatietyHydrationMatch.Success && mySatietyHydrationMatch.Groups.Count > 0)
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
        else if (mySatietyHydrationHealthMatch.Success && mySatietyHydrationHealthMatch.Groups.Count > 1)
        {
            string existingLine = mySatietyHydrationHealthMatch.Value;
            string updatedLine = Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp",
                mySatietyHydrationHealthMatch.Groups[1].Value, Math.Round(hydration * spoilageFactor), mySatietyHydrationHealthMatch.Groups[2].Value);
            dsc.Replace(existingLine, updatedLine);
        }
        else if (healthMatch.Success && healthMatch.Groups.Count > 1)
        {
            // Access the matched satiety and health values
            string satiety = healthMatch.Groups[1].Value;
            string health = healthMatch.Groups[2].Value;
            
            string existingLine = healthMatch.Value;
            string updatedLine = Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp",
                Math.Round(double.Parse(satiety)), Math.Round(hydration * spoilageFactor), Math.Round(double.Parse(health)));
            dsc.Replace(existingLine, updatedLine);
        } else if (satietyMatch.Success && satietyMatch.Groups.Count > 0)
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
            string foodCategoryPattern = Lang.Get("Food Category: {0}", @"(.*)");
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
            dsc.AppendLine(Lang.Get(BtCore.Modid+":purity{0}", Lang.Get(BtCore.Modid+$":purity-{hydrationProperties.Purity}")));
        }
    }
}