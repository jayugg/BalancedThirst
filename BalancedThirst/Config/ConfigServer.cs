using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;

namespace BalancedThirst.Config;

public class ConfigServer : IModConfig
{
    public float MaxHydration { get; set; } = 1500f;
    public bool ThirstKills { get; set; }
    public float ThirstSpeedModifier { get; set; }
    
    public float HotTemperatureThreshold { get; set; } = 27.0f;
    public float VomitHydrationMultiplier { get; set; } = 0.5f;
    public float VomitEuhydrationMultiplier { get; set; } = 0.8f;
    
    public float UrineNutrientChance { get; set; } = 0.4f;
    public float UrineDrainRate { get; set; } = 3f;
    
    public Dictionary<EnumSoilNutrient, float> UrineNutrientLevels { get; set; } = BtConstants.UrineNutrientLevels;
    
    public Dictionary<string, ThirstStatMultiplier> ThirstStatMultipliers { get; set; } = new()
    {
        { "hungerrate", new ThirstStatMultiplier() { Multiplier = 0.3f } },
        { "walkspeed", new ThirstStatMultiplier() { Multiplier = 0f, Inverted = true} }
    };

    public float PurePurityLevel { get; set; } = 1.0f;
    public float FilteredPurityLevel { get; set; } = 0.9f;
    public float PotablePurityLevel { get; set; } = 0.8f;
    public float OkayPurityLevel { get; set; } = 0.6f;
    public float StagnantPurityLevel { get; set; } = 0.3f;
    public float RotPurityLevel { get; set; } = 0.1f;
    public float FruitHydrationYield { get; set; } = 0.3f;
    public float VegetableHydrationYield { get; set; } = 0.2f;
    public float DairyHydrationYield { get; set; } = 0.1f;
    public float ProteinHydrationYield { get; set; } = 0.1f;
    public float GrainHydrationYield { get; set; } = -0.2f;
    public float NoNutritionHydrationYield { get; set; }
    public float UnknownHydrationYield { get; set; }
    public int DowsingRodRadius { get; set; } = 50;
    public bool BoilWaterInFirepits { get; set; } = true;
    public bool GushingSpringWater { get; set; } = true;
    
    // Advanced Settings
    public List<string> HeatableLiquidContainers { get; set; } = BtConstants.HeatableLiquidContainers;
    public List<string> WaterPortions { get; set; } = BtConstants.WaterPortions;
    public Dictionary<string, float> WaterContainers { get; set; } = BtConstants.WaterContainers;
    public Dictionary<string, HydrationProperties> HydratingLiquids { get; set; } = BtConstants.HydratingLiquids;
    public Dictionary<string, HydrationProperties> HydratingBlocks { get; set; } = BtConstants.HydratingBlocks;
    
    // Compatibility
    public bool UseHoDHydrationValues { get; set; }
    private bool _yieldThirstManagementToHoD;
    public bool YieldThirstManagementToHoD
    {
        get => _yieldThirstManagementToHoD;
        set => _yieldThirstManagementToHoD = BtCore.IsHoDLoaded && value;
    }
    
    public float HoDClothingCoolingMultiplier { get; set; } = 1f; 
    public ConfigServer(ICoreAPI api, ConfigServer previousConfig = null)
    {
        if (previousConfig == null)
        {
            return;
        }
        MaxHydration = previousConfig.MaxHydration;
        ThirstSpeedModifier = previousConfig.ThirstSpeedModifier;
        ThirstKills = previousConfig.ThirstKills;
        HotTemperatureThreshold = previousConfig.HotTemperatureThreshold;
        
        VomitHydrationMultiplier = previousConfig.VomitHydrationMultiplier;
        VomitEuhydrationMultiplier = previousConfig.VomitEuhydrationMultiplier;
        
        UrineNutrientChance = previousConfig.UrineNutrientChance;
        UrineDrainRate = previousConfig.UrineDrainRate;
        UrineNutrientLevels = previousConfig.UrineNutrientLevels;
        
        ThirstStatMultipliers = previousConfig.ThirstStatMultipliers;
        
        PurePurityLevel = previousConfig.PurePurityLevel;
        FilteredPurityLevel = previousConfig.FilteredPurityLevel;
        PotablePurityLevel = previousConfig.PotablePurityLevel;
        OkayPurityLevel = previousConfig.OkayPurityLevel;
        StagnantPurityLevel = previousConfig.StagnantPurityLevel;
        RotPurityLevel = previousConfig.RotPurityLevel;
        
        FruitHydrationYield = previousConfig.FruitHydrationYield;
        VegetableHydrationYield = previousConfig.VegetableHydrationYield;
        DairyHydrationYield = previousConfig.DairyHydrationYield;
        ProteinHydrationYield = previousConfig.ProteinHydrationYield;
        GrainHydrationYield = previousConfig.GrainHydrationYield;
        NoNutritionHydrationYield = previousConfig.NoNutritionHydrationYield;
        UnknownHydrationYield = previousConfig.UnknownHydrationYield;
        
        DowsingRodRadius = previousConfig.DowsingRodRadius;
        
        BoilWaterInFirepits = previousConfig.BoilWaterInFirepits;
        GushingSpringWater = previousConfig.GushingSpringWater;
        
        HeatableLiquidContainers = previousConfig.HeatableLiquidContainers;
        WaterContainers = previousConfig.WaterContainers;
        WaterPortions = previousConfig.WaterPortions;
        HydratingLiquids = previousConfig.HydratingLiquids;
        HydratingBlocks = previousConfig.HydratingBlocks;
        
        UseHoDHydrationValues = previousConfig.UseHoDHydrationValues;
        YieldThirstManagementToHoD = previousConfig.YieldThirstManagementToHoD;
        HoDClothingCoolingMultiplier = previousConfig.HoDClothingCoolingMultiplier;
    }
}