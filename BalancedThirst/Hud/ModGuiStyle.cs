using System;

namespace BalancedThirst.Hud;

public static class ModGuiStyle
{
    public static readonly double[] ThirstBarColor = new[]
    {
        0.2078431397676468,
        0.3137255012989044,
        0.43921568989753723,
        1.0
    };
    
    public static readonly double[] BladderBarColor = new[]
    {
        98 / 255.0,
        190 / 255.0,
        193 / 255.0,
        1.0
    };
    
    public static readonly double[] ThirstBarColor2 = new[]
    {
        38 / 255.0,
        70 / 255.0,
        83 / 255.0,
        1.0
    };
    public static readonly double[] ThirstBarColor3 = new[]
    {
        98 / 255.0,
        190 / 255.0,
        193 / 255.0,
        1.0
    };
    
    public static string ToHex(this double[] rgba)
    {
        if (rgba == null || rgba.Length < 3)
        {
            return "#000000";
        }
        
        string red = ((int)(rgba[0] * 255)).ToString("X2");
        string green = ((int)(rgba[1] * 255)).ToString("X2");
        string blue = ((int)(rgba[2] * 255)).ToString("X2");
        return "#" + red + green + blue;
    }
    
    public static double[] FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || (hex.Length != 7 && hex.Length != 9))
        {
            throw new ArgumentException("Hex string must start with '#' and be either 7 or 9 characters long.");
        }
        int red = Convert.ToInt32(hex.Substring(1, 2), 16);
        int green = Convert.ToInt32(hex.Substring(3, 2), 16);
        int blue = Convert.ToInt32(hex.Substring(5, 2), 16);
        double alpha = 1.0;
        if (hex.Length == 9)
        {
            alpha = Convert.ToInt32(hex.Substring(7, 2), 16) / 255.0;
        }
        return new[] { red / 255.0, green / 255.0, blue / 255.0, alpha };
    }
}
