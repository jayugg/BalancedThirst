using System.Reflection;
using BalancedThirst.Util;
using ProtoBuf;
using Vintagestory.API.Common;

namespace BalancedThirst.Config;

[ProtoContract]
public class SyncedConfig : IModConfig
{
    [ProtoMember(1)]
    public bool EnableThirst { get; set; } = true;
    
    [ProtoMember(2)]
    public bool EnableBladder { get; set; } = true;
    
    [ProtoMember(3)]
    public bool BoilWaterInFirepits { get; set; } = true;
    
    [ProtoMember(4)]
    public float DowsingRodRadius { get; set; } = BtConstants.DowsingRodRadius;
    [ProtoMember(5)]
    public float FruitHydrationYield { get; set; } = 0.3f;
    [ProtoMember(6)]
    public float VegetableHydrationYield { get; set; } = 0.2f;
    [ProtoMember(7)]
    public float DairyHydrationYield { get; set; } = 0.1f;
    [ProtoMember(8)]
    public float ProteinHydrationYield { get; set; } = 0.1f;
    [ProtoMember(9)]
    public float GrainHydrationYield { get; set; } = -0.2f;
    [ProtoMember(10)]
    public float NoNutritionHydrationYield { get; set; }
    [ProtoMember(11)]
    public float UnknownHydrationYield { get; set; }
    
    public SyncedConfig() { }

    public SyncedConfig(ICoreAPI api, SyncedConfig previousConfig = null)
    {
        if (previousConfig == null)
        {
            return;
        }
        EnableThirst = previousConfig.EnableThirst;
        EnableBladder = previousConfig.EnableBladder;
        BoilWaterInFirepits = previousConfig.BoilWaterInFirepits;
        DowsingRodRadius = previousConfig.DowsingRodRadius;
        FruitHydrationYield = previousConfig.FruitHydrationYield;
        VegetableHydrationYield = previousConfig.VegetableHydrationYield;
        DairyHydrationYield = previousConfig.DairyHydrationYield;
        ProteinHydrationYield = previousConfig.ProteinHydrationYield;
        GrainHydrationYield = previousConfig.GrainHydrationYield;
        NoNutritionHydrationYield = previousConfig.NoNutritionHydrationYield;
        UnknownHydrationYield = previousConfig.UnknownHydrationYield;
    }
    
    public static SyncedConfig FromServerConfig(ConfigServer config)
    {
        return new SyncedConfig
        {
            EnableThirst = config.EnableThirst,
            EnableBladder = config.EnableBladder,
            BoilWaterInFirepits = config.BoilWaterInFirepits,
            DowsingRodRadius = config.DowsingRodRadius,
            FruitHydrationYield = config.FruitHydrationYield,
            VegetableHydrationYield = config.VegetableHydrationYield,
            DairyHydrationYield = config.DairyHydrationYield,
            ProteinHydrationYield = config.ProteinHydrationYield,
            GrainHydrationYield = config.GrainHydrationYield,
            NoNutritionHydrationYield = config.NoNutritionHydrationYield,
            UnknownHydrationYield = config.UnknownHydrationYield
        };
    }

    public SyncedConfig Clone()
    {
        var clone = new SyncedConfig();
        var properties = typeof(SyncedConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.CanRead && property.CanWrite)
            {
                var value = property.GetValue(this);
                property.SetValue(clone, value);
            }
        }

        return clone;
    }
}