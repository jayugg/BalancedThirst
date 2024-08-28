using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Common;

namespace BalancedThirst.Thirst;

public class HydrationProperties
{
    public float Hydration;
    public float HydrationLossDelay = 10f;
    public EnumPurityLevel Purity = EnumPurityLevel.Distilled;
    public float EuhydrationWeight = 1/10f;
    public bool Scalding;
    public float Dehydration;
    
    public HydrationProperties Clone()
    {
        return new HydrationProperties()
        {
            Hydration = this.Hydration,
            HydrationLossDelay = this.HydrationLossDelay,
            Purity = this.Purity,
            EuhydrationWeight = this.EuhydrationWeight,
            Scalding = this.Scalding,
            Dehydration = this.Dehydration
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
                hydrationProps.Hydration = ConfigSystem.SyncedConfigData.FruitHydrationYield * saturation;
                break;
            case EnumFoodCategory.Vegetable:
                hydrationProps.Hydration = ConfigSystem.SyncedConfigData.VegetableHydrationYield * saturation;
                break;
            case EnumFoodCategory.Dairy:
                hydrationProps.Hydration = ConfigSystem.SyncedConfigData.DairyHydrationYield * saturation;
                break;
            case EnumFoodCategory.Protein:
                hydrationProps.Hydration = ConfigSystem.SyncedConfigData.ProteinHydrationYield * saturation;
                break;
            case EnumFoodCategory.Grain:
                hydrationProps.Hydration = ConfigSystem.SyncedConfigData.GrainHydrationYield * saturation;
                hydrationProps.Dehydration = 0.05f;
                break;
            case EnumFoodCategory.NoNutrition:
                if (ConfigSystem.SyncedConfigData.NoNutritionHydrationYield == 0) return null;
                hydrationProps.Hydration = ConfigSystem.SyncedConfigData.NoNutritionHydrationYield * saturation;
                break;
            case EnumFoodCategory.Unknown:
                if (ConfigSystem.SyncedConfigData.UnknownHydrationYield == 0) return null;
                hydrationProps.Hydration = ConfigSystem.SyncedConfigData.UnknownHydrationYield * saturation;
                break;
            default:
                return null;
        }
        return hydrationProps;
    }
    
    public static HydrationProperties operator *(HydrationProperties a, float b)
    {
        return new HydrationProperties()
        {
            Hydration = a.Hydration * b,
            HydrationLossDelay = a.HydrationLossDelay * b,
            Purity = a.Purity,
            EuhydrationWeight = a.EuhydrationWeight,
            Scalding = a.Scalding,
            Dehydration = a.Dehydration * b
        };
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
            Dehydration = a.Dehydration / b
        };
    }
    
    /// Used for BlockMeal calcs
    public static HydrationProperties operator +(HydrationProperties a, HydrationProperties b)
    {
        if (b == null || b.Hydration == 0) return a;
        if (a == null || a.Hydration == 0) return b;
        return new HydrationProperties()
        {
            Hydration = a.Hydration + b.Hydration,
            HydrationLossDelay = a.HydrationLossDelay + b.HydrationLossDelay,
            Purity = EnumPurityLevel.Distilled,
            EuhydrationWeight = a.EuhydrationWeight + b.EuhydrationWeight,
            Scalding = false,
            Dehydration = a.Dehydration + b.Dehydration
        };
    }
    
    public override string ToString()
    {
        return $"Hydration: {Hydration}, HydrationLossDelay: {HydrationLossDelay}, Purity: {Purity}, EuhydrationWeight: {EuhydrationWeight}, Scalding: {Scalding}, Dehydration: {Dehydration}";
    }
}