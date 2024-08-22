using System.Collections.Generic;
using BalancedThirst.Hud;
using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Client;

namespace BalancedThirst.HarmonyPatches.PerceptionEffects;

public class PerceptionEffects_Constructor_Patch
{
    public static void Postfix(Vintagestory.API.Client.PerceptionEffects __instance, ICoreClientAPI capi)
    {
        BtCore.Logger.Warning("PerceptionEffects_Constructor_Patch.Postfix");
        __instance.RegisterPerceptionEffect(new DehydratedPerceptionEffect(capi), BtConstants.DehydratedEffectId);
    }
}