using BalancedThirst.ModBehavior;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_tryEatBegin_Patch
{
    public static void Postfix(
        ItemSlot slot,
        EntityAgent byEntity,
        ref EnumHandHandling handling,
        string eatSound = "eat",
        int eatSoundRepeats = 1)
    {
        if (slot.Empty) return;
        var collObj = slot.Itemstack?.Collectible;
        HydrationProperties hydrationProperties = collObj?.GetHydrationProperties(slot.Itemstack, byEntity);
        if (hydrationProperties == null) return;
        byEntity.World.RegisterCallback(_ => DrinkableBehavior.PlayDrinkSound(byEntity, eatSoundRepeats), 500);
        byEntity.AnimManager?.StartAnimation("eat");
        if (byEntity is EntityPlayer { Player: IClientPlayer clientPlayer })
        {
            clientPlayer.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
        }
        handling = EnumHandHandling.PreventDefault;
    }
}