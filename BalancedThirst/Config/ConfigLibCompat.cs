using System;
using System.Collections.Generic;
using System.Linq;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using ConfigLib;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BalancedThirst.Config;

// From https://github.com/Craluminum-Mods/DanaTweaks/
public class ConfigLibCompat
{
    private const string settingPrefix = "balancedthirst:Config.Setting.";
    
    private const string settingsSimple = "balancedthirst:Config.SettingsSimple";
    private const string settingsStatMultipliers = "balancedthirst:Config.SettingsStatMultipliers";
    private const string settingsAdvanced = "balancedthirst:Config.SettingsAdvanced";
    private const string settingsCompat = "balancedthirst:Config.SettingsCompat";
    private const string textSupportsWildcard = "balancedthirst:Config.Text.SupportsWildcard";
    private const string textSupportsHex = "balancedthirst:Config.Text.SupportsHex";

    public ConfigLibCompat(ICoreAPI api)
    {
        if (api.Side == EnumAppSide.Server || api is ICoreClientAPI { IsSinglePlayer: true })
            api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig(Lang.Get("balancedthirst:balancedthirst"), (id, buttons) => EditConfigServer(id, buttons, api));
        else api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig(Lang.Get("balancedthirst:balancedthirst_sync"), (id, buttons) => EditConfigSync(id, buttons, api));
        if (api.Side == EnumAppSide.Client)
            api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig(Lang.Get("balancedthirst:balancedthirst_client"), (id, buttons) => EditConfigClient(id, buttons, api));
    }
    
    private static void SyncConfig(ICoreAPI api)
    {
        if (api is ICoreClientAPI { IsSinglePlayer: true } capi && ConfigSystem.ConfigServer.ResetModBoosts)
        {
            ConfigSystem.ResetModBoosts(capi.World?.Player?.Entity);
            ConfigSystem.ConfigServer.ResetModBoosts = false;
            ModConfig.WriteConfig(capi, BtConstants.ConfigServerName, ConfigSystem.ConfigServer);
        }
        api.Event.PushEvent(EventIds.ConfigReloaded);
    }
    
    private static void SyncConfigAdmin(ICoreAPI api)
    {
        BtCore.Logger.Warning("Sending synced config from admin");
        api.Event.PushEvent(EventIds.AdminSetConfig);
    }
    
    private void EditConfigClient(string id, ControlButtons buttons, ICoreAPI api)
    {
        if (buttons.Save) ModConfig.WriteConfig(api, BtConstants.ConfigClientName, ConfigSystem.ConfigClient);
        if (buttons.Restore) ConfigSystem.ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, BtConstants.ConfigClientName);
        if (buttons.Defaults) ConfigSystem.ConfigClient = new(api);

