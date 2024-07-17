using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BalancedThirst.Util;
using ProtoBuf;
using Vintagestory.API.Common;

namespace BalancedThirst.Config;

[ProtoContract]
public class SyncedConfig : IModConfig
{
    // Tag Booleans with IsRequired = true to prevent false values not being sent
    [ProtoMember(1, IsRequired = true)]
    public bool EnableThirst { get; set; } = true;
    
    [ProtoMember(2, IsRequired = true)]
    public bool EnableBladder { get; set; } = true;
    
    [ProtoMember(13, IsRequired = true)]
    public bool UrineStains { get; set; } = true;
    
    [ProtoMember(12, IsRequired = true)]
    public float ContainerDrinkSpeed { get; set; } = 0.25f;
    
    [ProtoMember(4, IsRequired = true)]
    public float DowsingRodRadius { get; set; } = BtConstants.DowsingRodRadius;
    [ProtoMember(5, IsRequired = true)]
    public float FruitHydrationYield { get; set; } = 0.3f;
    [ProtoMember(6, IsRequired = true)]
    public float VegetableHydrationYield { get; set; } = 0.2f;
    [ProtoMember(7, IsRequired = true)]
    public float DairyHydrationYield { get; set; } = 0.1f;
    [ProtoMember(8, IsRequired = true)]
    public float ProteinHydrationYield { get; set; } = 0.1f;
    [ProtoMember(9, IsRequired = true)]
    public float GrainHydrationYield { get; set; } = -0.2f;
    [ProtoMember(10, IsRequired = true)]
    public float NoNutritionHydrationYield { get; set; }
    [ProtoMember(11, IsRequired = true)]
    public float UnknownHydrationYield { get; set; }
    
    [ProtoMember(14, IsRequired = true)]
    public float ThirstRatePerDegrees { get; set; } = 5f;
    
    [ProtoMember(15, IsRequired = true)]
    public float HarshHeatExponentialMultiplier { get; set; } = 0.2f;
    
    [ProtoMember(16, IsRequired = true)]
    public bool ResetModBoosts { get; set; }
    
    public SyncedConfig() { }

    public SyncedConfig(ICoreAPI api, SyncedConfig previousConfig = null)
    {
        if (previousConfig == null)
        {
            return;
        }
        EnableThirst = previousConfig.EnableThirst;
        EnableBladder = previousConfig.EnableBladder;
        UrineStains = previousConfig.UrineStains;
        ContainerDrinkSpeed = previousConfig.ContainerDrinkSpeed;
        DowsingRodRadius = previousConfig.DowsingRodRadius;
        FruitHydrationYield = previousConfig.FruitHydrationYield;
        VegetableHydrationYield = previousConfig.VegetableHydrationYield;
        DairyHydrationYield = previousConfig.DairyHydrationYield;
        ProteinHydrationYield = previousConfig.ProteinHydrationYield;
        GrainHydrationYield = previousConfig.GrainHydrationYield;
        NoNutritionHydrationYield = previousConfig.NoNutritionHydrationYield;
        UnknownHydrationYield = previousConfig.UnknownHydrationYield;
        ThirstRatePerDegrees = previousConfig.ThirstRatePerDegrees;
        HarshHeatExponentialMultiplier = previousConfig.HarshHeatExponentialMultiplier;
        ResetModBoosts = previousConfig.ResetModBoosts;
    }

    public SyncedConfig Clone()
    {
        return new SyncedConfig
        {
            EnableThirst = EnableThirst,
            EnableBladder = EnableBladder,
            ContainerDrinkSpeed = ContainerDrinkSpeed,
            DowsingRodRadius = DowsingRodRadius,
            FruitHydrationYield = FruitHydrationYield,
            VegetableHydrationYield = VegetableHydrationYield,
            DairyHydrationYield = DairyHydrationYield,
            ProteinHydrationYield = ProteinHydrationYield,
            GrainHydrationYield = GrainHydrationYield,
            NoNutritionHydrationYield = NoNutritionHydrationYield,
            UnknownHydrationYield = UnknownHydrationYield,
            ThirstRatePerDegrees = ThirstRatePerDegrees,
            HarshHeatExponentialMultiplier = HarshHeatExponentialMultiplier,
            ResetModBoosts = ResetModBoosts
        };
    }
}