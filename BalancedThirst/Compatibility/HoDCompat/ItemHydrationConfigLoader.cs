using System.Collections.Generic;
using System.IO;
using System.Linq;
using BalancedThirst.Config;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace BalancedThirst.Compatibility.HoDCompat
{
    public static class ItemHydrationConfigLoader
    {
        public static List<JObject> LoadHydrationPatches(ICoreAPI api)
        {
            List<JObject> allPatches = new List<JObject>();
            string configFolder = ModConfig.GetConfigPath(api);
            List<string> configFiles = Directory.GetFiles(configFolder, "*AddItemHydration*.json").ToList();
            string btConfigPath = Path.Combine(configFolder, "BT.AddItemHydration.json");
            if (!File.Exists(btConfigPath))
            {
                GenerateBTHydrationConfig(api);
            }
            configFiles.Insert(0, btConfigPath);
            var sortedPatches = new SortedDictionary<int, List<JObject>>();

            foreach (string file in configFiles)
            {
                    string json = File.ReadAllText(file);
                    JObject parsedFile = JObject.Parse(json);
                    int priority = parsedFile["priority"]?.Value<int>() ?? 5;

                    if (!sortedPatches.ContainsKey(priority))
                    {
                        sortedPatches[priority] = new List<JObject>();
                    }

                    var patches = parsedFile["patches"]?.ToObject<List<JObject>>();
                    if (patches != null) sortedPatches[priority].AddRange(patches);
            }
            Dictionary<string, JObject> mergedPatches = new Dictionary<string, JObject>();

            foreach (var priorityLevel in sortedPatches.Keys.OrderByDescending(k => k))
            {
                foreach (var patch in sortedPatches[priorityLevel])
                {
                    string itemname = patch["itemname"]?.ToString();
                    if (itemname != null) mergedPatches[itemname] = patch;
                }
            }

            return mergedPatches.Values.ToList();
        }

        public static void GenerateBTHydrationConfig(ICoreAPI api)
        {
            string configPath = Path.Combine(ModConfig.GetConfigPath(api), "BT.AddItemHydration.json");
            if (!File.Exists(configPath))
            {
                var defaultConfig = new JObject
                {
                    ["priority"] = 2,
                    ["patches"] = new JArray
                    {
                        new JObject
                        {
                            ["itemname"] = "balancedthirst:waterportion-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["balancedthirst:waterportion-pure"] = 1000,
                                ["balancedthirst:waterportion-boiled"] = 800,
                                ["balancedthirst:waterportion-stagnant"] = 200,
                                ["*"] = 600
                            },
                            ["IsLiquid"] = true
                        },
                    }
                };
                    File.WriteAllText(configPath, defaultConfig.ToString());
            }
        }
    }
}