using System;
using BalancedThirst.ModBehavior;
using BalancedThirst.Thirst;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_tryEatStep_Patch
{
    public static void Postfix(
        ref bool __result,
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        ItemStack spawnParticleStack = null)
    {
        HydrationProperties hydrationProperties = slot.Itemstack.Collectible.GetHydrationProperties(slot.Itemstack, byEntity);
        if (hydrationProperties == null) return;
        Vec3d xyz = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
        xyz.X += byEntity.LocalEyePos.X;
        xyz.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
        xyz.Z += byEntity.LocalEyePos.Z;
        if (secondsUsed > 0.5 && (int) (30.0 * secondsUsed) % 7 == 1)
            byEntity.World.SpawnCubeParticles(xyz, spawnParticleStack ?? slot.Itemstack, 0.3f, 4, 0.5f, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
        if (byEntity.World is not IClientWorldAccessor)
            __result = true;
        ModelTransform modelTransform = new ModelTransform();
        modelTransform.EnsureDefaultValues();
        modelTransform.Origin.Set(0.0f, 0.0f, 0.0f);
        if (secondsUsed > 0.5)
            modelTransform.Translation.Y = Math.Min(0.02f, GameMath.Sin(20f * secondsUsed) / 10f);
        modelTransform.Translation.X -= Math.Min(1f, (float) (secondsUsed * 4.0 * 1.5700000524520874));
        modelTransform.Translation.Y -= Math.Min(0.05f, secondsUsed * 2f);
        modelTransform.Rotation.X += Math.Min(30f, secondsUsed * 350f);
        modelTransform.Rotation.Y += Math.Min(80f, secondsUsed * 350f);
        __result = secondsUsed <= 1.0;
    }
}