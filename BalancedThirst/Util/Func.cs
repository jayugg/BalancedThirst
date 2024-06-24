using System;

namespace BalancedThirst.Util;

public static class Func
{
    public static float HungerModifierSin(float ratio, float param)
    {
        return param * (float) Math.Sin(0.5*Math.PI*(1f - 2f*ratio));
    }

    public static float HungerModifierLinear(float ratio, float param)
    {
        return param * (1f - 2f*ratio);
    }

    public static float HungerModifierArcSin(float ratio, float param)
    {
        return param * (float) (2*Math.PI*Math.Asin(1f - 2f*ratio));
    }

    public static float HungerModifierCubic(float ratio, float param)
    {
        return param * (float) Math.Pow(1f - 2f*ratio, 3);
    }

    public static float HungerModifierICubic(float ratio, float param)
    {
        return param * (float) Math.Pow(1f - 2f*ratio, 1.0/3);
    }

    public static float HungerModifierQuintic(float ratio, float param)
    {
        return param * (float) Math.Pow(1f - 2f*ratio, 5);
    }

    public static float HungerModifierIQuintic(float ratio, float param)
    {
        return param * (float) Math.Pow(1f - 2f*ratio, 1.0/5);
    }

    public static float CalcHungerModifier(float ratio, float param, EnumHungerBuffCurve curveType, EnumUpOrDown centering = EnumUpOrDown.Centered)
    {
        var res = curveType switch
        {
            EnumHungerBuffCurve.Linear => HungerModifierLinear(ratio, param),
            EnumHungerBuffCurve.Sin => HungerModifierSin(ratio, param),
            EnumHungerBuffCurve.Asin => HungerModifierArcSin(ratio, param),
            EnumHungerBuffCurve.Cubic => HungerModifierCubic(ratio, param),
            EnumHungerBuffCurve.InverseCubic => HungerModifierICubic(ratio, param),
            EnumHungerBuffCurve.Quintic => HungerModifierQuintic(ratio, param),
            EnumHungerBuffCurve.InverseQuintic => HungerModifierIQuintic(ratio, param),
            EnumHungerBuffCurve.Flat0 => 0,
            EnumHungerBuffCurve.Flat1 => param * Math.Sign(0.5f - ratio),
            _ => throw new ArgumentOutOfRangeException(nameof(curveType), curveType, null)
        };
        if (centering == EnumUpOrDown.Centered)
            return res;
        return centering == EnumUpOrDown.Up ? 0.5f*(res + param) : 0.5f*(res - param);
    }
}