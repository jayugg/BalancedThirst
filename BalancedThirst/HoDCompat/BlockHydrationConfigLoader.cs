using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BalancedThirst.Config;
using Vintagestory.API.Common;

namespace BalancedThirst.HoDCompat
{
    public static class BlockHydrationConfigLoader
    {
        public static List<JObject> LoadBlockHydrationConfig(ICoreAPI api)
        {
            List<JObject> allPatches = new List<JObject>();
            string configFolder = ModConfig.GetConfigPath(api);
            List<string> configFiles = Directory.GetFiles(configFolder, "*AddBlockHydration*.json").ToList();
            string defaultConfigPath = Path.Combine(configFolder, "HoD.AddBlockHydration.json");
            if (!File.Exists(defaultConfigPath))
            {
                GenerateDefaultBlockHydrationConfig(api);
            }
            string btConfigPath = Path.Combine(configFolder, "BT.AddBlockHydration.json");
            if (!File.Exists(btConfigPath))
            {
                GenerateBTHydrationConfig(api);
            }

            configFiles.Insert(0, defaultConfigPath);
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
                sortedPatches[priority].AddRange(patches);
            }

            Dictionary<string, JObject> mergedPatches = new Dictionary<string, JObject>();

            foreach (var priorityLevel in sortedPatches.Keys.OrderByDescending(k => k))
            {
                foreach (var patch in sortedPatches[priorityLevel])
                {
                    string blockCode = patch["blockCode"]?.ToString();
                    mergedPatches[blockCode] = patch;
                }
            }

            return mergedPatches.Values.ToList();
        }
        
        public static void GenerateBTHydrationConfig(ICoreAPI api)
        {
            string configPath = Path.Combine(ModConfig.GetConfigPath(api), "BT.AddBlockHydration.json");
            if (!File.Exists(configPath))
            {
                var defaultConfig = new JObject
                {
                    ["priority"] = 2,
                    ["patches"] = new JArray
                    {
                        new JObject
                        {
                            ["itemname"] = "game:balancedthirst-purewater*",
                            ["hydrationByType"] = new JObject
                            {
                                ["*"] = 1000
                            },
                            ["IsLiquid"] = true
                        },
                    }
                };
                File.WriteAllText(configPath, defaultConfig.ToString());
            }
        }

        public static void GenerateDefaultBlockHydrationConfig(ICoreAPI api)
        {
            string configPath = Path.Combine(ModConfig.GetConfigPath(api), "HoD.AddBlockHydration.json");
            if (!File.Exists(configPath))
            {
                var defaultConfig = new JObject
                {
                    ["priority"] = 5,
                    ["patches"] = new JArray
                    {
                        new JObject
                        {
                            ["blockCode"] = "boilingwater*",
                            ["hydrationByType"] = new JObject
                            {
                                ["boilingwater-*"] = 100,
                                ["*"] = 100 // Ensure default wildcard entry
                            },
                            ["isBoiling"] = true,
                            ["hungerReduction"] = 0
                        },
                        new JObject
                        {
                            ["blockCode"] = "water*",
                            ["hydrationByType"] = new JObject
                            {
                                ["water-*"] = 100,
                                ["*"] = 100
                            },
                            ["isBoiling"] = false,
                            ["hungerReduction"] = 100
                        },
                        new JObject
                        {
                            ["blockCode"] = "saltwater*",
                            ["hydrationByType"] = new JObject
                            {
                                ["saltwater-*"] = -600,
                                ["*"] = -600 
                            },
                            ["isBoiling"] = false,
                            ["hungerReduction"] = 100
                        }
                    }
                };

                File.WriteAllText(configPath, defaultConfig.ToString());
            }
        }

    }
}