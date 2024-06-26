using System.Collections.Generic;
using BalancedThirst.Thirst;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BalancedThirst.Util;

public static class BtConstants
{
    public const string modDomain = "balancedthirst";
    
    public static readonly string ConfigServerName = "balancedthirst" + ".json";
    public static readonly string ConfigClientName = "balancedthirst_client" + ".json";
    
    public static readonly List<string> HeatableLiquidContainers = new()
    {
        "@(game):bowl-fired",
        "@(game):jug-fired",
        "@("+ BtCore.Modid + "):kettle-(.*)",
        "@("+ BtCore.Modid + "):kettle-clay-fired"
    };

    public static readonly List<string> WaterPortions = new()
    {
        "@(game):waterportion",
        "@(game):boilingwaterportion",
        "@(game):saltwaterportion",
        "@("+ BtCore.Modid + "):waterportion-(.*)"
    };
    
    public static readonly Dictionary<string, float> WaterContainers = new()
    {
        { "@(" + BtCore.Modid + "):waterskin-pelt", 0.6f },
        { "@(" + BtCore.Modid + "):waterskin-leather", 0.45f },
        { "@(liquidcontainers):watercontainer-leather.*", 0.45f },
        { "@(liquidcontainers):watercontainer-bamboo.*", 0.6f },
        { "@(liquidcontainers):watercontainer-wood.*", 0.6f },
        { "@(liquidcontainers):watercontainermetal-.*", 0.35f}
    };
    
    public static readonly Dictionary<string, HydrationProperties> HydratingLiquids = new()
    {
        { "@(game):waterportion", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Okay } },
        { "@(game):rawjuice-.*", new HydrationProperties { Hydration = 90 } },
        { "@(game):milkportion", new HydrationProperties { Hydration = 90 } },
        { "@(game):vinegarportion", new HydrationProperties { Hydration = 60 } },
        { "@(game):cider-.*", new HydrationProperties { Hydration = 60 } },
        { "@(game):spirit-.*", new HydrationProperties { Hydration = 20 } },
        { "@(game):spiritportion-.*", new HydrationProperties { Hydration = 10 } },
        { "@(game):honeyportion", new HydrationProperties { Hydration = 10 } },
        { "@(game):jamhoneyportion", new HydrationProperties { Hydration = 10 } },
        { "@(game):boilingwaterportion", new HydrationProperties { Hydration = 100, Scalding = true, Purity = EnumPurityLevel.Potable } },
        { "@(game):saltwaterportion", new HydrationProperties { Hydration = 60, Purity = EnumPurityLevel.Okay, EuhydrationWeight = -0.5f } },
        { "@(game):brineportion", new HydrationProperties { Hydration = 80, Purity = EnumPurityLevel.Okay } },
        { "@(" + BtCore.Modid + "):waterportion-pure", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Pure } },
        { "@(" + BtCore.Modid + "):waterportion-boiled", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Potable } },
        { "@(" + BtCore.Modid + "):waterportion-stagnant", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Stagnant } },
        { "@(" + BtCore.Modid + "):urineportion", new HydrationProperties { Hydration = 80, Purity = EnumPurityLevel.Pure, EuhydrationWeight = -0.5f } },
        { "@(game):rot", new HydrationProperties { Hydration = 20, Purity = EnumPurityLevel.Yuck } },
        { "@(game):honeycomb", new HydrationProperties { Hydration = 10 } },
    };
    
    public static readonly Dictionary<string, HydrationProperties> HydratingBlocks = new()
    {
        { "@(game):water-.*", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Okay } },
        { "@(game):" + BtCore.Modid + "-purewater-.*", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Pure } },
        { "@(game):boilingwater-.*", new HydrationProperties { Hydration = 100, Scalding = true, Purity = EnumPurityLevel.Potable } },
        { "@(game):saltwater-.*", new HydrationProperties { Hydration = 75, Purity = EnumPurityLevel.Okay, Salty = true } }
    };
    
    public static readonly Dictionary<EnumSoilNutrient, float> UrineNutrientLevels = new()
    {
        { EnumSoilNutrient.N, 0.1f },
        { EnumSoilNutrient.P, 0.0f },
        { EnumSoilNutrient.K, 0.1f }
    };
    
    public struct InteractionIds {
        public const string Drink = "drink";
        public const string PeeStand = "pee-stand";
        public const string PeeSit = "pee-sit";
        public const string Pee = "pee";
    }
    
    public static readonly Dictionary<string, WorldInteraction> Interactions = new()
    {
        {"drink", new WorldInteraction()
            {
                ActionLangCode = BtCore.Modid + ":interaction-drink",
                MouseButton = EnumMouseButton.Right,
                RequireFreeHand = true
            }
        },
        { "pee-stand", new WorldInteraction()
            {
                RequireFreeHand = true,
                ActionLangCode = BtCore.Modid+":interaction-pee",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "ctrl"
            }
        },
        { "pee-sit", new WorldInteraction()
            {
                ActionLangCode = BtCore.Modid+":interaction-pee",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "sitdown"
            }
        },
        { "pee", new WorldInteraction()
            {
                RequireFreeHand = true,
                ActionLangCode = BtCore.Modid+":interaction-pee",
                MouseButton = EnumMouseButton.Right
            }
        }
    };
}