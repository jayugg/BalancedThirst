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

public class DrinkableBehavior : CollectibleBehavior
{
  
    private ICoreAPI _api;
    
    public DrinkableBehavior(CollectibleObject collObj) : base(collObj)
    {
      //BtCore.Logger.Warning("Creating DrinkableBehavior");
    }
    
    public override void OnLoaded(ICoreAPI api)
    {
      _api = api;
      //BtCore.Logger.Warning("Initializing DrinkableBehavior");
      base.OnLoaded(api);
    }
    
    public virtual HydrationProperties GetHydrationProperties(ItemStack itemstack)
    {
      if (collObj is BlockLiquidContainerBase container)
      {
        return GetContainerHydrationProperties(container, itemstack);
      }
      try
      {
        JsonObject itemAttribute = itemstack?.ItemAttributes?["hydrationProps"];
        BtCore.Logger.Warning(itemAttribute?.ToString());
        return itemAttribute is { Exists: true } ? itemAttribute.AsObject<HydrationProperties>( null, itemstack.Collectible.Code.Domain) : null;
      }
      catch (Exception ex)
      {
        //BtCore.Logger.Error("Error getting hydration properties: " + ex.Message);
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
      if (!content.Collectible.HasBehavior<DrinkableBehavior>()) return null;
      var behavior = content.Collectible.GetBehavior<DrinkableBehavior>();
      BtCore.Logger.Warning("Getting hydration properties for " + content.Collectible.Code.Path);
      HydrationProperties hydrationProperties = behavior.GetHydrationProperties(new ItemStack(content.Item));
      if (hydrationProperties == null)
        return null;
      WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(content);
      BtCore.Logger.Warning("Items per litre: " + containableProps.ItemsPerLitre);
      BtCore.Logger.Warning("Stack size: " + content.StackSize);
      BtCore.Logger.Warning("Hydration: " + hydrationProperties.Hydration);
      float num = content.StackSize / containableProps.ItemsPerLitre;
      hydrationProperties.Hydration *= num;
      hydrationProperties.HydrationLossDelay *= num;
      BtCore.Logger.Warning("Hydration in container: " + hydrationProperties.Hydration);
      return hydrationProperties;
    }
    
    public static void PlayDrinkSound(EntityAgent byEntity, int eatSoundRepeats = 1)
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
    
}
