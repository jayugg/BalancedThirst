using System.Collections.Generic;
using BalancedThirst.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BalancedThirst.Items;

  public class ItemDowsingRod : Item
  {
    
    public override void OnHeldInteractStart(
      ItemSlot itemslot,
      EntityAgent byEntity,
      BlockSelection blockSel,
      EntitySelection entitySel,
      bool firstEvent,
      ref EnumHandHandling handling)
    {
      if (byEntity.Controls.Sneak)
      {
        base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        return;
      }
      int radius = BtCore.ConfigServer.DowsingRodRadius;
      if (radius <= 0) return;
      this.ProbeBlockNodeMode(byEntity.World, byEntity, itemslot, blockSel, radius);
      if (api.World.Rand.NextSingle() > 0.5f) DamageItem(byEntity.World, byEntity, itemslot);
      handling = EnumHandHandling.PreventDefault;
    }
    
    protected virtual void ProbeBlockNodeMode(
      IWorldAccessor world,
      Entity byEntity,
      ItemSlot itemslot,
      BlockSelection blockSel,
      int radius)
    {
      IPlayer byPlayer = null;
      if (byEntity is EntityPlayer player)
        byPlayer = world.PlayerByUid(player.PlayerUID);
      if (!(byPlayer is IServerPlayer serverPlayer))
        return;
      BlockPos blockPos = serverPlayer.Entity.Pos.AsBlockPos;
      BlockPos closestWaterPos = FindClosestWaterBlock(world, byEntity, blockPos, radius);

      if (closestWaterPos == null)
      {
        serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(serverPlayer.LanguageCode, "No pure water nearby"), EnumChatType.Notification);
      }
      else
      {
        TextCommandCallingArgs args1 = new TextCommandCallingArgs();
        args1.Caller = new Caller()
        {
          Player = serverPlayer,
          Pos = serverPlayer.Entity.Pos.XYZ,
        };
        args1.RawArgs = new CmdArgs("add blue Spring water");
        
        this.api.ChatCommands.Execute("waypoint", args1);
        
        //itemslot.Itemstack.Attributes.Se
        serverPlayer.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get(BtCore.Modid+":Found water nearby!"), EnumChatType.Notification);
        //serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "Closest water found at " + closestWaterPos, EnumChatType.Notification);
        var message = new DowsingRodMessage()
        {
          Position = closestWaterPos
        };
        ((IServerNetworkChannel)serverPlayer.Entity.Api.Network.GetChannel(BtCore.Modid + ".drink")).SendPacket(message, serverPlayer);
      }
    }
    
    public BlockPos FindClosestWaterBlock(IWorldAccessor world, Entity byEntity, BlockPos startPos, int radius)
    {
      Queue<BlockPos> queue = new Queue<BlockPos>();
      HashSet<BlockPos> visited = new HashSet<BlockPos>();
      queue.Enqueue(startPos);
      visited.Add(startPos);

      while (queue.Count > 0)
      {
        BlockPos currentPos = queue.Dequeue();
        Block block = world.BlockAccessor.GetBlock(currentPos);

        if (block.BlockMaterial == EnumBlockMaterial.Liquid && block.Code.ToString().Contains("purewater"))
        {
          return currentPos;
        }

        foreach (BlockFacing facing in BlockFacing.ALLFACES)
        {
          BlockPos neighborPos = currentPos.AddCopy(facing.Normali);
          if (!visited.Contains(neighborPos) && startPos.DistanceTo(neighborPos) <= radius)
          {
            queue.Enqueue(neighborPos);
            visited.Add(neighborPos);
          }
        }
      }
      
      return null;
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
    {
      return new []
      {
        new WorldInteraction()
        {
          ActionLangCode = "Search for water",
          MouseButton = EnumMouseButton.None
        }
      };
    }
}
