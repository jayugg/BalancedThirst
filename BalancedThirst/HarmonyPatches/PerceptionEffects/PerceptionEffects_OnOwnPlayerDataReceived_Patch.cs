using System.Linq;
using BalancedThirst.Hud;
using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BalancedThirst.HarmonyPatches.PerceptionEffects;

public class PerceptionEffects_OnOwnPlayerDataReceived_Patch
{
    public static void Prefix(Vintagestory.API.Client.PerceptionEffects __instance, EntityPlayer eplr)
    {
        BtCore.Logger.Warning("PerceptionEffects_OnOwnPlayerDataReceived_Patch.Prefix");
        if (!__instance.RegisteredEffects.Contains(BtConstants.DehydratedEffectId))
        {
           __instance.RegisterPerceptionEffect(new DehydratedPerceptionEffect(__instance.GetField<ICoreClientAPI>("capi")), BtConstants.DehydratedEffectId);
        }
        BtCore.Logger.Warning(__instance.RegisteredEffects.Aggregate("Registered Effects: ", (current, effect) => current + (effect + ", ")));
        __instance.TriggerEffect(BtConstants.DehydratedEffectId, 1f, true);
    }
}