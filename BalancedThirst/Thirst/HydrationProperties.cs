using System;
using BalancedThirst.Util;
using Vintagestory.API.Common;

namespace BalancedThirst.Thirst;

public class HydrationProperties
{
    public float Hydration;
    public float HydrationLossDelay = 10f;
    public EnumPurityLevel Purity;
    public float EuhydrationWeight = 1/10f;
    public bool Scalding;
    [Obsolete ("Use EuhydrationWeight instead")]
    public bool Salty;
    
    public HydrationProperties Clone()
    {
        return new HydrationProperties()
        {
            Hydration = this.Hydration,
            HydrationLossDelay = this.HydrationLossDelay,
            Purity = this.Purity,
            EuhydrationWeight = this.EuhydrationWeight + (this.Salty ? -0.6f : 0),
            Scalding = this.Scalding,
        };
    }
    
    public static HydrationProperties FromFloat(float hydration)
    {
        return new HydrationProperties()
        {
            Hydration = hydration
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
                hydrationProps.Hydration = BtCore.ConfigServer.FruitHydrationYield * saturation;
                break;
            case EnumFoodCategory.Vegetable:
                hydrationProps.Hydration = BtCore.ConfigServer.VegetableHydrationYield * saturation;
                break;
            case EnumFoodCategory.Dairy:
                hydrationProps.Hydration = BtCore.ConfigServer.DairyHydrationYield * saturation;
                break;
            case EnumFoodCategory.Protein:
                hydrationProps.Hydration = BtCore.ConfigServer.ProteinHydrationYield * saturation;
                break;
            case EnumFoodCategory.Grain:
                hydrationProps.Hydration = BtCore.ConfigServer.GrainHydrationYield * saturation;
                break;
            case EnumFoodCategory.NoNutrition:
                if (BtCore.ConfigServer.NoNutritionHydrationYield == 0) return null;
                hydrationProps.Hydration = BtCore.ConfigServer.NoNutritionHydrationYield * saturation;
                break;
            case EnumFoodCategory.Unknown:
                if (BtCore.ConfigServer.UnknownHydrationYield == 0) return null;
                hydrationProps.Hydration = BtCore.ConfigServer.UnknownHydrationYield * saturation;
                break;
            default:
                return null;
        }
        return hydrationProps;
    }

    public static HydrationProperties operator /(HydrationProperties a, float b)
    {
        return new HydrationProperties()
        {
            Hydration = a.Hydration / b,
            HydrationLossDelay = a.HydrationLossDelay / b,
            Purity = a.Purity,
            EuhydrationWeight = a.EuhydrationWeight,
            Scalding = a.Scalding,
        };
    }
}