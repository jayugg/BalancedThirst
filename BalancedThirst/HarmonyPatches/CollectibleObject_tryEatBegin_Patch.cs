using System;
using System.Text;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace BalancedThirst.HarmonyPatches;

public class CollectibleObject_tryEatBegin_Patch
{
    public static void Postfix(
        ItemSlot slot,
        EntityAgent byEntity,
        ref EnumHandHandling handling,
        string eatSound = "eat",
        int eatSoundRepeats = 1)
    {
        var collObj = slot.Itemstack.Collectible;
        HydrationProperties hydrationProperties = collObj.GetHydrationProperties(slot.Itemstack, byEntity);
        //BtCore.Logger.Warning(hydrationProperties?.Hydration.ToString());
        if (slot.Empty) return;
        byEntity.World.RegisterCallback(_ => DrinkableBehavior.PlayDrinkSound(byEntity, eatSoundRepeats), 500);
        byEntity.AnimManager?.StartAnimation("eat");
        if (byEntity is EntityPlayer { Player: IClientPlayer clientPlayer })
        {
            clientPlayer.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
        }
        handling = EnumHandHandling.PreventDefault;
    }
}