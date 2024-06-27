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
            string btConfigPath = Path.Combine(configFolder, "BT.AddBlockHydration.json");
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

                var patches = parsedFile["patches"].ToObject<List<JObject>>();
                sortedPatches[priority].AddRange(patches);
            }

            Dictionary<string, JObject> mergedPatches = new Dictionary<string, JObject>();

            foreach (var priorityLevel in sortedPatches.Keys.OrderByDescending(k => k))
            {
                foreach (var patch in sortedPatches[priorityLevel])
                {
                    string blockCode = patch["blockCode"].ToString();
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
                            ["blockCode"] = "balancedthirst-purewater*",
                            ["hydrationByType"] = new JObject
                            {
                                ["*"] = 600
                            },
                            ["isBoiling"] = false,
                            ["hungerReduction"] = 50
                        },
                    }
                };
                File.WriteAllText(configPath, defaultConfig.ToString());
            }
        }

    }
}