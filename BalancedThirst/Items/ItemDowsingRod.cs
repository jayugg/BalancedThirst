using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace BalancedThirst.Items;

  public class ItemDowsingRod : Item
  {
    public override void OnLoaded(ICoreAPI api)
    {
      base.OnLoaded(api);
      ICoreClientAPI capi = api as ICoreClientAPI;
      if (api.Side != EnumAppSide.Server)
        return;
    }
    
    public override void OnHeldInteractStart(
      ItemSlot itemslot,
      EntityAgent byEntity,
      BlockSelection blockSel,
      EntitySelection entitySel,
      bool firstEvent,
      ref EnumHandHandling handling)
    {
      int toolMode = this.GetToolMode(itemslot, (byEntity as EntityPlayer).Player, blockSel);
      int radius = 8 * this.api.World.Config.GetString("propickNodeSearchRadius").ToInt();
      this.ProbeBlockNodeMode(byEntity.World, byEntity, itemslot, blockSel, radius);
      this.DamageItem(byEntity.World, byEntity, itemslot);
      handling = EnumHandHandling.PreventDefault;
    }
    
    protected virtual void ProbeBlockNodeMode(
      IWorldAccessor world,
      Entity byEntity,
      ItemSlot itemslot,
      BlockSelection blockSel,
      int radius)
    {
      IPlayer byPlayer = (IPlayer) null;
      if (byEntity is EntityPlayer)
        byPlayer = world.PlayerByUid(((EntityPlayer) byEntity).PlayerUID);
      if (!(byPlayer is IServerPlayer serverPlayer))
        return;
      BlockPos blockPos = blockSel.Position.Copy();
      int quantityFound = 0;
      this.api.World.BlockAccessor.WalkBlocks(blockPos.AddCopy(radius, radius, radius), blockPos.AddCopy(-radius, -radius, -radius), (Action<Block, int, int, int>) ((nblock, x, y, z) =>
      {
        if (nblock.BlockMaterial != EnumBlockMaterial.Liquid || !nblock.Code.ToString().Contains("purewater"))
          return;
        quantityFound += 1;
      }));
      if (quantityFound == 0)
      {
        serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, "No pure water nearby"), EnumChatType.Notification);
      }
      else
      {
        string l2 = Lang.GetL(serverPlayer.LanguageCode, this.resultTextByQuantity(quantityFound));
        serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, l2), EnumChatType.Notification);
      }
    }

    protected virtual string resultTextByQuantity(int value)
    {
      if (value < 2)
        return "dowsingrod-nodesearch-traceamount";
      if (value < 4)
        return "dowsingrod-nodesearch-smallamount";
      if (value < 10)
        return "dowsingrod-nodesearch-mediumamount";
      if (value < 30)
        return "dowsingrod-nodesearch-largeamount";
      return value < 60 ? "dowsingrod-nodesearch-verylargeamount" : "dowsingrod-nodesearch-hugeamount";
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
    {
      return new WorldInteraction[1]
      {
        new WorldInteraction()
        {
          ActionLangCode = "Change tool mode",
          HotKeyCodes = new string[1]{ "toolmodeselect" },
          MouseButton = EnumMouseButton.None
        }
      };
    }
}
