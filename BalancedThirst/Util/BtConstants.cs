using System.Collections.Generic;
using BalancedThirst.ModBehavior;

namespace BalancedThirst.Util;

public static class BtConstants
{
    public static readonly string ConfigServerName = "balancedthirst" + ".json";
    public static readonly string ConfigClientName = "balancedthirst_client" + ".json";
    public static readonly List<string> HeatableLiquidContainers = new()
    {
        "game:bowl-fired",
        "game:jug-fired",
        BtCore.Modid + "kettle"
    };

    public static readonly List<string> WaterPortions = new()
    {
        "game:waterportion",
        "game:boilingwaterportion",
        "game:saltwaterportion",
        BtCore.Modid + ":waterportion-pure",
        BtCore.Modid + ":waterportion-boiled",
        BtCore.Modid + ":waterportion-stagnant"
    };
    
    public static readonly Dictionary<string, HydrationProperties> HydratingLiquids = new()
    {
        { "game:waterportion", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Okay } },
        { "game:rawjuice", new HydrationProperties { Hydration = 90 } },
        { "game:milkportion", new HydrationProperties { Hydration = 90 } },
        { "game:vinegarportion", new HydrationProperties { Hydration = 60 } },
        { "game:cider", new HydrationProperties { Hydration = 60 } },
        { "game:spirit", new HydrationProperties { Hydration = 20 } },
        { "game:honeyportion", new HydrationProperties { Hydration = 10 } },
        { "game:jamhoneyportion", new HydrationProperties { Hydration = 10 } },
        { "game:boilingwaterportion", new HydrationProperties { Hydration = 100, Scalding = true, Purity = EnumPurityLevel.Boiled } },
        { "game:saltwaterportion", new HydrationProperties { Hydration = 60, Salty = true, Purity = EnumPurityLevel.Okay } },
        { "game:brineportion", new HydrationProperties { Hydration = 80, Salty = true, Purity = EnumPurityLevel.Okay } },
        { BtCore.Modid + ":waterportion-pure", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Pure } },
        { BtCore.Modid + ":waterportion-boiled", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Boiled } },
        { BtCore.Modid + ":waterportion-stagnant", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Stagnant } },
        { "game:rot", new HydrationProperties { Hydration = 20, Purity = EnumPurityLevel.Yuck } }
    };
    
    public static readonly Dictionary<string, HydrationProperties> HydratingBlocks = new()
    {
        { "game:water", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Okay } },
        { "game:" + BtCore.Modid + "-purewater", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Pure } },
        { "game:boilingwater", new HydrationProperties { Hydration = 100, Scalding = true, Purity = EnumPurityLevel.Boiled } },
        { "game:saltwater", new HydrationProperties { Hydration = 75, Purity = EnumPurityLevel.Okay, Salty = true } }
    };
}