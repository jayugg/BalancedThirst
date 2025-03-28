using System.Collections.Generic;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BalancedThirst.Util;

public static class BtConstants
{
    public static readonly float DowsingRodRadius = 50f;
    public static readonly string ConfigServerName = "BalancedThirst/balancedthirst" + ".json";
    public static readonly string ConfigClientName = "BalancedThirst/balancedthirst_client" + ".json";

    public static readonly List<string> WaterPortions = new()
    {
        "@(.*):waterportion.*",
        "@(game):waterportion.*",
        "@(game):boilingwaterportion",
        "@(game):saltwaterportion",
        $"@({BtCore.Modid}):waterportion-(.*)"
    };
    
    public static readonly Dictionary<string, float> WaterContainers = new()
    {
        { "@(.*):waterskin.*", 0.6f },
        { $"@({BtCore.Modid}):gourd-.*-carved", 0.5f },
        { $"@({BtCore.Modid}):waterskin-pelt", 0.6f },
        { $"@({BtCore.Modid}):waterskin-leather", 0.45f },
        { "@(liquidcontainers):watercontainer-leather.*", 0.45f },
        { "@(liquidcontainers):watercontainer-bamboo.*", 0.6f },
        { "@(liquidcontainers):watercontainer-wood.*", 0.6f },
        { "@(liquidcontainers):watercontainermetal-.*", 0.35f }
    };
    
    // New balancing strat: 3.75 L of water to get full hydration, that is 400 hydration per liter
    public static readonly Dictionary<string, HydrationProperties> HydratingLiquids = new()
    {
        // Catch-alls
        { "@(.*):.*juiceportion.*", new HydrationProperties { Hydration = 360 } },
        { "@(.*):.*ciderportion.*", new HydrationProperties { Hydration = 240, Dehydration = 0.05f } },
        { "@(.*):.*spiritportion.*", new HydrationProperties { Hydration = 60, Dehydration = 0.2f } },
        { "@(.*):.*milkportion.*", new HydrationProperties { Hydration = 360 } },
        { "@(.*):.*honeyportion.*", new HydrationProperties { Hydration = 40 } },
        { "@(.*):.*vinegarportion.*", new HydrationProperties { Hydration = 280 } },
        { "@(.*):.*dryfruit.*", new HydrationProperties { Hydration = 10, Dehydration = 0.05f } },
        
        // From expandedfoods but just in case
        { "@(.*):fruitsyrupportion.*", new HydrationProperties { Hydration = 80 } },
        { "@(.*):treesyrupportion.*", new HydrationProperties { Hydration = 80 } },
        { "@(.*):brothportion.*", new HydrationProperties { Hydration = 80 } },
        { "@(.*):clarifiedbrothportion.*", new HydrationProperties { Hydration = 120 } },
        { "@(.*):strongspiritportion.*", new HydrationProperties { Hydration = 40, Dehydration = 0.25f } },
        { "@(.*):potentspiritportion.*", new HydrationProperties { Hydration = -10, Dehydration = 0.35f } },
        { "@(.*):vegetablejuiceportion.*", new HydrationProperties { Hydration = 320 } },
        { "@(.*):potentwineportion.*", new HydrationProperties { Hydration = 120, Dehydration = 0.1f } },
        { "@(.*):.*dehydratedfruit.*", new HydrationProperties { Hydration = 10, Dehydration = 0.01f } },
        { "@(.*):yogurt.*", new HydrationProperties { Hydration = 180 } },
        { "@(.*):yoghurt.*", new HydrationProperties { Hydration = 180 } }, // just in case
        
        { "@(game):waterportion", new HydrationProperties { Hydration = 400, Purity = EnumPurityLevel.Okay } },
        { "@(game):curdledmilkportion", new HydrationProperties { Hydration = 240 } },
        { "@(game):boilingwaterportion", new HydrationProperties { Hydration = 400, Scalding = true, Purity = EnumPurityLevel.Potable } },
        { "@(game):saltwaterportion", new HydrationProperties { Hydration = 300, Purity = EnumPurityLevel.Okay, EuhydrationWeight = -0.5f, Dehydration = 1f } },
        { "@(game):brineportion", new HydrationProperties { Hydration = 350, Purity = EnumPurityLevel.Pure, EuhydrationWeight = 0, Dehydration = 0.4f } },
        { "@(game):rot", new HydrationProperties { Hydration = 80, Purity = EnumPurityLevel.Yuck, EuhydrationWeight = -0.7f, Dehydration = 0.05f } },
        { "@(game):honeycomb", new HydrationProperties { Hydration = 40, Dehydration = 0.05f } },
        { "@(game):fat", new HydrationProperties { Hydration = 20, Dehydration = 0.1f } },
        { $"@({BtCore.Modid}):waterportion-pure", new HydrationProperties { Hydration = 400, Purity = EnumPurityLevel.Pure } },
        { $"@({BtCore.Modid}):waterportion-(boiled|potable)", new HydrationProperties { Hydration = 400, Purity = EnumPurityLevel.Potable } },
        { $"@({BtCore.Modid}):waterportion-stagnant", new HydrationProperties { Hydration = 400, Purity = EnumPurityLevel.Stagnant } },
        { $"@({BtCore.Modid}):waterportion-distilled", new HydrationProperties { Hydration = 400, Purity = EnumPurityLevel.Distilled, EuhydrationWeight = 0f } },
        { $"@({BtCore.Modid}):urineportion", new HydrationProperties { Hydration = 240, Purity = EnumPurityLevel.Pure, EuhydrationWeight = -0.5f, Dehydration = 1 } },
        { $"@({BtCore.Modid}):dryvegetable.*", new HydrationProperties { Hydration = 0, Dehydration = 0.05f } },
        { $"@({BtCore.Modid}):vomit", new HydrationProperties { Hydration = 20, Purity = EnumPurityLevel.Yuck } },
        { "@(aculinaryartillery):eggyolkfullportion-.*", new HydrationProperties { Hydration = 160 } },
        { "@(aculinaryartillery):eggyolkportion-.*", new HydrationProperties { Hydration = 100 } },
        { "@(aculinaryartillery):eggwhiteportion.*", new HydrationProperties { Hydration = 200 } },
        { "@(expandedfoods):dryfruit-.*", new HydrationProperties { Hydration = 20, Dehydration = 0.05f } },
        { "@(wildcraftherb):root-burnet", new HydrationProperties { Hydration = 20 } },
        { "@(butchering):bloodportion.*", new HydrationProperties { Hydration = 320 } },
        { "@(expandedfoods):soymilk-raw", new HydrationProperties { Hydration = 360, Purity = EnumPurityLevel.Yuck } },
        { "@(expandedfoods):soymilk-edible", new HydrationProperties { Hydration = 360 } },
        { "@(expandedfoods):peanutliquid.*", new HydrationProperties { Hydration = 60 } },
        { "@(expandedfoods):peanutliquid-butter", new HydrationProperties { Hydration = 20, Dehydration = 0.1f } },
        { "@(expandedfoods):maplesapportion-*", new HydrationProperties { Hydration = 200, Dehydration = 0.05f} },
        { "@(expandedfoods):dressing-salad-*", new HydrationProperties { Hydration = 180 } },
    };
    
