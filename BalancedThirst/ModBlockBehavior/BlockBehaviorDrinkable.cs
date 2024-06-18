using System;
using System.Collections.Generic;
using BalancedThirst.ModBehavior;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BalancedThirst.ModBlockBehavior;

public class BlockBehaviorDrinkable : BlockBehavior
{
    public BlockBehaviorDrinkable(Block block) : base(block)
    {
        BtCore.Logger.Warning("Creating BlockBehaviorDrinkable for " + block.Code.Path);
    }
    
    public virtual HydrationProperties GetHydrationProperties(ItemStack itemstack)
    {
        try
        {
            BtCore.Logger.Warning("Getting hydration properties for " + itemstack.Collectible.Code.Path);
            JsonObject itemAttribute = itemstack.ItemAttributes?["hydrationProps"];
            return itemAttribute is { Exists: true } ? itemAttribute.AsObject<HydrationProperties>( null, itemstack.Collectible.Code.Domain) : null;
        }
        catch (Exception ex)
        {
            BtCore.Logger.Error("Error getting hydration properties: " + ex.Message);
            return null;
        }
    }
    
    public HydrationProperties GetHydrationProperties(IWorldAccessor world, Entity byEntity)
    {
        var itemstack = GetBlockStack(world, byEntity);
        try
        {
            BtCore.Logger.Warning("Getting hydration properties for " + itemstack.Collectible.Code.Path);
            JsonObject itemAttribute = itemstack.ItemAttributes?["hydrationProps"];
            return itemAttribute is { Exists: true } ? itemAttribute.AsObject<HydrationProperties>( null, itemstack.Collectible.Code.Domain) : null;
        }
        catch (Exception ex)
        {
            BtCore.Logger.Error("Error getting hydration properties: " + ex.Message);
            return null;
        }
    }

    public override bool OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        ref EnumHandling handling) 
    {
        if (!byPlayer.Entity.Controls.Sneak) return false;
        var byEntity = byPlayer.Entity;
        byEntity.PlayEntitySound("drink", byPlayer);
        handling = EnumHandling.PreventDefault;
        var itemStack = GetBlockStack(world, byEntity);
        if (itemStack == null) return false;
        HydrationProperties hydrationProperties = collObj.GetHydrationProperties(itemStack, byEntity);
        BtCore.Logger.Warning(hydrationProperties?.Hydration.ToString());
        byEntity.World.RegisterCallback(_ => PlayDrinkSound(byEntity, 4), 500);
        byEntity.AnimManager?.StartAnimation("eat");
        if (byEntity is { Player: IClientPlayer clientPlayer })
        {
            BtCore.Logger.Warning("TriggerFpAnimation");
            clientPlayer.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
        }
        handling = EnumHandling.PreventDefault;
        return true;
    }

    public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        BtCore.Logger.Warning("Drinking continues");
        if (secondsUsed % 1 == 0)
        {
            var byEntity = byPlayer.Entity;
            var blockStack = GetBlockStack(world, byEntity);
            if (blockStack == null) return false;
            var hydrationProps = GetHydrationProperties(blockStack);
            if (hydrationProps == null) return false;
            byPlayer.Entity.ReceiveHydration(GetHydrationProperties(blockStack));
            byEntity.World.RegisterCallback(_ => PlayDrinkSound(byEntity, 4), 500);
            Vec3d xyz = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
            xyz.X += byEntity.LocalEyePos.X;
            xyz.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
            xyz.Z += byEntity.LocalEyePos.Z;
            if (secondsUsed > 0.5 && (int) (30.0 * secondsUsed) % 7 == 1)
                byEntity.World.SpawnCubeParticles(xyz, blockStack, 0.3f, 4, 0.5f, byEntity.Player);
            if (byEntity.World is not IClientWorldAccessor)
                return true;
            ModelTransform modelTransform = new ModelTransform();
            modelTransform.EnsureDefaultValues();
            modelTransform.Origin.Set(0.0f, 0.0f, 0.0f);
            if (secondsUsed > 0.5)
                modelTransform.Translation.Y = Math.Min(0.02f, GameMath.Sin(20f * secondsUsed) / 10f);
            modelTransform.Translation.X -= Math.Min(1f, (float) (secondsUsed * 4.0 * 1.5700000524520874));
            modelTransform.Translation.Y -= Math.Min(0.05f, secondsUsed * 2f);
            modelTransform.Rotation.X += Math.Min(30f, secondsUsed * 350f);
            modelTransform.Rotation.Y += Math.Min(80f, secondsUsed * 350f);
            return secondsUsed <= 1.0;
        }
        handling = EnumHandling.PreventDefault;
        return false;
    }

    private ItemStack GetBlockStack(IWorldAccessor world, Entity byEntity)
    {
        AssetLocation assetLocation = new AssetLocation(this.block.Code.ToString());
        Item blockItem = byEntity.World.GetItem(assetLocation);
        if (blockItem == null)
        {
            BtCore.Logger.Error($"No item found with asset location: {assetLocation}");
            return null;
        }
        ItemStack blockStack = new ItemStack(blockItem);
        return blockStack;
    }
    
    private void PlayDrinkSound(EntityAgent byEntity, int eatSoundRepeats = 1)
    {
        if (byEntity.Controls.HandUse != EnumHandInteract.HeldItemInteract)
            return;
        IPlayer dualCallByPlayer = (IPlayer) null;
        if (byEntity is EntityPlayer)
            dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer) byEntity).PlayerUID);
        byEntity.PlayEntitySound("drink", dualCallByPlayer);
        eatSoundRepeats--;
        if (eatSoundRepeats <= 0)
            return;
        byEntity.World.RegisterCallback(dt => this.PlayDrinkSound(byEntity, eatSoundRepeats), 300);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
    {
        List<WorldInteraction> interactions = new List<WorldInteraction>();
        interactions.AddRange(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling));
        interactions.Add(new WorldInteraction()
        {
            HotKeyCode = "sneak",
            ActionLangCode = "blockhelp-drinkable-drink",
            MouseButton = EnumMouseButton.Right,
            RequireFreeHand = true
        });
        return interactions.ToArray();
    }
    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        return base.GetPlacedBlockInfo(world, pos, forPlayer) + Lang.Get("blockdesc-drinkable");
    }
}