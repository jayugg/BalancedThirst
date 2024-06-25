using BalancedThirst.Util;

namespace BalancedThirst.Thirst;

public class ThirstStatMultiplier
{
    public float Multiplier { get; set; } 
    public EnumUpOrDown Centering { get; set; } = EnumUpOrDown.Centered;
    public EnumBuffCurve Curve { get; set; } = EnumBuffCurve.Sin;
    public EnumBuffCurve LowerHalfCurve { get; set; } = EnumBuffCurve.None;
    public bool Inverted { get; set; }
    
    public float CalcModifier(float ratio)
    {
        if (Inverted)
        {
            ratio = 1 - ratio;
        }
        if (LowerHalfCurve != EnumBuffCurve.None)
        {
            return ratio < 0.5
                ? Func.CalcStatModifier(ratio, Multiplier,
                    LowerHalfCurve, Centering)
                : Func.CalcStatModifier(ratio, Multiplier,
                    Curve, Centering);
        }
        return Func.CalcStatModifier(ratio, Multiplier,
            Curve, Centering);
    }
}