    public static readonly Dictionary<string, HydrationProperties> HydratingBlocks = new()
    {
        { "@(game):water-.*", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Okay } },
        { $"@(game):{BtCore.Modid}-purewater-.*", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Pure } },
        { "@(game):boilingwater-.*", new HydrationProperties { Hydration = 100, Scalding = true, Purity = EnumPurityLevel.Potable } },
        { "@(game):saltwater-.*", new HydrationProperties { Hydration = 80, Purity = EnumPurityLevel.Okay, EuhydrationWeight = -0.5f, Dehydration = 5 } },
        { "@(.*):water-.*", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Okay } },
    };
    
    public static readonly List<EnumBlockMaterial> UrineStainableMaterials = new()
    {
        EnumBlockMaterial.Stone,
        EnumBlockMaterial.Soil
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
        { "drink", new WorldInteraction()
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

    public static string PeeKeyCode = BtCore.Modid + ":hotkey-pee";

    public static string DehydratedEffectId = $"{BtCore.Modid}:dehydrated";
    
    public static Dictionary<string, float> DynamicWaterPurityWeights = new()
    {
        { "temperature", 0.45f },
        { "rainfall", 0.3f },
        { "fertility", 0.1f },
        { "forestDensity", 0.2f },
        { "geologicActivity", 0.2f },
        { "altitude", 0.1f },
        { "flowingWater", 0.3f }
    };
}