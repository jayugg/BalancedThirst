using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using BalancedThirst.Util;
using Vintagestory.API.Common;

namespace BalancedThirst.Config;

public class ConfigServer : IModConfig
{
    public List<string> HeatableLiquidContainers { get; set; } = BtConstants.HeatableLiquidContainers;

    public List<string> WaterPortions { get; set; } = BtConstants.WaterPortions;
    
    public Dictionary<string, HydrationProperties> HydratingLiquids { get; set; } = BtConstants.HydratingLiquids;
    
    public Dictionary<string, HydrationProperties> HydratingBlocks { get; set; } = BtConstants.HydratingBlocks;

    public float ThirstHungerMultiplier { get; set; } = 0.3f;
    public float VomitHydrationMultiplier { get; set; } = 0.5f;
    public float VomitEuhydrationMultiplier { get; set; } = 0.8f;
    
    public float PurePurityLevel { get; set; } = 1.0f;
    public float FilteredPurityLevel { get; set; } = 0.9f;
    public float BoiledPurityLevel { get; set; } = 0.8f;
    public float OkayPurityLevel { get; set; } = 0.6f;
    public float StagnantPurityLevel { get; set; } = 0.3f;
    public float RotPurityLevel { get; set; } = 0.1f;
    public float FruitHydrationYield { get; set; } = 0.3f;
    public float VegetableHydrationYield { get; set; } = 0.2f;
    public float DairyHydrationYield { get; set; } = 0.1f;
    public float ProteinHydrationYield { get; set; } = 0.1f;
    public float GrainHydrationYield { get; set; } = -0.2f;
    public float NoNutritionHydrationYield { get; set; } = 0;
    public float UnknownHydrationYield { get; set; } = 0;
    public int DowsingRodRadius { get; set; } = 50;
    
    public ConfigServer(ICoreAPI api, ConfigServer previousConfig = null)
    {
        if (previousConfig == null)
        {
            return;
        }
        HeatableLiquidContainers = previousConfig.HeatableLiquidContainers;
        WaterPortions = previousConfig.WaterPortions;
        HydratingLiquids = previousConfig.HydratingLiquids;
        HydratingBlocks = previousConfig.HydratingBlocks;
        ThirstHungerMultiplier = previousConfig.ThirstHungerMultiplier;
        DowsingRodRadius = previousConfig.DowsingRodRadius;
    }
}