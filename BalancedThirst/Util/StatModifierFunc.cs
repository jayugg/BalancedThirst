using System;

namespace BalancedThirst.Util;

public static class Func
{
    public static float StatModifierSin(float ratio, float param)
    {
        return param * (float) Math.Sin(0.5*Math.PI*(1f - 2f*ratio));
    }

    public static float StatModifierLinear(float ratio, float param)
    {
        return param * (1f - 2f*ratio);
    }

    public static float StatModifierArcSin(float ratio, float param)
    {
        return param * (float) (2*Math.PI*Math.Asin(1f - 2f*ratio));
    }

    public static float StatModifierCubic(float ratio, float param)
    {
        return param * (float) Math.Pow(1f - 2f*ratio, 3);
    }

    public static float StatModifierICubic(float ratio, float param)
    {
        return - param * (float) Math.Pow(Math.Abs(1f - 2f*ratio), 1.0/3);
    }

    public static float StatModifierQuintic(float ratio, float param)
    {
        return param * (float) Math.Pow(1f - 2f*ratio, 5);
    }

    public static float StatModifierIQuintic(float ratio, float param)
    {
        return -param * (float) Math.Pow(Math.Abs(1f - 2f*ratio), 1.0/5);
    }

    public static float CalcStatModifier(float ratio, float param, EnumBuffCurve curveType, EnumUpOrDown centering = EnumUpOrDown.Centered)
    {
        var res = curveType switch
        {
            EnumBuffCurve.Linear => StatModifierLinear(ratio, param),
            EnumBuffCurve.Sin => StatModifierSin(ratio, param),
            EnumBuffCurve.Asin => StatModifierArcSin(ratio, param),
            EnumBuffCurve.Cubic => StatModifierCubic(ratio, param),
            EnumBuffCurve.InverseCubic => StatModifierICubic(ratio, param),
            EnumBuffCurve.Quintic => StatModifierQuintic(ratio, param),
            EnumBuffCurve.InverseQuintic => StatModifierIQuintic(ratio, param),
            EnumBuffCurve.Flat0 => 0,
            EnumBuffCurve.Flat1 => param * Math.Sign(0.5f - ratio),
            _ => throw new ArgumentOutOfRangeException(nameof(curveType), curveType, null)
        };
        if (centering == EnumUpOrDown.Centered)
            return res;
        return centering == EnumUpOrDown.Up ? 0.5f*(res + param) : 0.5f*(res - param);
    }
}