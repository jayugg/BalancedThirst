using System.Collections.Generic;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BalancedThirst.HarmonyPatches.Meal;

public class BlockMeal_tryFinishEatMeal_Patch
{
    private static bool ShouldSkipPatch => !ConfigSystem.SyncedConfigData.EnableThirst;
    
    static bool _alreadyCalled = false;
    private static Dictionary<string, HydrationProperties> _capturedProperties = new();
    private static Dictionary<string, float> _capturedServings = new();
    
    public static void Prefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity, bool handleAllServingsConsumed)
    {
        if (ShouldSkipPatch) return;
        _alreadyCalled = false;
        var api = byEntity?.World?.Api;
        if (api is not { Side: EnumAppSide.Server } || slot?.Itemstack == null) return;
        var collObj = slot.Itemstack.Collectible;
        if (collObj is not Vintagestory.GameContent.BlockMeal meal || !(secondsUsed >= 0.95f)) return;
        HydrationProperties hydrationProps = meal.GetHydrationProperties(byEntity.World, slot.Itemstack, byEntity);
        if (hydrationProps == null || byEntity is not EntityPlayer player) return;
        TransitionState transitionState = collObj.UpdateAndGetTransitionState(byEntity.Api.World, slot, EnumTransitionType.Perish);
        double spoilState = transitionState?.TransitionLevel ?? 0.0;
        float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
        hydrationProps.Hydration *= num1;
        hydrationProps.EuhydrationWeight *= num1;
        hydrationProps.HydrationLossDelay *= num1;
        _capturedProperties[player.PlayerUID] = hydrationProps;
        _capturedServings[player.PlayerUID] = meal.GetQuantityServings(byEntity.World, slot.Itemstack);
    }
    public static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity, bool handleAllServingsConsumed)
    {
        if (ShouldSkipPatch) return;
        if (_alreadyCalled) return;
        _alreadyCalled = true;
        if (byEntity is not EntityPlayer player || !_capturedProperties.ContainsKey(player.PlayerUID)) return;
        var api = byEntity.World?.Api;
        if (api is not { Side: EnumAppSide.Server }) return;
        float servingsBeforeConsume = _capturedServings[player.PlayerUID];
        float servingsAfterConsume = (slot.Itemstack.Collectible as BlockMeal)?.GetQuantityServings(byEntity.World, slot.Itemstack) ?? 0;
        float servingsConsumed = servingsBeforeConsume - servingsAfterConsume;
        if (servingsConsumed <= 0) return;
        byEntity.ReceiveHydration(_capturedProperties[player.PlayerUID] * servingsConsumed / servingsBeforeConsume);
        _capturedProperties.Remove(player.PlayerUID);
    }
}