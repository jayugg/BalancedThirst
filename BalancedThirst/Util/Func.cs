using System;

namespace BalancedThirst.Util;

public static class Func
{
    public static float HungerModifierSin(float ratio)
    {
        return BtCore.ConfigServer.ThirstHungerMultiplier * (float) Math.Sin(0.5*Math.PI*(1f - 2f*ratio));
    }

    public static float HungerModifierLinear(float ratio)
    {
        return BtCore.ConfigServer.ThirstHungerMultiplier * (1f - 2f*ratio);
    }

    public static float HungerModifierArcSin(float ratio)
    {
        return BtCore.ConfigServer.ThirstHungerMultiplier * (float) (2*Math.PI*Math.Asin(1f - 2f*ratio));
    }

    public static float HungerModifierCubic(float ratio)
    {
        return BtCore.ConfigServer.ThirstHungerMultiplier * (float) Math.Pow(1f - 2f*ratio, 3);
    }

    public static float HungerModifierICubic(float ratio)
    {
        return BtCore.ConfigServer.ThirstHungerMultiplier * (float) Math.Pow(1f - 2f*ratio, 1.0/3);
    }

    public static float HungerModifierQuintic(float ratio)
    {
        return BtCore.ConfigServer.ThirstHungerMultiplier * (float) Math.Pow(1f - 2f*ratio, 5);
    }

    public static float HungerModifierIQuintic(float ratio)
    {
        return BtCore.ConfigServer.ThirstHungerMultiplier * (float) Math.Pow(1f - 2f*ratio, 1.0/5);
    }

    public static float CalcHungerModifier(EnumHungerBuffCurve curveType, float ratio)
    {
        return curveType switch
        {
            EnumHungerBuffCurve.Linear => HungerModifierLinear(ratio),
            EnumHungerBuffCurve.Sin => HungerModifierSin(ratio),
            EnumHungerBuffCurve.Asin => HungerModifierArcSin(ratio),
            EnumHungerBuffCurve.Cubic => HungerModifierCubic(ratio),
            EnumHungerBuffCurve.InverseCubic => HungerModifierICubic(ratio),
            EnumHungerBuffCurve.Quintic => HungerModifierQuintic(ratio),
            EnumHungerBuffCurve.InverseQuintic => HungerModifierIQuintic(ratio),
            EnumHungerBuffCurve.Flat0 => 0,
            EnumHungerBuffCurve.Flat1 => BtCore.ConfigServer.ThirstHungerMultiplier * Math.Sign(0.5f - ratio),
            _ => throw new ArgumentOutOfRangeException(nameof(curveType), curveType, null)
        };
    }
}