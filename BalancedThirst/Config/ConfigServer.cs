using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using ProtoBuf;
using Vintagestory.API.Common;

namespace BalancedThirst.Config;

[ProtoContract]
public class ConfigServer : IModConfig
{
    #region thirst
    public bool EnableThirst { get; set; } = true;
    public float MaxHydration { get; set; } = 1500f;
    public bool ThirstKills { get; set; }
    public float ThirstSpeedModifier { get; set; }
    public float VomitHydrationMultiplier { get; set; } = 0.5f;
    public float VomitEuhydrationMultiplier { get; set; } = 0.8f;
    public float ContainerDrinkSpeed { get; set; } = 0.25f;
    public float DowsingRodRadius { get; set; } = BtConstants.DowsingRodRadius;
    public float FruitHydrationYield { get; set; } = 0.3f;
    public float VegetableHydrationYield { get; set; } = 0.2f;
    public float DairyHydrationYield { get; set; } = 0.1f;
    public float ProteinHydrationYield { get; set; } = 0.1f;
    public float GrainHydrationYield { get; set; } = -0.2f;
    public float NoNutritionHydrationYield { get; set; }
    public float UnknownHydrationYield { get; set; }
    public float ThirstRatePerDegrees { get; set; } = 5f;
    public float HarshHeatExponentialMultiplier { get; set; } = 0.2f;
    public bool DynamicWaterPurity { get; set; } = true;
    public bool EnableDehydration { get; set; } = true;
    public float HotTemperatureThreshold { get; set; } = 27.0f;
    public float PurePurityLevel { get; set; } = 1.0f;
    public float FilteredPurityLevel { get; set; } = 0.9f;
    public float PotablePurityLevel { get; set; } = 0.8f;
    public float OkayPurityLevel { get; set; } = 0.6f;
    public float StagnantPurityLevel { get; set; } = 0.3f;
    public float RotPurityLevel { get; set; } = 0.1f;
    public bool GushingSpringWater { get; set; } = true;
    
    // Advanced settings
    public Dictionary<string, StatMultiplier> ThirstStatMultipliers { get; set; } = new()
    {
        { "hungerrate", new StatMultiplier() { Multiplier = 0.3f } },
        { "walkspeed", new StatMultiplier() { Multiplier = 0f, Inverted = true} }
    };
    
    public List<string> WaterPortions { get; set; } = BtConstants.WaterPortions;
    public Dictionary<string, float> WaterContainers { get; set; } = BtConstants.WaterContainers;
    public Dictionary<string, HydrationProperties> HydratingLiquids { get; set; } = BtConstants.HydratingLiquids;
    public Dictionary<string, HydrationProperties> HydratingBlocks { get; set; } = BtConstants.HydratingBlocks;
    public Dictionary<string, float> DynamicWaterPurityWeights = BtConstants.DynamicWaterPurityWeights;
    
    // Compatibility
    public float HoDClothingCoolingMultiplier { get; set; } = 1f; 
    public float CamelHumpMaxHydrationMultiplier { get; set; } = 1/3f;

    #endregion

    #region bladder

    public bool EnableBladder { get; set; } = true;
    public bool UrineStains { get; set; } = true;
    public bool SpillWashStains { get; set; } = true;
    
    public float BladderWalkSpeedDebuff { get; set; } = 0.5f;
    public float BladderCapacityOverload { get; set; } = 0.25f;
    public float UrineNutrientChance { get; set; } = 0.1f;
    public float UrineDrainRate { get; set; } = 3f;
    
    // Advanced settings
    public Dictionary<EnumSoilNutrient, float> UrineNutrientLevels { get; set; } = BtConstants.UrineNutrientLevels;
    public List<EnumBlockMaterial> UrineStainableMaterials { get; set; } = BtConstants.UrineStainableMaterials;
    
    // Compatibility
    public float ElephantBladderCapacityMultiplier { get; set; } = 1/2f;

    #endregion

    public ConfigServer(ICoreAPI api, ConfigServer previousConfigServer = null)
    {
        if (previousConfigServer == null)
        {
            return;
        }
        EnableThirst = previousConfigServer.EnableThirst;
        MaxHydration = previousConfigServer.MaxHydration;
        ThirstKills = previousConfigServer.ThirstKills;
        ThirstSpeedModifier = previousConfigServer.ThirstSpeedModifier;
        VomitHydrationMultiplier = previousConfigServer.VomitHydrationMultiplier;
        VomitEuhydrationMultiplier = previousConfigServer.VomitEuhydrationMultiplier;
        ContainerDrinkSpeed = previousConfigServer.ContainerDrinkSpeed;
        DowsingRodRadius = previousConfigServer.DowsingRodRadius;
        FruitHydrationYield = previousConfigServer.FruitHydrationYield;
        VegetableHydrationYield = previousConfigServer.VegetableHydrationYield;
        DairyHydrationYield = previousConfigServer.DairyHydrationYield;
        ProteinHydrationYield = previousConfigServer.ProteinHydrationYield;
        GrainHydrationYield = previousConfigServer.GrainHydrationYield;
        NoNutritionHydrationYield = previousConfigServer.NoNutritionHydrationYield;
        UnknownHydrationYield = previousConfigServer.UnknownHydrationYield;
        ThirstRatePerDegrees = previousConfigServer.ThirstRatePerDegrees;
        HarshHeatExponentialMultiplier = previousConfigServer.HarshHeatExponentialMultiplier;
        DynamicWaterPurity = previousConfigServer.DynamicWaterPurity;
        EnableDehydration = previousConfigServer.EnableDehydration;
        HotTemperatureThreshold = previousConfigServer.HotTemperatureThreshold;
        PurePurityLevel = previousConfigServer.PurePurityLevel;
        FilteredPurityLevel = previousConfigServer.FilteredPurityLevel;
        PotablePurityLevel = previousConfigServer.PotablePurityLevel;
        OkayPurityLevel = previousConfigServer.OkayPurityLevel;
        StagnantPurityLevel = previousConfigServer.StagnantPurityLevel;
        RotPurityLevel = previousConfigServer.RotPurityLevel;
        GushingSpringWater = previousConfigServer.GushingSpringWater;
        ThirstStatMultipliers = previousConfigServer.ThirstStatMultipliers;
        WaterContainers = previousConfigServer.WaterContainers;
        WaterPortions = previousConfigServer.WaterPortions;
        HoDClothingCoolingMultiplier = previousConfigServer.HoDClothingCoolingMultiplier;
        CamelHumpMaxHydrationMultiplier = previousConfigServer.CamelHumpMaxHydrationMultiplier;
    }
}