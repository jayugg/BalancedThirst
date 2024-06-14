using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BalancedThirst.ModBlockBehavior;

public class BlockBehaviorDrinkable : BlockBehavior
{
    private int _thirstSlake;
    private int _vomitEvery;

    public BlockBehaviorDrinkable(Block block) : base(block) { }
    
    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        this._thirstSlake = properties["thirstSlake"].AsInt();
        this._vomitEvery = properties["vomitEvery"].AsInt();
    }

    public override bool OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        ref EnumHandling handling) 
    {
        if (byPlayer.Entity.Controls.Sneak)
        {
            BtCore.Logger.Warning("Can Drinking!");
            byPlayer.Entity.PlayEntitySound("drink", byPlayer);
            handling = EnumHandling.PreventDefault;
            return true;
        }
        return false;
    }

    public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        BtCore.Logger.Warning("Drinking continues");
        if (secondsUsed % 0.5 == 0)
        {
            PlayDrinkSound(byPlayer.Entity, 10);
            Item waterItem = byPlayer.Entity.World.GetItem(new AssetLocation("game:water-still-7"));
            if (waterItem != null)
            {
                ItemStack waterStack = new ItemStack(waterItem);
                byPlayer.Entity.World.SpawnCubeParticles(byPlayer.Entity.Pos.AheadCopy(0.25).XYZ.Add(0.0, byPlayer.Entity.SelectionBox.Y2 / 2.0, 0.0), waterStack, 0.75f, 20, 0.45f);
            }
            BtCore.Logger.Warning(_thirstSlake.ToString());
            AddSlakeTo(byPlayer, _thirstSlake != 0 ? (int)(_thirstSlake/2.5) : 20);
        }
        handling = EnumHandling.PreventDefault;
        return false;
    }
    
    private void AddSlakeTo(IPlayer player, int value)
    {
        var thirstTree = player.Entity.WatchedAttributes.GetTreeAttribute(BtCore.Modid+":thirst");

        float? currentSlake = thirstTree.TryGetFloat("currentslake");
        float? maxSlake = thirstTree.TryGetFloat("maxslake");

        if (!currentSlake.HasValue || !maxSlake.HasValue) return;
        thirstTree.SetFloat("currentslake", Math.Min(currentSlake.Value + value, maxSlake.Value));
        player.Entity.WatchedAttributes.MarkPathDirty(BtCore.Modid+":thirst");
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
        byEntity.World.RegisterCallback((Action<float>) (dt => this.PlayDrinkSound(byEntity, eatSoundRepeats)), 300);
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