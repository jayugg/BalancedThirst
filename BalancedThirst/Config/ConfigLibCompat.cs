using System;
using System.Collections.Generic;
using System.Linq;
using BalancedThirst.ModBehavior;
using BalancedThirst.Util;
using ConfigLib;
using ImGuiNET;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BalancedThirst.Config;

// From https://github.com/Craluminum-Mods/DanaTweaks/
public class ConfigLibCompat
{
    private const string settingPrefix = "balancedthirst:Config.Setting.";
    
    private const string settingsSimple = "balancedthirst:Config.SettingsSimple";
    private const string settingsAdvanced = "balancedthirst:Config.SettingsAdvanced";
    private const string textSupportsWildcard = "balancedthirst:Config.Text.SupportsWildcard";

    public ConfigLibCompat(ICoreAPI api)
    {
        api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig(Lang.Get("balancedthirst:balancedthirst"), (id, buttons) => EditConfigServer(id, buttons, api));
        api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig(Lang.Get("balancedthirst:balancedthirst_client"), (id, buttons) => EditConfigClient(id, buttons, api));
    }
    
    private void EditConfigClient(string id, ControlButtons buttons, ICoreAPI api)
    {
        if (buttons.Save) ModConfig.WriteConfig(api, BtConstants.ConfigClientName, BtCore.ConfigClient);
        if (buttons.Restore) BtCore.ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, BtConstants.ConfigClientName);
        if (buttons.Defaults) BtCore.ConfigClient = new(api);

