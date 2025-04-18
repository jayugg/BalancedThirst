using System;
using System.Text;
using System.Text.RegularExpressions;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BalancedThirst.ModBehavior;

public class DrinkableBehavior : CollectibleBehavior
{
  
    private ICoreAPI _api;
    
    public DrinkableBehavior(CollectibleObject collObj) : base(collObj)
    {
    }
    
    public override void OnLoaded(ICoreAPI api)
    {
      _api = api;
      base.OnLoaded(api);
    }
    
    internal virtual HydrationProperties GetHydrationProperties(IWorldAccessor world, ItemStack itemstack, Entity byEntity)
    {
        return ExtractDirectHydrationProperties(itemstack);
    }

    protected HydrationProperties ExtractContainerHydrationProperties(IWorldAccessor world, ItemStack itemstack, Entity byEntity)
    {
        if (collObj is BlockLiquidContainerBase container && container.GetContent(itemstack) != null && container.GetContent(itemstack).Collectible.HasBehavior<DrinkableBehavior>())
        {
            return GetContainerHydrationProperties(container, itemstack);
        }
        return null;
    }

    protected HydrationProperties ExtractNutritionHydrationProperties(IWorldAccessor world, ItemStack itemstack, Entity byEntity)
    {
        if (itemstack.Collectible.HasBehavior<HydratingFoodBehavior>())
        {
            return HydrationProperties.FromNutrition(
                collObj.GetNutritionProperties(world, itemstack, byEntity) ?? 
                collObj.Attributes?.Token["nutritionPropsWhenInMeal"]?.ToObject<FoodNutritionProperties>()
                );
        }
        return null;
    }

    protected HydrationProperties ExtractDirectHydrationProperties(ItemStack itemstack)
    {
        try
        {
            var itemAttribute = itemstack?.ItemAttributes?["hydrationProps"];
            return itemAttribute is { Exists: true } ? itemAttribute.AsObject<HydrationProperties>(null, itemstack.Collectible.Code.Domain) : null;
        }
        catch (Exception ex)
        {
            BtCore.Logger.Error("Error getting hydration properties: " + ex.Message);
            return null;
        }
    }
    
    internal static HydrationProperties GetContentHydrationPropsPerLitre(
      BlockLiquidContainerBase container,
      ItemStack itemstack)
    {
      if (itemstack == null) return null;
      var content = container.GetContent(itemstack);
      if (content == null) return null;
      if (!content.Collectible.HasBehavior<DrinkableBehavior>()) return null;
      var behavior = content.Collectible.GetBehavior<DrinkableBehavior>();
      var hydrationProperties = behavior.ExtractDirectHydrationProperties(new ItemStack(content.Item) {Attributes = content.Attributes});
      return hydrationProperties;
    }
    
    public static HydrationProperties GetContainerHydrationProperties(
      BlockLiquidContainerBase container,
      ItemStack itemstack)
    {
      if (itemstack == null) return null;
      var content = container.GetContent(itemstack);
      if (content == null) return null;
      var hydrationProperties = GetContentHydrationPropsPerLitre(container, itemstack);
      var containableProps = BlockLiquidContainerBase.GetContainableProps(content);
      if (containableProps == null || hydrationProperties == null) return null;
      var num = content.StackSize / containableProps.ItemsPerLitre;
      hydrationProperties.Hydration *= num;
      hydrationProperties.HydrationLossDelay *= num;
      return hydrationProperties;
    }
    
    public static void PlayDrinkSound(EntityAgent byEntity, int eatSoundRepeats = 1)
    {
      if (byEntity.Controls.HandUse != EnumHandInteract.HeldItemInteract)
        return;
      IPlayer dualCallByPlayer = null;
      if (byEntity is EntityPlayer player)
        dualCallByPlayer = player.World.PlayerByUid(player.PlayerUID);
      byEntity.PlayEntitySound("drink", dualCallByPlayer);
      eatSoundRepeats--;
      if (eatSoundRepeats <= 0)
        return;
      byEntity.World.RegisterCallback(_ => PlayDrinkSound(byEntity, eatSoundRepeats), 300);
    }
    
