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

    public ConfigLibCompat(ICoreAPI api)
    {
        api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig(Lang.Get("balancedthirst:balancedthirst"), (id, buttons) => EditConfigServer(id, buttons, api));
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
            config.ThirstHungerMultiplier = OnInputFloat(id, config.ThirstHungerMultiplier, nameof(config.ThirstHungerMultiplier));
            config.VomitHydrationMultiplier = OnInputFloat(id, config.VomitHydrationMultiplier, nameof(config.VomitHydrationMultiplier));
            config.VomitEuhydrationMultiplier = OnInputFloat(id, config.VomitEuhydrationMultiplier, nameof(config.VomitEuhydrationMultiplier));
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
}