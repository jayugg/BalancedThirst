using System;
using Vintagestory.API.Common;

namespace BalancedThirst.ModBehavior;

public class HydrationProperties
{
    public float Hydration;
    public float HydrationLossDelay = 10f;
    public float Purity = 1;
    public bool Scalding;
    public bool Salty;
    
    public HydrationProperties Clone()
    {
        return new HydrationProperties()
        {
            Hydration = this.Hydration,
            HydrationLossDelay = this.HydrationLossDelay,
            Purity = this.Purity,
            Scalding = this.Scalding,
            Salty = this.Salty
        };
    }
    
    public static HydrationProperties FromNutrition(FoodNutritionProperties nutritionProps)
    {
        var hydrationProps = new HydrationProperties();
        if (nutritionProps == null) return null;
        var saturation = nutritionProps.Satiety;
        switch (nutritionProps.FoodCategory)
        {
            case EnumFoodCategory.Fruit:
                hydrationProps.Hydration = 0.3f * saturation;
                break;
            case EnumFoodCategory.Vegetable:
                hydrationProps.Hydration = 0.2f * saturation;
                break;
            case EnumFoodCategory.Dairy:
            case EnumFoodCategory.Protein:
                hydrationProps.Hydration = 0.1f * saturation;
                break;
            case EnumFoodCategory.Grain:
                hydrationProps.Hydration = -0.2f * saturation;
                break;
            default:
                return null;
        }
        return hydrationProps;
    }
}