using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace BalancedThirst.HoDCompat;

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
                        var hydrationByType = patch["hydrationByType"].ToObject<Dictionary<string, float>>();
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
}