        BuildSettingsClient(BtCore.ConfigClient, id);
    }
    
    private void BuildSettingsClient(ConfigClient config, string id)
    {
        if (ImGui.CollapsingHeader(Lang.Get(settingsSimple) + $"##settingSimple-{id}"))
        {
            config.ThirstBarX = OnInputFloat(id, config.ThirstBarX, nameof(config.ThirstBarX));
            config.ThirstBarY = OnInputFloat(id, config.ThirstBarY, nameof(config.ThirstBarY));
        }
    }

    private void EditConfigServer(string id, ControlButtons buttons, ICoreAPI api)
    {
        if (buttons.Save) ModConfig.WriteConfig(api, BtConstants.ConfigServerName, BtCore.ConfigServer);
        if (buttons.Restore) BtCore.ConfigServer = ModConfig.ReadConfig<ConfigServer>(api, BtConstants.ConfigServerName);
        if (buttons.Defaults) BtCore.ConfigServer = new(api);

        BuildSettingsServer(BtCore.ConfigServer, id);
    }
    
    private void BuildSettingsServer(ConfigServer config, string id)
    {
        if (ImGui.CollapsingHeader(Lang.Get(settingsSimple) + $"##settingSimple-{id}"))
        {
            config.ThirstKills = OnCheckBox(id, config.ThirstKills, nameof(config.ThirstKills));
            config.ThirstSpeedModifier = OnInputFloat(id, config.ThirstSpeedModifier, nameof(config.ThirstSpeedModifier));
            config.ThirstHungerMultiplier = OnInputFloat(id, config.ThirstHungerMultiplier, nameof(config.ThirstHungerMultiplier));
            config.ThirstHungerMultiplierUpOrDown = OnInputEnum(id, config.ThirstHungerMultiplierUpOrDown, nameof(config.ThirstHungerMultiplierUpOrDown));
            config.HungerBuffCurve = OnInputEnum(id, config.HungerBuffCurve, nameof(config.HungerBuffCurve));
            config.LowerHalfHungerBuffCurve = OnInputEnum(id, config.LowerHalfHungerBuffCurve, nameof(config.LowerHalfHungerBuffCurve));
            ImGui.Separator();
            config.VomitHydrationMultiplier = OnInputFloat(id, config.VomitHydrationMultiplier, nameof(config.VomitHydrationMultiplier));
            config.VomitEuhydrationMultiplier = OnInputFloat(id, config.VomitEuhydrationMultiplier, nameof(config.VomitEuhydrationMultiplier));
            ImGui.Separator();
            config.PurePurityLevel = OnInputFloat(id, config.PurePurityLevel, nameof(config.PurePurityLevel));
            config.FilteredPurityLevel = OnInputFloat(id, config.FilteredPurityLevel, nameof(config.FilteredPurityLevel));
            config.PotablePurityLevel = OnInputFloat(id, config.PotablePurityLevel, nameof(config.PotablePurityLevel));
            config.OkayPurityLevel = OnInputFloat(id, config.OkayPurityLevel, nameof(config.OkayPurityLevel));
            config.StagnantPurityLevel = OnInputFloat(id, config.StagnantPurityLevel, nameof(config.StagnantPurityLevel));
            config.RotPurityLevel = OnInputFloat(id, config.RotPurityLevel, nameof(config.RotPurityLevel));
            ImGui.Separator();
            config.FruitHydrationYield = OnInputFloat(id, config.FruitHydrationYield, nameof(config.FruitHydrationYield));
            config.VegetableHydrationYield = OnInputFloat(id, config.VegetableHydrationYield, nameof(config.VegetableHydrationYield));
            config.DairyHydrationYield = OnInputFloat(id, config.DairyHydrationYield, nameof(config.DairyHydrationYield));
            config.ProteinHydrationYield = OnInputFloat(id, config.ProteinHydrationYield, nameof(config.ProteinHydrationYield));
            config.GrainHydrationYield = OnInputFloat(id, config.GrainHydrationYield, nameof(config.GrainHydrationYield), -1);
            config.NoNutritionHydrationYield = OnInputFloat(id, config.NoNutritionHydrationYield, nameof(config.NoNutritionHydrationYield));
            config.UnknownHydrationYield = OnInputFloat(id, config.UnknownHydrationYield, nameof(config.UnknownHydrationYield));
            ImGui.Separator();
            config.DowsingRodRadius = OnInputInt(id, config.DowsingRodRadius, nameof(config.DowsingRodRadius));
        }
        if (ImGui.CollapsingHeader(Lang.Get(settingsAdvanced) + $"##settingAdvanced-{id}"))
        {
            ImGui.Indent();
            if (ImGui.CollapsingHeader(Lang.Get(settingPrefix + nameof(config.HeatableLiquidContainers)) + $"##settingHeatableContainers")) {
                config.HeatableLiquidContainers = OnInputList(id, config.HeatableLiquidContainers, nameof(config.HeatableLiquidContainers));
            }
            if (ImGui.CollapsingHeader(Lang.Get(settingPrefix + nameof(config.WaterPortions)) + $"##settingWaterPortions")) {
                config.WaterPortions = OnInputList(id, config.WaterPortions, nameof(config.WaterPortions));
            }
            if (ImGui.CollapsingHeader(Lang.Get(settingPrefix + nameof(config.WaterContainers)) + $"##settingWaterContainers")) {
                DictionaryEditor(config.WaterContainers, 1.0f, Lang.Get(textSupportsWildcard));
            }
            if (ImGui.CollapsingHeader(Lang.Get(settingPrefix + nameof(config.HydratingLiquids)) + $"##settingHydratingLiquids")) {
                DictionaryEditor(config.HydratingLiquids, new HydrationProperties(), Lang.Get(textSupportsWildcard));
            }
            if (ImGui.CollapsingHeader(Lang.Get(settingPrefix + nameof(config.HydratingBlocks)) + $"##settingHydratingBlocks"))
            {
                DictionaryEditor(config.HydratingBlocks, new HydrationProperties(), Lang.Get(textSupportsWildcard));
            }
            ImGui.Unindent();
        }
    }

    private bool OnCheckBox(string id, bool value, string name)
    {
        bool newValue = value;
        ImGui.Checkbox(Lang.Get(settingPrefix + name) + $"##{name}-{id}", ref newValue);
        return newValue;
    }

    private bool OnCheckBoxWithoutTranslation(string id, bool value, string name)
    {
        bool newValue = value;
        ImGui.Checkbox(name + $"##{name}-{id}", ref newValue);
        return newValue;
    }

    private int OnInputInt(string id, int value, string name, int minValue = default)
    {
        int newValue = value;
        ImGui.InputInt(Lang.Get(settingPrefix + name) + $"##{name}-{id}", ref newValue, step: 1, step_fast: 10);
        return newValue < minValue ? minValue : newValue;
    }

    private float OnInputFloat(string id, float value, string name, float minValue = default)
    {
        float newValue = value;
        ImGui.InputFloat(Lang.Get(settingPrefix + name) + $"##{name}-{id}", ref newValue, step: 0.01f, step_fast: 1.0f);
        return newValue < minValue ? minValue : newValue;
    }

    private string OnInputText(string id, string value, string name)
    {
        string newValue = value;
        ImGui.InputText(Lang.Get(settingPrefix + name) + $"##{name}-{id}", ref newValue, 64);
        return newValue;
    }
    
    private IEnumerable<string> OnInputTextMultiline(string id, IEnumerable<string> values, string name)
    {
        string newValue = values.Any() ? values.Aggregate((first, second) => $"{first}\n{second}") : "";
        ImGui.InputTextMultiline(Lang.Get(settingPrefix + name) + $"##{name}-{id}", ref newValue, 256, new(0, 0));
        return newValue.Split('\n', StringSplitOptions.RemoveEmptyEntries).AsEnumerable();
    }
    
    private T OnInputEnum<T>(string id, T value, string name) where T : Enum
    {
        string[] enumNames = Enum.GetNames(typeof(T));
        int index = Array.IndexOf(enumNames, value.ToString());

        if (ImGui.Combo(Lang.Get(settingPrefix + name) + $"##{name}-{id}", ref index, enumNames, enumNames.Length))
        {
            value = (T)Enum.Parse(typeof(T), enumNames[index]);
        }

        return value;
    }
    
    private List<string> OnInputList(string id, List<string> values, string name)
    {
        List<string> newValues = new List<string>(values);
        for (int i = 0; i < newValues.Count; i++)
        {
            string newValue = newValues[i];
            ImGui.InputText($"{name}[{i}]##{name}-{id}-{i}", ref newValue, 64);
            newValues[i] = newValue;
        }

        if (ImGui.Button($"Add##{name}-{id}"))
        {
            newValues.Add("");
        }

        return newValues;
    }
    
    private void DictionaryEditor<T>(Dictionary<string, T> dict, T defaultValue = default, string hint = "", string[] possibleValues = null)
    {
        if (ImGui.BeginTable("dict", 3, ImGuiTableFlags.BordersOuter))
        {
            for (int row = 0; row < dict.Count; row++)
            {
                ImGui.TableNextRow();
                string key = dict.Keys.ElementAt(row);
                string prevKey = (string)key.Clone();
                T value = dict.Values.ElementAt(row);
                ImGui.TableNextColumn();
                ImGui.InputTextWithHint($"##text-{row}", hint, ref key, 300);
                if (prevKey != key)
                {
                    dict.Remove(prevKey);
                    dict.TryAdd(key, value);
                    value = dict.Values.ElementAt(row);
                }
                ImGui.TableNextColumn();
                if (typeof(T) == typeof(int))
                {
                    int intValue = Convert.ToInt32(value);
                    ImGui.InputInt($"##int-{row}" + key, ref intValue);
                    value = (T)Convert.ChangeType(intValue, typeof(T));
                }
                else if (typeof(T) == typeof(float))
                {
                    float floatValue = Convert.ToSingle(value);
                    ImGui.InputFloat($"##float-{row}" + key, ref floatValue);
                    value = (T)Convert.ChangeType(floatValue, typeof(T));
                }
                else if (typeof(T) == typeof(bool))
                {
                    bool boolValue = Convert.ToBoolean(value);
                    ImGui.Checkbox($"##boolean-{row}" + key, ref boolValue);
                    value = (T)Convert.ChangeType(boolValue, typeof(T));
                }
                else if (typeof(T) == typeof(HydrationProperties))
                {
                    if (value is not HydrationProperties customValue) continue;
                    customValue.Hydration = OnInputFloat($"##float-{row}" + key, customValue.Hydration, nameof(HydrationProperties.Hydration));
                    customValue.HydrationLossDelay = OnInputFloat($"##float-{row}" + key, customValue.HydrationLossDelay, nameof(HydrationProperties.HydrationLossDelay));
                    customValue.Purity = OnInputEnum($"##purity-{row}" + key, customValue.Purity, nameof(HydrationProperties.Purity));
                    customValue.Scalding = OnCheckBoxWithoutTranslation($"##boolean-{row}" + key, customValue.Scalding, nameof(HydrationProperties.Scalding));
                    customValue.Salty = OnCheckBoxWithoutTranslation($"##boolean-{row}" + key, customValue.Salty, nameof(HydrationProperties.Salty));
                    value = (T)Convert.ChangeType(customValue, typeof(HydrationProperties));
                }
                dict[key] = value;
                ImGui.TableNextColumn();
                if (ImGui.Button($"Remove##row-value-{row}"))
                {
                    dict.Remove(key);
                }
            }
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Button("Add"))
            {
                int id = dict.Count;
                string newKey = possibleValues?.FirstOrDefault(x => !dict.ContainsKey(x)) ?? $"row {id}";
                while (dict.ContainsKey(newKey)) newKey = $"row {++id}";
                dict.TryAdd(newKey, defaultValue);
            }
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.EndTable();
        }
    }
}