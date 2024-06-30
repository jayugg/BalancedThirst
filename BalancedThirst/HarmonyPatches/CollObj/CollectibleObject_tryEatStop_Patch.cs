using BalancedThirst.ModBehavior;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace BalancedThirst.HarmonyPatches.CollObj;

public class CollectibleObject_tryEatStop_Patch
{
    static bool alreadyCalled = false;
    static EntityPlayer playerCalled; 
    public static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        var collObj = slot?.Itemstack?.Collectible;
        HydrationProperties hydrationProperties = collObj.GetHydrationProperties(slot.Itemstack, byEntity);
        if (byEntity.World is not IServerWorldAccessor ||
            !byEntity.HasBehavior<EntityBehaviorThirst>() ||
            hydrationProperties == null ||
            secondsUsed < 0.949999988079071)
            return;
        if (alreadyCalled && playerCalled == byEntity)
        {
            byEntity.World.RegisterCallback(_ =>
            {
                alreadyCalled = false;
                playerCalled = null;
            }, 300);
            return;
        }
        TransitionState transitionState = collObj.UpdateAndGetTransitionState(byEntity.Api.World, slot, EnumTransitionType.Perish);
        double spoilState = transitionState?.TransitionLevel ?? 0.0;
        float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
        hydrationProperties.Hydration *= num1;
        hydrationProperties.HydrationLossDelay *= num1;
        byEntity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProperties);
        alreadyCalled = true;
        playerCalled = byEntity as EntityPlayer;
    }
}