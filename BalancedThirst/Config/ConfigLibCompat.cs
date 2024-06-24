using System;
using System.Collections.Generic;
using System.Linq;
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
            config.ThirstSpeedModifier = OnInputFloat(id, config.ThirstSpeedModifier, nameof(config.ThirstSpeedModifier));
            config.ThirstKills = OnCheckBox(id, config.ThirstKills, nameof(config.ThirstKills));
            config.ThirstHungerMultiplier = OnInputFloat(id, config.ThirstHungerMultiplier, nameof(config.ThirstHungerMultiplier));
            config.HungerBuffCurve = OnInputEnum(id, config.HungerBuffCurve, nameof(config.HungerBuffCurve));
            config.LowerHalfHungerBuffCurve = OnInputEnum(id, config.LowerHalfHungerBuffCurve, nameof(config.LowerHalfHungerBuffCurve));
            ImGui.Separator();
            config.VomitHydrationMultiplier = OnInputFloat(id, config.VomitHydrationMultiplier, nameof(config.VomitHydrationMultiplier));
            config.VomitEuhydrationMultiplier = OnInputFloat(id, config.VomitEuhydrationMultiplier, nameof(config.VomitEuhydrationMultiplier));
            ImGui.Separator();
            config.PurePurityLevel = OnInputFloat(id, config.PurePurityLevel, nameof(config.PurePurityLevel));
            config.FilteredPurityLevel = OnInputFloat(id, config.FilteredPurityLevel, nameof(config.FilteredPurityLevel));
            config.BoiledPurityLevel = OnInputFloat(id, config.BoiledPurityLevel, nameof(config.BoiledPurityLevel));
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
}