using System;
using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.BlockLiquidContainer;

public class BlockLiquidContainerBase_TryFillFromBlock_Patch
{
    public static void Postfix(
        bool __result,
        BlockLiquidContainerBase __instance,
        ItemSlot itemslot,
        EntityAgent byEntity,
        BlockPos pos
        )
    {
        if (ConfigSystem.SyncedConfigData.DynamicWaterPurity == false) return;
        if (__result == false) return;
        if (byEntity is not EntityPlayer player) return;
        var contentStack = __instance.GetContent(itemslot.Itemstack);
        if (!contentStack?.Collectible?.Code?.BeginsWith("game", "waterportion") ?? true) return;
        if (byEntity.Api is not ICoreServerAPI sapi) return;
        var waterCode = GetWaterCodeForPosition(pos, sapi);
        if (!waterCode.Contains("stagnant") && itemslot.Itemstack.Collectible.GetToolMode(itemslot, player, pos) == WaterContainerBehavior.DirtyWaterMode) return;
        var newContentStack = new ItemStack(byEntity.World.GetItem(new AssetLocation(waterCode)))
        {
            StackSize = contentStack.StackSize,
            Attributes = contentStack.Attributes
        };
        __instance.SetContent(itemslot.Itemstack, newContentStack);
        itemslot.MarkDirty();
    }

    public static string GetWaterCodeForPosition(BlockPos pos, ICoreServerAPI sapi)
    {
        float worldHeight = sapi.WorldManager.MapSizeY;
        var climate = sapi.World.BlockAccessor.GetClimateAt(pos);
        var block = sapi.World.BlockAccessor.GetBlock(pos);
        var waterfall = block is BlockWaterfall or BlockWaterflowing;
        var flowingWater = pos.IsRiverBlock(sapi.World) ? 0.9f : waterfall ? 0.3f : 0f;
        var purity = CalculateWaterPurity(
            climate.Temperature,
            climate.Rainfall,
            climate.Fertility,
            climate.ForestDensity,
            climate.GeologicActivity,
            pos.Y / worldHeight,
            flowingWater
            );
        /*
        BtCore.Logger.Warning($"Water purity at {pos} is {purity}");
        BtCore.Logger.Warning($"Temperature: {climate.Temperature}");
        BtCore.Logger.Warning($"Rainfall: {climate.Rainfall}");
        BtCore.Logger.Warning($"Fertility: {climate.Fertility}");
        BtCore.Logger.Warning($"Forest Density: {climate.ForestDensity}");
        BtCore.Logger.Warning($"Geologic Activity: {climate.GeologicActivity}");
        BtCore.Logger.Warning($"Altitude: {pos.Y / worldHeight}");
        BtCore.Logger.Warning($"Flowing Water: {flowingWater}");
        */
        return purity switch {
            <= 0.2f => $"{BtCore.Modid}:waterportion-stagnant",
            <= 0.7f => $"game:waterportion",
            <= 0.9f => $"{BtCore.Modid}:waterportion-potable",
            _ => $"{BtCore.Modid}:waterportion-pure"
        };
    }
    
    private static float CalculateWaterPurity(
        float temp, 
        float rainfall, 
        float fertility, 
        float forestDensity, 
        float geologicActivity, 
        float altitude,
        float flowingWater
        )
    {
        var weights = ConfigSystem.ConfigServer.DynamicWaterPurityWeights;

        // Calculate purity for each factor
        float tPurity = CalculateTemperatureEffect(temp);
        float rPurity = CalculateRainfallEffect(rainfall);
        float fPurity = CalculateFertilityEffect(fertility);
        float fdPurity = CalculateForestDensityEffect(forestDensity);
        float gPurity = CalculateGeologicActivityEffect(geologicActivity);
        float aPurity = CalculateAltitudeEffect(altitude);
        float fwPurity = flowingWater;
        
        // Calculate weighted average
        float purity =
            weights["temperature"] * tPurity +
            weights["rainfall"] * rPurity +
            weights["fertility"] * fPurity +
            weights["forestDensity"] * fdPurity +
            weights["geologicActivity"] * gPurity +
            weights["altitude"] * aPurity +
            weights["flowingWater"] * fwPurity;

        return purity;
    }
    
    private static float CalculateTemperatureEffect(float temp)
    {
        // Cool temperatures are generally safe
        return temp switch
        {
            // Temperatures <= 0 are considered safe because they inhibit microbial growth
            <= 0 => 1.0f,
            // Temperatures between 0 and 10 increase risk slightly due to potential microbial activity
            <= 10 => Math.Max(0.7f, 1.0f - temp / 10.0f), 
            // Temperatures between 10 and 30 increase risk moderately as they are more conducive to microbial growth
            <= 30 => Math.Max(0.5f, 1.0f - (temp - 10) / 20.0f),
            // Temperatures between 30 and 60 increase risk significantly due to optimal conditions for microbial proliferation
            <= 60 => Math.Max(0.3f, 1.0f - (temp - 30) / 30.0f),
            // Temperatures > 60 are considered safer due to boiling, which kills most microbes
            _ => Math.Min(1.0f, (temp - 60) / 40.0f + 0.7f)
        };
    }

    private static float CalculateRainfallEffect(float rainfall)
    {
        // Low rainfall leads to scarcity and pollutant concentration
        return rainfall switch
        {
            // Rainfall < 0.1 is considered low
            < 0.1f => 0.3f,
            // Optimal rainfall dilutes contaminants
            <= 0.8f => 1.0f - (float)Math.Pow((rainfall - 0.5f) / 0.3f, 2),
            // Excessive rainfall causes runoff and contamination
            _ => Math.Max(0.2f, 1.0f - (float)Math.Pow((rainfall - 0.8f) / 0.2f, 2))
        };
    }
    
    private static float CalculateForestDensityEffect(float forestDensity)
    {
        return forestDensity < 0.8f ?
            // Higher forest density generally increases purity
            forestDensity :
            // Minor diminishing returns due to organic decay at very high density
            Math.Min(1.0f, 0.8f + (forestDensity - 0.8f) / 2.5f);
    }
    
    private static float CalculateAltitudeEffect(float altitude)
    {
        // Linearly increasing effect based on normalized altitude (0-1)
        return altitude;
    }
    
    private static float CalculateGeologicActivityEffect(float geologicActivity)
    {
        // Mean and standard deviation of the Gaussian curve
        float mu = 0.5f; // Moderate geologic activity is ideal for water quality
        float sigma = 0.2f; // Spread around the mean
        // Calculate the Gaussian function
        return (float)Math.Exp(-Math.Pow((geologicActivity - mu), 2) / (2 * Math.Pow(sigma, 2)));
    }
    
    private static float CalculateFertilityEffect(float fertility)
    {
        float maxFertility = 100.0f; // Assuming fertility is measured on a 0-100 scale
        // Higher fertility reduces purity
        return Math.Max(0.1f, 1.0f - (fertility / maxFertility));
    }
}