using System;
using System.Linq;
using System.Text;
using BalancedThirst.Systems;
using BalancedThirst.Thirst;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BalancedThirst.ModBehavior;

public class WaterContainerBehavior : DrinkableBehavior
{
    private SkillItem[] modes;

    public static int DirtyWaterMode = 1;
    
    public WaterContainerBehavior(CollectibleObject collObj) : base(collObj)
    {
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        
        if (ConfigSystem.SyncedConfigData.DynamicWaterPurity == false ||
            inSlot.Itemstack.Collectible is not BlockLiquidContainerBase container
            || inSlot.Itemstack.Attributes?.GetInt("toolMode") != DirtyWaterMode) return base.GetHeldInteractionHelp(inSlot, ref handling);
        var result = base.GetHeldInteractionHelp(inSlot, ref handling);
        WorldInteraction newInteraction = new WorldInteraction()
        {
            ActionLangCode = $"{BtCore.Modid}:heldhelp-container-getvanillawater",
            HotKeyCode = "ctrl",
            MouseButton = EnumMouseButton.Right,
            ShouldApply = (_, _, _) =>
                container.GetCurrentLitres(inSlot.Itemstack) <= 0.0f
        };
        result[0] = newInteraction;
        return result;
    }

    public override void OnLoaded(ICoreAPI api)
    {
        if (ConfigSystem.SyncedConfigData.DynamicWaterPurity == false) return;
        modes = new SkillItem[2]
        {
            new SkillItem
            {
                Code = new AssetLocation("pickwater"),
                Name = Lang.Get($"{BtCore.Modid}:heldhelp-container-getwater")
            },
            new SkillItem
            {
                Code = new AssetLocation("pickdirtywater"),
                Name = Lang.Get($"{BtCore.Modid}:heldhelp-container-getvanillawater")
            }
        };

        if (api is not ICoreClientAPI capi) return;
        modes[0].WithIcon(capi, "lake");
        modes[0].TexturePremultipliedAlpha = false;
        modes[1].WithIcon(capi, "select");
        modes[1].TexturePremultipliedAlpha = false;
    }
    
    public override void OnUnloaded(ICoreAPI api)
    {
        var i = 0;
        while (modes != null && i < modes.Length)
        {
            modes[i]?.Dispose();
            i++;
        }
    }
    
    public override void SetToolMode(
        ItemSlot slot,
        IPlayer byPlayer,
        BlockSelection blockSel,
        int toolMode)
    {
        if (ConfigSystem.SyncedConfigData.DynamicWaterPurity == false) return;
        slot.Itemstack.Attributes.SetInt(nameof (toolMode), toolMode);
    }
    
    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        if (ConfigSystem.SyncedConfigData.DynamicWaterPurity == false) return base.GetToolModes(slot, forPlayer, blockSel);
        return IsEmpty(slot.Itemstack) ? modes : null;
    }

    private bool IsEmpty(ItemStack itemStack)
    {
        if (itemStack.Collectible is not BlockLiquidContainerBase container) return false;
        var content = container.GetContent(itemStack);
        return content is not { StackSize: > 0 };
    }

    public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection) => slot.Itemstack.Attributes.GetInt("toolMode");

    internal override HydrationProperties GetHydrationProperties(IWorldAccessor world, ItemStack itemstack, Entity byEntity)
    {
        return base.ExtractContainerHydrationProperties(world, itemstack, byEntity);
    }

    public static float GetTransitionRateMul(CollectibleObject collectible, EnumTransitionType transType)
    {
        try
        {
            JsonObject attribute = collectible.Attributes?["waterTransitionMul"];
            return attribute is { Exists: true } ? attribute.AsFloat(1) : 1;
        }
        catch (Exception ex)
        {
            BtCore.Logger.Error("Error getting water transition multiplier: " + ex.Message);
            return 1;
        }
    }

    public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand, ref EnumHandling bhHandling)
    {
        var res = base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand, ref bhHandling);
        if (activeHotbarSlot.Itemstack is not { } stack) return res;
        if (stack.Collectible is not BlockLiquidContainerBase container) return res;
        var code = stack.Collectible.Code.ToString();
        var content = container.GetContent(stack);
        if (content == null) return res;
        if (content.Collectible.Code.BeginsWith("balancedthirst","juiceportion-cabalash"))
        {
            var newJuice = forEntity.World.GetItem(new AssetLocation("game:juiceportion-cabalash"));
            BtCore.Logger.Warning($"Transitioning legacy item balancedthirst:juiceportion-cabalash in inventory");
            container.SetContent(activeHotbarSlot.Itemstack, new ItemStack(newJuice)
            {
                StackSize = content.StackSize,
                Attributes = content.Attributes
            });
        }
        if (code.Contains("kettle-clay"))
        {
            BtCore.Logger.Warning($"Transitioning legacy item {code} in inventory");
            var newContainer = forEntity.World.GetBlock(new AssetLocation(code.Replace("-clay-", "-")));
            if (newContainer == null) return res;
            activeHotbarSlot.Itemstack = new ItemStack(newContainer);
            container.SetContent(activeHotbarSlot.Itemstack, content);
        }
        if (code.Contains("waterskin-leather"))
        {
            BtCore.Logger.Warning($"Transitioning legacy item {code} in inventory");
            var newContainer = forEntity.World.GetBlock(new AssetLocation($"{BtCore.Modid}:gourd-large-carved"));
            if (newContainer == null) return res;
            activeHotbarSlot.Itemstack = new ItemStack(newContainer);
            container.SetContent(activeHotbarSlot.Itemstack, content);
        }
        if (code.Contains("waterskin-pelt"))
        {
            BtCore.Logger.Warning($"Transitioning legacy item {code} in inventory");
            var newContainer = forEntity.World.GetBlock(new AssetLocation($"{BtCore.Modid}:gourd-medium-carved"));
            if (newContainer == null) return res;
            activeHotbarSlot.Itemstack = new ItemStack(newContainer);
            container.SetContent(activeHotbarSlot.Itemstack, content);
        }
        if (code.Equals($"{BtCore.Modid}:woodenbowl-raw"))
        {
            BtCore.Logger.Warning($"Transitioning legacy item {code} in inventory");
            var newContainer = forEntity.World.GetBlock(new AssetLocation($"{BtCore.Modid}:woodenbowl-raw-oak"));
            if (newContainer == null) return res;
            activeHotbarSlot.Itemstack = new ItemStack(newContainer);
            container.SetContent(activeHotbarSlot.Itemstack, content);
        }
        if (code.Equals($"{BtCore.Modid}:woodenbowl-waxed"))
        {
            BtCore.Logger.Warning($"Transitioning legacy item {code} in inventory");
            var newContainer = forEntity.World.GetBlock(new AssetLocation($"{BtCore.Modid}:woodenbowl-waxed-pine"));
            if (newContainer == null) return res;
            activeHotbarSlot.Itemstack = new ItemStack(newContainer);
            container.SetContent(activeHotbarSlot.Itemstack, content);
        }
        return res;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        if (ConfigSystem.SyncedConfigData.DynamicWaterPurity == false) return;
        if (inSlot.Itemstack.Collectible.IsWaterContainer())
        {
            dsc.AppendLine(Lang.Get($"{BtCore.Modid}:iteminfo-storedwater", GetTransitionRateMul(collObj, EnumTransitionType.Perish)));
        }
    }
}