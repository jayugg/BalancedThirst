using System;
using BalancedThirst.Systems;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace BalancedThirst.Hud;

public class DehydratedPerceptionEffect : PerceptionEffect
{
    private float currentStrength;

    public DehydratedPerceptionEffect(ICoreClientAPI capi)
        : base(capi)
    {
    }

    public override void OnBeforeGameRender(float dt)
    {
        if (this.capi.IsGamePaused)
            return;
        this.HandleEffects(Math.Min(dt, 1f));
    }

    private void HandleEffects(float dt)
    {
        this.currentStrength = (this.capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute(
            $"{BtCore.Modid}:thirst")?.GetFloat("dehydration") ?? 0);
        this.ApplyVignette(this.currentStrength);
    }

    private void ApplyVignette(float strength)
    {
        this.capi.Render.ShaderUniforms.GlitchWaviness = strength < 5f ? 0 : Math.Min((strength - 5) / 100f, 0.3f);
        this.capi.Render.ShaderUniforms.ExtraBloom = Math.Min(strength / 60f, 0.5f);
    }
}