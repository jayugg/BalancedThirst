using BalancedThirst.ModBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace BalancedThirst.HarmonyPatches;

public class CollectibleObject_tryEatStop_Patch
{
    public static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        var collObj = slot.Itemstack.Collectible;
        HydrationProperties hydrationProperties = collObj.GetHydrationProperties(slot.Itemstack, byEntity);
        if (!(byEntity.World is IServerWorldAccessor) || !byEntity.HasBehavior<EntityBehaviorThirst>() || hydrationProperties == null || secondsUsed < 0.949999988079071)
            return;
        TransitionState transitionState = collObj.UpdateAndGetTransitionState(byEntity.Api.World, slot, EnumTransitionType.Perish);
        double spoilState = transitionState?.TransitionLevel ?? 0.0;
        float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
        hydrationProperties.Hydration *= num1;
        hydrationProperties.HydrationLossDelay *= num1;
        byEntity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProperties);
    }
}