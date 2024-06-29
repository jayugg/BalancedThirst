using System.Collections.Generic;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace BalancedThirst.Compatibility.HoDCompat;

public class HoDManager
{
    /* -------------------------Revised method for compatibility------------------------------ */
    // Revised method for compatibility
    public static void SetHydration(ICoreAPI api, CollectibleObject collectible, float hydrationValue)
    {
        collectible.AddDrinkableBehavior();
        collectible.SetHydrationProperties(HydrationProperties.FromFloat(hydrationValue));
    }
    /* --------------------------------------------------------------------------------------- */
    
    public static void ApplyHydrationPatches(ICoreAPI api, List<JObject> patches)
    {
        foreach (var collectible in api.World.Collectibles)
        {
            string itemName = collectible.Code?.ToString() ?? "Unknown Item";
            foreach (var patch in patches)
            {
                string patchItemName = patch["itemname"]?.ToString();
                if (IsMatch(itemName, patchItemName))
                {
                    if (patch.ContainsKey("hydration"))
                    {
                        float hydration = patch["hydration"].ToObject<float>();
                        SetHydration(api, collectible, hydration);
                    }
                    else if (patch.ContainsKey("hydrationByType"))
                    {
                        var hydrationByType = patch["hydrationByType"]?.ToObject<Dictionary<string, float>>();
                        foreach (var entry in hydrationByType)
                        {
                            string key = entry.Key;
                            float hydration = entry.Value;

                            if (IsMatch(itemName, key))
                            {
                                SetHydration(api, collectible, hydration);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
    private static bool IsMatch(string itemName, string patchItemName)
    {
        if (string.IsNullOrEmpty(patchItemName)) return false;
        if (patchItemName.Contains("*"))
        {
            string pattern = "^" + patchItemName.Replace("*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(itemName, pattern);
        }
        return itemName == patchItemName;
    }
    
    /* ------------------------------------------Block---------------------------------------- */

    public static void SetBlockHydration(Block block, BlockHydrationConfig config)
    {
        HydrationProperties hydrationProperties = config.ToHydrationProperties(block.Code.ToString());
        if (hydrationProperties != null)
        {
            block.SetHydrationProperties(hydrationProperties);
        }
    }

    public static void ApplyBlockHydrationPatches(ICoreAPI api, List<JObject> patches)
    {
        foreach (var block in api.World.Blocks)
        {
            foreach (var patch in patches)
            {
                string blockCode = patch["blockCode"].ToString();
                if (IsWildcardMatch(block.Code.ToString(), blockCode))
                {
                    var config = patch.ToObject<BlockHydrationConfig>();
                    SetBlockHydration(block, config);
                }
            }
        }
    }
    
    private static bool IsWildcardMatch(string text, string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern);
    }
    
    public static float? GetBlockHydration(string blockCode, Dictionary<string, float> hydrationByType)
    {
        // Exact match
        if (hydrationByType.TryGetValue(blockCode, out var hydration))
        {
            return hydration;
        }

        // Wildcard match
        foreach (var key in hydrationByType.Keys)
        {
            if (IsWildcardMatch(blockCode, key))
            {
                return hydrationByType[key];
            }
        }
        return null;
    }

    public class BlockHydrationConfig
    {
        public Dictionary<string, float> HydrationByType { get; set; }
        public bool IsBoiling { get; set; }
        public int HungerReduction { get; set; }
    
        public HydrationProperties ToHydrationProperties(string blockCode)
        {
            if (HydrationByType != null && GetBlockHydration(blockCode, HydrationByType) is { } hydration)
            {
                return new HydrationProperties
                {
                    Hydration = hydration,
                    Scalding = IsBoiling,
                };
            }
            return null;
        }
    }
}
