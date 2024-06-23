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