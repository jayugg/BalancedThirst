using System;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace BalancedThirst.ModBehavior;

public class CDrinkableBehavior : CollectibleBehavior
{
  
    public HydrationProperties HydrationProps;
    private ICoreAPI _api;
  
    public CDrinkableBehavior(CollectibleObject collObj) : base(collObj)
    {
    }

    public void Initialize(ICoreAPI api)
    {
      _api = api;
      JsonObject attributes = collObj.Attributes;
      JToken hydrationToken = attributes["hydrationProps"].Token;
      if (hydrationToken != null)
      {
        HydrationProps = hydrationToken.ToObject<HydrationProperties>();
      }
    }
    
    public void Initialize(ICoreAPI api, HydrationProperties props)
    {
      _api = api;
      HydrationProps = props.Clone();
    }
    
    public virtual HydrationProperties GetHydrationProperties(
      IWorldAccessor world,
      ItemStack itemstack,
      Entity forEntity)
    {
      return HydrationProps;
    }
    
    protected virtual void TryDrinkBegin(
      ItemSlot slot,
      EntityAgent byEntity,
      ref EnumHandHandling handling,
      int eatSoundRepeats = 1)
    {
      if (slot.Empty || GetHydrationProperties(byEntity.World, slot.Itemstack, byEntity) == null)
      {
        BtCore.Logger.Warning("No hydration properties found");
        return;
      }

      byEntity.World.RegisterCallback(_ => PlayDrinkSound(byEntity, eatSoundRepeats), 500);
      byEntity.AnimManager?.StartAnimation("eat");
      handling = EnumHandHandling.PreventDefault;
    }
    
    protected virtual bool TryDrinkStep(
      float secondsUsed,
      ItemSlot slot,
      EntityAgent byEntity,
      ItemStack spawnParticleStack = null)
    {
      if (GetHydrationProperties(byEntity.World, slot.Itemstack, byEntity) == null)
        return false;
      Vec3d xyz = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
      xyz.X += byEntity.LocalEyePos.X;
      xyz.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
      xyz.Z += byEntity.LocalEyePos.Z;
      if (secondsUsed > 0.5 && (int) (30.0 * secondsUsed) % 7 == 1)
        byEntity.World.SpawnCubeParticles(xyz, spawnParticleStack ?? slot.Itemstack, 0.3f, 4, 0.5f, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
      if (!(byEntity.World is IClientWorldAccessor))
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
    
    protected virtual void TryDrinkStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
      HydrationProperties hydrationProperties = GetHydrationProperties(byEntity.World, slot.Itemstack, byEntity);
      if (!(byEntity.World is IServerWorldAccessor) || hydrationProperties == null || secondsUsed < 0.949999988079071)
        return;
      TransitionState transitionState = collObj.UpdateAndGetTransitionState(_api.World, slot, EnumTransitionType.Perish);
      double spoilState = transitionState != null ? transitionState.TransitionLevel : 0.0;
      float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
      
      if (!byEntity.HasBehavior<EntityBehaviorThirst>()) return;
      
      byEntity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProperties.Hydration * num1);
      IPlayer player;
      if (byEntity is EntityPlayer entityPlayer) {
        player = entityPlayer.World.PlayerByUid(entityPlayer.PlayerUID);
        slot.TakeOut(1);
        if (hydrationProperties.EatenStack != null)
        {
          if (slot.Empty)
            slot.Itemstack = hydrationProperties.EatenStack.ResolvedItemstack.Clone();
          else if (player == null || !player.InventoryManager.TryGiveItemstack(hydrationProperties.EatenStack.ResolvedItemstack.Clone(), true))
            entityPlayer.World.SpawnItemEntity(hydrationProperties.EatenStack.ResolvedItemstack.Clone(), entityPlayer.SidedPos.XYZ);
        }
        slot.MarkDirty();
        player?.InventoryManager.BroadcastHotbarSlot();
      }
    }
    
    protected void PlayDrinkSound(EntityAgent byEntity, int eatSoundRepeats = 1)
    {
      if (byEntity.Controls.HandUse != EnumHandInteract.HeldItemInteract)
        return;
      IPlayer dualCallByPlayer = null;
      if (byEntity is EntityPlayer)
        dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer) byEntity).PlayerUID);
      byEntity.PlayEntitySound("drink", dualCallByPlayer);
      eatSoundRepeats--;
      if (eatSoundRepeats <= 0)
        return;
      byEntity.World.RegisterCallback(_ => PlayDrinkSound(byEntity, eatSoundRepeats), 300);
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel,
      bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
      BtCore.Logger.Warning("OnHeldInteractStart");
      TryDrinkBegin(slot, byEntity, ref handHandling);
      base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
      EntitySelection entitySel, ref EnumHandling handling)
    {
      BtCore.Logger.Warning("OnHeldInteractStep");
      TryDrinkStep(secondsUsed, slot, byEntity);
      return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
    }
    
    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
      EntitySelection entitySel, ref EnumHandling handling)
    {
      BtCore.Logger.Warning("OnHeldInteractStop");
      TryDrinkStop(secondsUsed, slot, byEntity);
      base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
    }
}
