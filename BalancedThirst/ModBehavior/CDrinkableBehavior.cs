using System;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.ModBehavior;

public class CDrinkableBehavior : CollectibleBehavior
{
  
    private ICoreAPI _api;
    
    public CDrinkableBehavior(CollectibleObject collObj) : base(collObj)
    {
      BtCore.Logger.Warning("Creating CDrinkableBehavior");
    }
    
    public override void OnLoaded(ICoreAPI api)
    {
      _api = api;
      BtCore.Logger.Warning("Initializing CDrinkableBehavior");
      base.OnLoaded(api);
    }
    
    public virtual HydrationProperties GetHydrationProperties(ItemStack itemstack)
    {
      BtCore.Logger.Warning("Getting hydration properties");
      if (collObj is BlockLiquidContainerBase)
      {
        return GetContainerHydrationProperties(collObj as BlockLiquidContainerBase, itemstack);
      }
      try
      {
        JsonObject itemAttribute = itemstack?.ItemAttributes?["hydrationProps"];
        return itemAttribute is { Exists: true } ? itemAttribute.AsObject<HydrationProperties>( null, itemstack.Collectible.Code.Domain) : null;
      }
      catch (Exception ex)
      {
        BtCore.Logger.Error("Error getting hydration properties: " + ex.Message);
        return null;
      }
    }
    
    public static HydrationProperties GetContainerHydrationProperties(
      BlockLiquidContainerBase container,
      ItemStack itemstack)
    {
      if (itemstack == null) return null;
      ItemStack content = container.GetContent(itemstack);
      if (content == null) return null;
      if (!content.Collectible.HasBehavior<CDrinkableBehavior>()) return null;
      var behavior = content.Collectible.GetBehavior<CDrinkableBehavior>();
      HydrationProperties hydrationProperties = behavior.GetHydrationProperties(itemstack);
      if  (hydrationProperties == null)
        return null;
      WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(content);
      if (containableProps?.NutritionPropsPerLitre == null) return hydrationProperties;
      float num = content.StackSize / containableProps.ItemsPerLitre;
      hydrationProperties.Hydration *= num;
      hydrationProperties.HydrationLossDelay *= num;
      hydrationProperties.Contamination *= num;
      return hydrationProperties;
    }
    
    protected virtual void TryDrinkBegin(
      ItemSlot slot,
      EntityAgent byEntity,
      ref EnumHandHandling handling,
      int eatSoundRepeats = 1)
    {
      BtCore.Logger.Warning("TryEatBeginBehavior");
      if (!slot.Itemstack.Collectible.HasBehavior<CDrinkableBehavior>()) return;
      var behavior = slot.Itemstack.Collectible.GetBehavior<CDrinkableBehavior>();
      BtCore.Logger.Warning("TryEatBeginBehavior2");
      HydrationProperties hydrationProperties = behavior.GetHydrationProperties(slot.Itemstack);
      BtCore.Logger.Warning(hydrationProperties.Hydration.ToString());
      if (slot.Empty)
        return;
      byEntity.World.RegisterCallback(_ => behavior.PlayDrinkSound(byEntity, eatSoundRepeats), 500);
      byEntity.AnimManager?.StartAnimation("eat");
      BtCore.Logger.Warning("PreventDefault");
      handling = EnumHandHandling.PreventDefault;
    }
    
    protected virtual bool TryDrinkStep(
      float secondsUsed,
      ItemSlot slot,
      EntityAgent byEntity,
      ItemStack spawnParticleStack = null)
    {
      BtCore.Logger.Warning("TryEatStepBehavior");
      var behavior = slot.Itemstack.Collectible.GetBehavior<CDrinkableBehavior>();
      HydrationProperties hydrationProperties = behavior.GetHydrationProperties(slot.Itemstack);
      if (hydrationProperties == null) return false;
      Vec3d xyz = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
      xyz.X += byEntity.LocalEyePos.X;
      xyz.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
      xyz.Z += byEntity.LocalEyePos.Z;
      if (secondsUsed > 0.5 && (int) (30.0 * secondsUsed) % 7 == 1)
        byEntity.World.SpawnCubeParticles(xyz, spawnParticleStack ?? slot.Itemstack, 0.3f, 4, 0.5f, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
      if (!(byEntity.World is IClientWorldAccessor))
        return false;
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
      BtCore.Logger.Warning("TryEatStopBehavior");
      var behavior = slot.Itemstack.Collectible.GetBehavior<CDrinkableBehavior>();
      BtCore.Logger.Warning("TryEatStopBehavior2");
      HydrationProperties hydrationProperties = behavior.GetHydrationProperties(slot.Itemstack);
      if (!(byEntity.World is IServerWorldAccessor) || !byEntity.HasBehavior<EntityBehaviorThirst>() || hydrationProperties == null || secondsUsed < 0.949999988079071)
        return;
      TransitionState transitionState = slot.Itemstack.Collectible.UpdateAndGetTransitionState(byEntity.Api.World, slot, EnumTransitionType.Perish);
      double spoilState = transitionState?.TransitionLevel ?? 0.0;
      float num1 = GlobalConstants.FoodSpoilageSatLossMul((float) spoilState, slot.Itemstack, byEntity);
      byEntity.GetBehavior<EntityBehaviorThirst>().ReceiveHydration(hydrationProperties.Hydration*num1, hydrationProperties.HydrationLossDelay*num1);
    }
    
    public void PlayDrinkSound(EntityAgent byEntity, int eatSoundRepeats = 1)
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
    
    /*

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel,
      bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
      BtCore.Logger.Warning("OnHeldInteractStart");
      BtCore.Logger.Warning(handling.ToString());
      TryDrinkBegin(slot, byEntity, ref handHandling);
      base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
      EntitySelection entitySel, ref EnumHandling handling)
    {
      if (TryDrinkStep(secondsUsed, slot, byEntity))
        return true;
      return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
    }
    
    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
      EntitySelection entitySel, ref EnumHandling handling)
    {
      TryDrinkStop(secondsUsed, slot, byEntity);
      base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
    }
    
    */
    
}
