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
    public static readonly string SyncedConfigName = "BalancedThirst/balancedthirst_sync" + ".json";

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
        { $"@({BtCore.Modid}):waterskin-pelt", 0.6f },
        { $"@({BtCore.Modid}):waterskin-leather", 0.45f },
        { "@(liquidcontainers):watercontainer-leather.*", 0.45f },
        { "@(liquidcontainers):watercontainer-bamboo.*", 0.6f },
        { "@(liquidcontainers):watercontainer-wood.*", 0.6f },
        { "@(liquidcontainers):watercontainermetal-.*", 0.35f }
    };
    
    public static readonly Dictionary<string, HydrationProperties> HydratingLiquids = new()
    {
        // Catch-alls
        { "@(.*):.*juiceportion.*", new HydrationProperties { Hydration = 180 } },
        { "@(.*):.*ciderportion.*", new HydrationProperties { Hydration = 120, Dehydration = 0.05f } },
        { "@(.*):.*spiritportion.*", new HydrationProperties { Hydration = 30, Dehydration = 0.2f } },
        { "@(.*):.*milkportion.*", new HydrationProperties { Hydration = 180 } },
        { "@(.*):.*honeyportion.*", new HydrationProperties { Hydration = 20 } },
        { "@(.*):.*vinegarportion.*", new HydrationProperties { Hydration = 140 } },
        { "@(.*):.*dryfruit.*", new HydrationProperties { Hydration = 10, Dehydration = 0.05f } },
        
        // From expandedfoods but just in case
        { "@(.*):fruitsyrupportion.*", new HydrationProperties { Hydration = 40 } },
        { "@(.*):treesyrupportion.*", new HydrationProperties { Hydration = 40 } },
        { "@(.*):brothportion.*", new HydrationProperties { Hydration = 40 } },
        { "@(.*):clarifiedbrothportion.*", new HydrationProperties { Hydration = 60 } },
        { "@(.*):strongspiritportion.*", new HydrationProperties { Hydration = 20, Dehydration = 0.25f } },
        { "@(.*):potentspiritportion.*", new HydrationProperties { Hydration = -10, Dehydration = 0.35f } },
        { "@(.*):vegetablejuiceportion.*", new HydrationProperties { Hydration = 160 } },
        { "@(.*):potentwineportion.*", new HydrationProperties { Hydration = 60, Dehydration = 0.1f } },
        { "@(.*):yogurt.*", new HydrationProperties { Hydration = 90 } },
        { "@(.*):yoghurt.*", new HydrationProperties { Hydration = 90 } }, // just in case
        
        { "@(game):waterportion", new HydrationProperties { Hydration = 200, Purity = EnumPurityLevel.Okay } },
        { "@(game):curdledmilkportion", new HydrationProperties { Hydration = 100 } },
        { "@(game):boilingwaterportion", new HydrationProperties { Hydration = 200, Scalding = true, Purity = EnumPurityLevel.Potable } },
        { "@(game):saltwaterportion", new HydrationProperties { Hydration = 120, Purity = EnumPurityLevel.Okay, EuhydrationWeight = -0.5f, Dehydration = 1f } },
        { "@(game):brineportion", new HydrationProperties { Hydration = 160, Purity = EnumPurityLevel.Pure, EuhydrationWeight = 0, Dehydration = 0.4f } },
        { "@(game):rot", new HydrationProperties { Hydration = 40, Purity = EnumPurityLevel.Yuck, EuhydrationWeight = -0.7f, Dehydration = 0.05f } },
        { "@(game):honeycomb", new HydrationProperties { Hydration = 20, Dehydration = 0.05f } },
        { "@(game):fat", new HydrationProperties { Hydration = 10, Dehydration = 0.1f } },
        { $"@({BtCore.Modid}):waterportion-pure", new HydrationProperties { Hydration = 200, Purity = EnumPurityLevel.Pure } },
        { $"@({BtCore.Modid}):waterportion-boiled", new HydrationProperties { Hydration = 200, Purity = EnumPurityLevel.Potable } },
        { $"@({BtCore.Modid}):waterportion-stagnant", new HydrationProperties { Hydration = 200, Purity = EnumPurityLevel.Stagnant } },
        { $"@({BtCore.Modid}):waterportion-distilled", new HydrationProperties { Hydration = 200, Purity = EnumPurityLevel.Pure, EuhydrationWeight = 0f } },
        { $"@({BtCore.Modid}):urineportion", new HydrationProperties { Hydration = 120, Purity = EnumPurityLevel.Pure, EuhydrationWeight = -0.5f, Dehydration = 1 } },
        { $"@({BtCore.Modid}):dryvegetable.*", new HydrationProperties { Hydration = 0, Dehydration = 0.05f } },
        { "@(aculinaryartillery):eggyolkfullportion-.*", new HydrationProperties { Hydration = 80 } },
        { "@(aculinaryartillery):eggyolkportion-.*", new HydrationProperties { Hydration = 50 } },
        { "@(aculinaryartillery):eggwhiteportion.*", new HydrationProperties { Hydration = 100 } },
        { "@(expandedfoods):dryfruit-.*", new HydrationProperties { Hydration = 10, Dehydration = 0.05f } },
        { "@(wildcraftherb):root-burnet", new HydrationProperties { Hydration = 10 } },
        { "@(butchering):bloodportion.*", new HydrationProperties { Hydration = 160 } },
        { "@(expandedfoods):soymilk-raw", new HydrationProperties { Hydration = 160, Purity = EnumPurityLevel.Yuck } },
        { "@(expandedfoods):soymilk-edible", new HydrationProperties { Hydration = 160 } },
        { "@(expandedfoods):peanutliquid.*", new HydrationProperties { Hydration = 30 } },
        { "@(expandedfoods):peanutliquid-butter", new HydrationProperties { Hydration = 0, Dehydration = 0.1f } },
        { "@(expandedfoods):maplesapportion-*", new HydrationProperties { Hydration = 80, Dehydration = 0.05f} },
        { "@(expandedfoods):dressing-salad-*", new HydrationProperties { Hydration = 90 } },
    };
    
    public static readonly Dictionary<string, HydrationProperties> HydratingBlocks = new()
    {
        { "@(game):water-.*", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Okay } },
        { $"@(game):{BtCore.Modid}-purewater-.*", new HydrationProperties { Hydration = 100, Purity = EnumPurityLevel.Pure } },
        { "@(game):boilingwater-.*", new HydrationProperties { Hydration = 100, Scalding = true, Purity = EnumPurityLevel.Potable } },
        { "@(game):saltwater-.*", new HydrationProperties { Hydration = 75, Purity = EnumPurityLevel.Okay, EuhydrationWeight = -0.5f, Dehydration = 5 } },
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
}