    public static bool IsWaterPortion(CollectibleObject collectible)
    {
      try
      {
        var attribute = collectible.Attributes?["waterportion"];
        return attribute is { Exists: true } && attribute.AsBool();
      }
      catch (Exception ex)
      {
        BtCore.Logger.Error("Error getting waterportion bool: " + ex.Message);
        return false;
      }
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
      base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        var itemstack = inSlot.Itemstack;
        var collectible = itemstack.Collectible;
        var entity = world.Side == EnumAppSide.Client ? (world as IClientWorldAccessor)?.Player.Entity : null;
        if (entity == null) return;
        var hydrationProperties = GetHydrationProperties(world, itemstack, entity);
        if (hydrationProperties == null) return;
        var spoilState = collectible.AppendPerishableInfoText(inSlot, new StringBuilder(), world);
        var spoilageFactor = GlobalConstants.FoodSpoilageSatLossMul(spoilState, itemstack, entity);
        var hydration = hydrationProperties.Hydration * spoilageFactor;
        var existingText = dsc.ToString();
        
        var satietyPattern = Lang.Get("When eaten: {0} sat", @"([-]?[0-9.]+)");
        var healthPattern = Lang.Get("When eaten: {0} sat, {1} hp", @"([-]?[0-9.]+)", @"([-]?[0-9.]+)");
        
        var satietyMatch = Regex.Match(existingText, satietyPattern);
        var healthMatch = Regex.Match(existingText, healthPattern);
        
        var mySatietyHydrationPattern = Lang.Get("When eaten: {0} sat, {1} hyd", @"([-]?[0-9.]+)", @"([-]?[0-9.]+)");
        var myHydrationPattern = Lang.Get("When drank: {0} hyd", @"([-]?[0-9.]+)");
        var mySatietyHydrationHealthPattern = Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp", @"([-]?[0-9.]+)", @"([-]?[0-9.]+)", @"([-]?[0-9.]+)");
        
        var mySatietyHydrationMatch = Regex.Match(existingText, mySatietyHydrationPattern);
        var myHydrationMatch = Regex.Match(existingText, myHydrationPattern);
        var mySatietyHydrationHealthMatch = Regex.Match(existingText, mySatietyHydrationHealthPattern);

        // If a match is found, replace the hydration value in the matched line with the new hydration value
        if (mySatietyHydrationMatch.Success && mySatietyHydrationMatch.Groups.Count > 0)
        {
            var existingLine = mySatietyHydrationMatch.Value;
            var updatedLine = Lang.Get("When eaten: {0} sat, {1} hyd",
                mySatietyHydrationMatch.Groups[1].Value, Math.Round(hydration * spoilageFactor));
            dsc.Replace(existingLine, updatedLine);
        }
        else if (myHydrationMatch.Success)
        {
            var existingLine = myHydrationMatch.Value;
            var updatedLine = Lang.Get("When drank: {0} hyd", Math.Round(hydration * spoilageFactor));
            dsc.Replace(existingLine, updatedLine);
        }
        else if (mySatietyHydrationHealthMatch.Success && mySatietyHydrationHealthMatch.Groups.Count > 1)
        {
            var existingLine = mySatietyHydrationHealthMatch.Value;
            var updatedLine = Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp",
                mySatietyHydrationHealthMatch.Groups[1].Value, Math.Round(hydration * spoilageFactor), mySatietyHydrationHealthMatch.Groups[2].Value);
            dsc.Replace(existingLine, updatedLine);
        }
        else if (healthMatch.Success && healthMatch.Groups.Count > 1)
        {
            // Access the matched satiety and health values
            var satiety = healthMatch.Groups[1].Value;
            var health = healthMatch.Groups[2].Value;
            
            var existingLine = healthMatch.Value;
            var updatedLine = Lang.Get("When eaten: {0} sat, {1} hyd, {2} hp",
                Math.Round(double.Parse(satiety)), Math.Round(hydration * spoilageFactor), Math.Round(double.Parse(health)));
            dsc.Replace(existingLine, updatedLine);
        } else if (satietyMatch.Success && satietyMatch.Groups.Count > 0)
        {
            // Access the matched satiety value
            var satiety = satietyMatch.Groups[1].Value;
            
            if (Math.Round(double.Parse(satiety)) == 0 && hydration != 0)
            {
                var existingLine = satietyMatch.Value;
                var updatedLine = Lang.Get("When drank: {0} hyd", Math.Round(hydration * spoilageFactor));
                dsc.Replace(existingLine, updatedLine);
            }

            // If the hydration is not zero and the health value was not matched, append the hydration
            if (hydration != 0 && !healthMatch.Success)
            {
                var existingLine = satietyMatch.Value;
                var updatedLine = Lang.Get("When eaten: {0} sat, {1} hyd",
                    Math.Round(double.Parse(satiety)), Math.Round(hydration * spoilageFactor));
                dsc.Replace(existingLine, updatedLine);
            }
        }
        var nutritionProperties = collectible.GetNutritionProperties(world, itemstack, entity);
        if (nutritionProperties != null && nutritionProperties.FoodCategory == EnumFoodCategory.NoNutrition)
        {
            var foodCategoryPattern = Lang.Get("Food Category: {0}", @"(.*)");
            var foodCategoryMatch = Regex.Match(existingText, foodCategoryPattern);
            
            if (foodCategoryMatch.Success && hydration != 0)
            {
                var existingLine = foodCategoryMatch.Value;
                dsc.Replace(existingLine, "");
            }
        }
        if ((hydrationProperties.Purity != EnumPurityLevel.Okay && hydrationProperties.Purity != EnumPurityLevel.Distilled) ||
            (hydrationProperties.Purity == EnumPurityLevel.Pure && !itemstack.Collectible.Code.ToString().Contains("pure")))
        {
            if (existingText.Contains(Lang.Get(BtCore.Modid+$":purity-{hydrationProperties.Purity}"))) return;
            dsc.Append(Lang.Get(BtCore.Modid+":purity{0}", Lang.Get($"{BtCore.Modid}:purity-{hydrationProperties.Purity}")) + "\n");
        }
    }
}