        if (buttons.Save || buttons.Restore || buttons.Defaults) SyncConfig(api); 
        BuildSettingsClient(ConfigSystem.ConfigClient, id);
    }
    
    private void BuildSettingsClient(ConfigClient config, string id)
    {
        if (ImGui.CollapsingHeader(Lang.Get(settingsSimple) + $"##settingSimple-{id}"))
        {
            config.ThirstBarX = OnInputFloat(id, config.ThirstBarX, nameof(config.ThirstBarX), -float.MaxValue);
            config.ThirstBarY = OnInputFloat(id, config.ThirstBarY, nameof(config.ThirstBarY), -float.MaxValue);
            ImGui.Separator();
            config.PeeMode = OnInputEnum(id, config.PeeMode, nameof(config.PeeMode));
            config.BladderBarVisible = OnCheckBox(id, config.BladderBarVisible, nameof(config.BladderBarVisible));
            config.HideBladderBarAt = OnInputFloat(id, config.HideBladderBarAt, nameof(config.HideBladderBarAt));
            ImGui.Separator();
            config.ThirstBarColor = OnInputHex(id, config.ThirstBarColor, nameof(config.ThirstBarColor));
            config.BladderBarColor = OnInputHex(id, config.BladderBarColor, nameof(config.BladderBarColor));
            config.UrineColor = OnInputText(id, config.UrineColor, nameof(config.UrineColor));
        }
    }

    private void EditConfigSync(string id, ControlButtons buttons, ICoreAPI api)
    {
        if (buttons.Save) ModConfig.WriteConfig(api, BtConstants.SyncedConfigName, ConfigSystem.SyncedConfig);
        if (buttons.Restore) ConfigSystem.SyncedConfig = ModConfig.ReadConfig<SyncedConfig>(api, BtConstants.SyncedConfigName);
        if (buttons.Defaults) ConfigSystem.SyncedConfig = new(api);
        
        if (buttons.Save || buttons.Restore || buttons.Defaults) SyncConfigAdmin(api); 
        BuildSettingsSync(ConfigSystem.SyncedConfig, id);
    }
    
    private void BuildSettingsSync(SyncedConfig config, string id)
    {
        if (ImGui.CollapsingHeader(Lang.Get(settingsSimple) + $"##settingSimple-{id}"))
        {
            config.EnableThirst = OnCheckBox(id, config.EnableThirst, nameof(config.EnableThirst));
            config.EnableBladder = OnCheckBox(id, config.EnableBladder, nameof(config.EnableBladder));
            ImGui.Separator();
            config.SpillWashStains = OnCheckBox(id, config.SpillWashStains, nameof(config.SpillWashStains));
            config.UrineStains = OnCheckBox(id, config.UrineStains, nameof(config.UrineStains));
            config.ContainerDrinkSpeed = OnInputFloat(id, config.ContainerDrinkSpeed, nameof(config.ContainerDrinkSpeed));
            config.FruitHydrationYield = OnInputFloat(id, config.FruitHydrationYield, nameof(config.FruitHydrationYield));
            config.VegetableHydrationYield = OnInputFloat(id, config.VegetableHydrationYield, nameof(config.VegetableHydrationYield));
            config.DairyHydrationYield = OnInputFloat(id, config.DairyHydrationYield, nameof(config.DairyHydrationYield));
            config.ProteinHydrationYield = OnInputFloat(id, config.ProteinHydrationYield, nameof(config.ProteinHydrationYield));
            config.GrainHydrationYield = OnInputFloat(id, config.GrainHydrationYield, nameof(config.GrainHydrationYield), -1);
            config.NoNutritionHydrationYield = OnInputFloat(id, config.NoNutritionHydrationYield, nameof(config.NoNutritionHydrationYield));
            config.UnknownHydrationYield = OnInputFloat(id, config.UnknownHydrationYield, nameof(config.UnknownHydrationYield));
            ImGui.Separator();
            config.DowsingRodRadius = OnInputFloat(id, config.DowsingRodRadius, nameof(config.DowsingRodRadius));
        }
    }

    private void EditConfigServer(string id, ControlButtons buttons, ICoreAPI api)
    {
        if (buttons.Save) ModConfig.WriteConfig(api, BtConstants.ConfigServerName, ConfigSystem.ConfigServer);
        if (buttons.Restore) ConfigSystem.ConfigServer = ModConfig.ReadConfig<ConfigServer>(api, BtConstants.ConfigServerName);
        if (buttons.Defaults) ConfigSystem.ConfigServer = new(api);
        
        if (buttons.Save || buttons.Restore || buttons.Defaults) SyncConfig(api); 
        BuildSettingsServer(ConfigSystem.ConfigServer, id);
    }
    
    private void BuildSettingsServer(ConfigServer config, string id)
    {
        if (ImGui.CollapsingHeader(Lang.Get(settingsSimple) + $"##settingSimple-{id}"))
        {
            config.EnableThirst = OnCheckBox(id, config.EnableThirst, nameof(config.EnableThirst));
            config.MaxHydration = OnInputFloat(id, config.MaxHydration, nameof(config.MaxHydration));
            config.ThirstKills = OnCheckBox(id, config.ThirstKills, nameof(config.ThirstKills));
            config.ThirstSpeedModifier = OnInputFloat(id, config.ThirstSpeedModifier, nameof(config.ThirstSpeedModifier));
            config.ContainerDrinkSpeed = OnInputFloat(id, config.ContainerDrinkSpeed, nameof(config.ContainerDrinkSpeed));
            config.HotTemperatureThreshold = OnInputFloat(id, config.HotTemperatureThreshold, nameof(config.HotTemperatureThreshold));
            config.EnableDehydration = OnCheckBox(id, config.EnableDehydration, nameof(config.EnableDehydration));
            config.VomitHydrationMultiplier = OnInputFloat(id, config.VomitHydrationMultiplier, nameof(config.VomitHydrationMultiplier));
            config.VomitEuhydrationMultiplier = OnInputFloat(id, config.VomitEuhydrationMultiplier, nameof(config.VomitEuhydrationMultiplier));
            ImGui.Separator();
            config.EnableBladder = OnCheckBox(id, config.EnableBladder, nameof(config.EnableBladder));
            config.SpillWashStains = OnCheckBox(id, config.SpillWashStains, nameof(config.SpillWashStains));
            config.UrineStains = OnCheckBox(id, config.UrineStains, nameof(config.UrineStains));
            config.BladderWalkSpeedDebuff = OnInputFloat(id, config.BladderWalkSpeedDebuff, nameof(config.BladderWalkSpeedDebuff));
            config.BladderCapacityOverload = OnInputFloat(id, config.BladderCapacityOverload, nameof(config.BladderCapacityOverload));
            config.UrineNutrientChance = OnInputFloat(id, config.UrineNutrientChance, nameof(config.UrineNutrientChance));
            config.UrineDrainRate = OnInputFloat(id, config.UrineDrainRate, nameof(config.UrineDrainRate));
            DisplayEnumFloatDictionary(config.UrineNutrientLevels, nameof(config.UrineNutrientLevels), id);
            ImGui.Separator();
            if (ImGui.CollapsingHeader(Lang.Get(settingsStatMultipliers) + $"##settingStatMultipliers-{id}"))
            {
                ImGui.Indent();
                DictionaryEditor(config.ThirstStatMultipliers, new StatMultiplier());
                ImGui.Unindent();
            }
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
            config.DowsingRodRadius = OnInputFloat(id, config.DowsingRodRadius, nameof(config.DowsingRodRadius));
            config.GushingSpringWater = OnCheckBox(id, config.GushingSpringWater, nameof(config.GushingSpringWater));
        }
        if (ImGui.CollapsingHeader(Lang.Get(settingsAdvanced) + $"##settingAdvanced-{id}"))
        {
            ImGui.Indent();
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
            if (ImGui.CollapsingHeader(Lang.Get(settingPrefix + nameof(config.UrineStainableMaterials)) + $"##settingUrineStainableMaterials"))
            {
                config.UrineStainableMaterials = OnInputList(id, config.UrineStainableMaterials, nameof(config.UrineStainableMaterials));
            }
            ImGui.Unindent();
        }
        if (ImGui.CollapsingHeader(Lang.Get(settingsCompat) + $"##settingCompat-{id}"))
        {
            ImGui.Indent();
            config.HoDClothingCoolingMultiplier = OnInputFloat(id, config.HoDClothingCoolingMultiplier, nameof(config.HoDClothingCoolingMultiplier));
            config.CamelHumpMaxHydrationMultiplier = OnInputFloat(id, config.CamelHumpMaxHydrationMultiplier, nameof(config.CamelHumpMaxHydrationMultiplier));
            config.ElephantBladderCapacityMultiplier = OnInputFloat(id, config.ElephantBladderCapacityMultiplier, nameof(config.ElephantBladderCapacityMultiplier));
            ImGui.Unindent();
        }
        ImGui.Separator();
        config.ResetModBoosts = OnCheckBox(id, config.ResetModBoosts, nameof(config.ResetModBoosts));
    }

    private bool OnCheckBox(string id, bool value, string name, bool isDisabled = false)
    {
        bool newValue = value && !isDisabled;
        if (isDisabled)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
        }
        if (ImGui.Checkbox(Lang.Get(settingPrefix + name) + $"##{name}-{id}", ref newValue))
        {
            if (isDisabled)
            {
                newValue = value;
            }
        }
        if (isDisabled)
        {
            ImGui.PopStyleVar();
        }
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
    
    private double OnInputDouble(string id, double value, string name, double minValue = default)
    {
        double newValue = value;
        ImGui.InputDouble(Lang.Get(settingPrefix + name) + $"##{name}-{id}", ref newValue, step: 0.01f, step_fast: 1.0f);
        return newValue < minValue ? minValue : newValue;
    }

    private string OnInputText(string id, string value, string name)
    {
        string newValue = value;
        ImGui.InputText(Lang.Get(settingPrefix + name) + $"##{name}-{id}", ref newValue, 64);
        return newValue;
    }
    
    private string OnInputHex(string id, string value, string name)
    {
        string newValue = value;
        ImGui.InputTextWithHint(Lang.Get(settingPrefix + name) + $"##{name}-{id}", textSupportsHex,ref newValue, 64);
        if (string.IsNullOrEmpty(newValue) || !value.StartsWith("#") ||
            (newValue.Length != 7 && newValue.Length != 9)) return value;
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
    
    private List<T> OnInputList<T>(string id, List<T> values, string name) where T : struct, Enum
    {
        List<T> newValues = new List<T>(values);
        for (int i = 0; i < newValues.Count; i++)
        {
            string newValue = newValues[i].ToString();
            ImGui.InputText($"{name}[{i}]##{name}-{id}-{i}", ref newValue, 64);
            if (Enum.TryParse(newValue, out T parsedValue))
            {
                newValues[i] = parsedValue;
            }
        }

        if (ImGui.Button($"Add##{name}-{id}"))
        {
            newValues.Add(default);
        }

        return newValues;
    }
    
    private void DictionaryEditor<T>(Dictionary<string, T> dict, T defaultValue = default, string hint = "", string[] possibleValues = null)
    {
        if (ImGui.BeginTable("dict", 2, ImGuiTableFlags.BordersOuter))
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
                    customValue.EuhydrationWeight = OnInputFloat($"##float-{row}" + key, customValue.EuhydrationWeight, nameof(HydrationProperties.EuhydrationWeight));
                    customValue.Dehydration = OnInputFloat($"##float-{row}" + key, customValue.Dehydration, nameof(HydrationProperties.Dehydration));
                    value = (T)Convert.ChangeType(customValue, typeof(HydrationProperties));
                }
                else if (typeof(T) == typeof(StatMultiplier))
                {
                    if (value is not StatMultiplier customValue) continue;
                    customValue.Multiplier = OnInputFloat($"##float-{row}" + key, customValue.Multiplier, nameof(StatMultiplier.Multiplier));
                    customValue.Centering = OnInputEnum($"##centering-{row}" + key, customValue.Centering, nameof(StatMultiplier.Centering));
                    customValue.Curve = OnInputEnum($"##curve-{row}" + key, customValue.Curve, nameof(StatMultiplier.Curve));
                    customValue.LowerHalfCurve = OnInputEnum($"##lowerhalf-{row}" + key, customValue.LowerHalfCurve, nameof(StatMultiplier.LowerHalfCurve));
                    customValue.Inverted = OnCheckBoxWithoutTranslation($"##boolean-{row}" + key, customValue.Inverted, nameof(StatMultiplier.Inverted));
                    value = (T)Convert.ChangeType(customValue, typeof(StatMultiplier));
                }
                dict[key] = value;
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
            ImGui.EndTable();
        }
    }
    
    private void DisplayEnumFloatDictionary<T>(Dictionary<T, float> dictionary, string name, string id) where T : Enum
    {
        if (ImGui.CollapsingHeader(Lang.Get(settingPrefix + name) + $"##dictEnumFloat-{id}"))
        {
            ImGui.Indent();
            foreach (var pair in dictionary)
            {
                T key = pair.Key;
                float value = pair.Value;

                ImGui.Text(key.ToString());
                ImGui.SameLine();
                ImGui.InputFloat($"##{key}", ref value);
                
                dictionary[key] = value;
            }
            ImGui.Unindent();
        }
    }
}