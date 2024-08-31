using System;
using BalancedThirst.Systems;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace BalancedThirst.Hud;

public class DehydratedPerceptionEffect : PerceptionEffect
{
    private float currentStrength;
    private float strength;
    private int duration;
    private long bloomUntil;
    
    private readonly NormalizedSimplexNoise noiseGenerator;

    public DehydratedPerceptionEffect(ICoreClientAPI capi)
        : base(capi)
    {
        this.noiseGenerator = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, 123L);
    }

    public override void OnOwnPlayerDataReceived(EntityPlayer eplr)
    {
        eplr.WatchedAttributes.RegisterModifiedListener("drankSaltwater", new Action(this.OnDrank));
    }
    
    private void OnDrank()
    {
        EntityPlayer entity = this.capi.World.Player.Entity;
        this.strength = entity.WatchedAttributes.GetFloat("drankSaltwater", 0.0f);
        if ((double) this.strength == 0.0)
            return;
        this.duration = GameMath.Clamp(200 + (int) ((double) this.strength * 10.0), 200, 600) * 3;
        this.bloomUntil = this.capi.ElapsedMilliseconds + (long) this.duration;
    }
    public override void OnBeforeGameRender(float dt)
    {
        if (this.capi.IsGamePaused)
            return;
        this.HandleDrankEffects(Math.Min(dt, 1f));
    }
    
    private void ApplyDrankBloom(float elapsedSeconds, float healthThreshold)
    {
        int num1 = GameMath.Clamp((int) (this.bloomUntil - this.capi.ElapsedMilliseconds), 0, this.duration);
        float num2 = (float) (this.noiseGenerator.Noise(12412.0, (double) elapsedSeconds / 2.0) * 0.5 + Math.Pow((double) Math.Abs(GameMath.Sin((float) ((double) elapsedSeconds * 1.0 / 0.699999988079071))), 30.0) * 0.5);
        float num3 = Math.Min(healthThreshold * 1.5f, 1f) * (float) ((double) num2 * 0.75 + 0.5);
        this.capi.Render.ShaderUniforms.ExtraSepia += GameMath.Clamp(GameMath.Clamp(this.strength / 2f, 0.5f, 3.5f) * ((float) num1 / (float) Math.Max(1, this.duration)) + num3, 0.0f, 1.5f);
    }
    
    private float CalculateHealthThreshold()
    {
        ITreeAttribute treeAttribute = (this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(
            $"{BtCore.Modid}:thirst"));
        float? nullable1 = treeAttribute?.GetFloat("dehydration");
        float nullable2 = 10f;
        return Math.Max(0.0f, (float) ((0.23 - (double) (nullable1.HasValue ? new float?(nullable1.GetValueOrDefault() / nullable2) : new float?()).GetValueOrDefault(1f)) * 1.0 / 0.18));
    }

    private void HandleDrankEffects(float dt)
    {
        float healthThreshold = this.CalculateHealthThreshold();
        float elapsedSeconds = (float) this.capi.InWorldEllapsedMilliseconds / 1000f;
        //this.ApplyDrankBloom(elapsedSeconds, healthThreshold);
        this.ApplyBloom(this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(
            $"{BtCore.Modid}:thirst").GetFloat("dehydration"));
    }
    
    /*
    private void HandleFreezingEffects(float dt)
    {
        this.currentStrength += ((this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(
            $"{BtCore.Modid}:thirst")?.GetFloat("dehydration") ?? 0) - this.currentStrength) * dt;
        this.ApplyFrostVignette(this.currentStrength);
        if ((double) this.currentStrength <= 0.1 || this.capi.World.Player.CameraMode != EnumCameraMode.FirstPerson)
            return;
    }
    
    private void ApplyFrostVignette(float strength)
    {
        this.capi.Render.ShaderUniforms.FrostVignetting = 0.5f;
        var program = this.capi.ModLoader?.GetModSystem<BtCore>()?.ThirstShaderProgram;
        BtCore.Logger.Warning("Disposed? " + (program?.Disposed ?? true));
        if (program == null || program.Disposed) return;
        BtCore.Logger.Warning("Applying vignette");
        program.Use();
        program.Uniform("dehydrationVignetting", strength);
    }
    */


    private void ApplyBloom(float strength)
    {
        /*
        this.capi.Render.ShaderUniforms.ExtraBloom = Math.Min(strength, 10);
        var thirstShaderProgram = this.capi.ModLoader.GetModSystem<BtCore>().ThirstShaderProgram;
        IShaderProgram curShader = capi.Render.CurrentActiveShader;
        curShader?.Stop();
        thirstShaderProgram.Use();
        thirstShaderProgram?.Uniform("time", 100);
        thirstShaderProgram.Stop();
        curShader?.Use();
        */
    }
}