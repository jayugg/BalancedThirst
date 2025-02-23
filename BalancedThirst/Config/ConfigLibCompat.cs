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
public partial class ConfigLibCompat
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
        if (api.Side == EnumAppSide.Client)
            api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig(Lang.Get("balancedthirst:balancedthirst_client"), (id, buttons) => EditConfigClient(id, buttons, api));
    }
    
    private void EditConfigClient(string id, ControlButtons buttons, ICoreAPI api)
    {
        if (buttons.Save) ModConfig.WriteConfig(api, BtConstants.ConfigClientName, ConfigSystem.ConfigClient);
        if (buttons.Restore) ConfigSystem.ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, BtConstants.ConfigClientName);
        if (buttons.Defaults) ConfigSystem.ConfigClient = new(api);
        BuildSettingsClient(ConfigSystem.ConfigClient, id);
    }
    
    private void BuildSettingsClient(ConfigClient config, string id)
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

    private void EditConfigServer(string id, ControlButtons buttons, ICoreAPI api)
    {
        if (buttons.Save) ModConfig.WriteConfig(api, BtConstants.ConfigServerName, ConfigSystem.ConfigServer);
        if (buttons.Restore) ConfigSystem.ConfigServer = ModConfig.ReadConfig<ConfigServer>(api, BtConstants.ConfigServerName);
        if (buttons.Defaults) ConfigSystem.ConfigServer = new(api);
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
            if (ImGui.CollapsingHeader(Lang.Get(settingPrefix + nameof(config.DynamicWaterPurityWeights)) + $"##settingsDynamicWaterPurityWeights-{id}"))
            {
                ImGui.Indent();
                DictionaryEditor(config.DynamicWaterPurityWeights, 0.2f);
                ImGui.Unindent();
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
    }
}