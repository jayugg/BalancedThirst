using System.Collections.Generic;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;

namespace BalancedThirst.Config;

public class ConfigServer : SyncedConfig
{
    public float MaxHydration { get; set; } = 1500f;
    public bool ThirstKills { get; set; }
    public float ThirstSpeedModifier { get; set; }
    public float HotTemperatureThreshold { get; set; } = 27.0f;
    public float VomitHydrationMultiplier { get; set; } = 0.5f;
    public float VomitEuhydrationMultiplier { get; set; } = 0.8f;
    public float BladderWalkSpeedDebuff { get; set; } = 0.5f;
    public float BladderCapacityOverload { get; set; } = 0.25f;
    public float UrineNutrientChance { get; set; } = 0.1f;
    public float UrineDrainRate { get; set; } = 3f;
    
    public Dictionary<EnumSoilNutrient, float> UrineNutrientLevels { get; set; } = BtConstants.UrineNutrientLevels;
    
    public Dictionary<string, StatMultiplier> ThirstStatMultipliers { get; set; } = new()
    {
        { "hungerrate", new StatMultiplier() { Multiplier = 0.3f } },
        { "walkspeed", new StatMultiplier() { Multiplier = 0f, Inverted = true} }
    };

    public float PurePurityLevel { get; set; } = 1.0f;
    public float FilteredPurityLevel { get; set; } = 0.9f;
    public float PotablePurityLevel { get; set; } = 0.8f;
    public float OkayPurityLevel { get; set; } = 0.6f;
    public float StagnantPurityLevel { get; set; } = 0.3f;
    public float RotPurityLevel { get; set; } = 0.1f;
    public bool GushingSpringWater { get; set; } = true;
    
    // Advanced Settings
    public List<string> WaterPortions { get; set; } = BtConstants.WaterPortions;
    public Dictionary<string, float> WaterContainers { get; set; } = BtConstants.WaterContainers;
    public Dictionary<string, HydrationProperties> HydratingLiquids { get; set; } = BtConstants.HydratingLiquids;
    public Dictionary<string, HydrationProperties> HydratingBlocks { get; set; } = BtConstants.HydratingBlocks;
    
    // Compatibility
    public bool UseHoDHydrationValues { get; set; }
    public float HoDClothingCoolingMultiplier { get; set; } = 1f; 
    public float CamelHumpMaxHydrationMultiplier { get; set; } = 1/3f;
    public float ElephantBladderCapacityMultiplier { get; set; } = 1/2f;
    public ConfigServer(ICoreAPI api, ConfigServer previousConfig = null)
    {
        if (previousConfig == null)
        {
            return;
        }
        EnableThirst = previousConfig.EnableThirst;
        MaxHydration = previousConfig.MaxHydration;
        ThirstSpeedModifier = previousConfig.ThirstSpeedModifier;
        ThirstKills = previousConfig.ThirstKills;
        HotTemperatureThreshold = previousConfig.HotTemperatureThreshold;
        ContainerDrinkSpeed = ContainerDrinkSpeed;
        
        VomitHydrationMultiplier = previousConfig.VomitHydrationMultiplier;
        VomitEuhydrationMultiplier = previousConfig.VomitEuhydrationMultiplier;

        EnableBladder = EnableBladder;
        BladderWalkSpeedDebuff = previousConfig.BladderWalkSpeedDebuff;
        BladderCapacityOverload = previousConfig.BladderCapacityOverload;
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
        
        GushingSpringWater = previousConfig.GushingSpringWater;
        
        WaterContainers = previousConfig.WaterContainers;
        WaterPortions = previousConfig.WaterPortions;
        HydratingLiquids = previousConfig.HydratingLiquids;
        HydratingBlocks = previousConfig.HydratingBlocks;
        
        UseHoDHydrationValues = previousConfig.UseHoDHydrationValues;
        HoDClothingCoolingMultiplier = previousConfig.HoDClothingCoolingMultiplier;
        CamelHumpMaxHydrationMultiplier = previousConfig.CamelHumpMaxHydrationMultiplier;
        ElephantBladderCapacityMultiplier = previousConfig.ElephantBladderCapacityMultiplier;
    }
}