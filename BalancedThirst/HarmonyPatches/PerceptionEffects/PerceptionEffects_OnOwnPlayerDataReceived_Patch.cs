using BalancedThirst.Hud;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BalancedThirst.HarmonyPatches.PerceptionEffects;

public class PerceptionEffects_OnOwnPlayerDataReceived_Patch
{
    public static void Prefix(Vintagestory.API.Client.PerceptionEffects __instance, EntityPlayer eplr)
    {
        if (!__instance.RegisteredEffects.Contains(BtConstants.DehydratedEffectId))
        {
           __instance.RegisterPerceptionEffect(new DehydratedPerceptionEffect(__instance.GetField<ICoreClientAPI>("capi")), BtConstants.DehydratedEffectId);
        }
        __instance.TriggerEffect(BtConstants.DehydratedEffectId, 1f, true);
    }
}