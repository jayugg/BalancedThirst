using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace BalancedThirst.Items;

  public class ItemDowsingRod : Item
  {
    private ProPickWorkSpace ppws;
    private SkillItem[] toolModes;
    public override void OnLoaded(ICoreAPI api)
    {
      base.OnLoaded(api);
      ICoreClientAPI capi = api as ICoreClientAPI;
      this.toolModes = ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "dowsingRodToolModes", (CreateCachableObjectDelegate<SkillItem[]>) (() =>
      {
        SkillItem[] skillItemArray;
        if (api.World.Config.GetString("propickNodeSearchRadius").ToInt() > 0)
          skillItemArray = new SkillItem[2]
          {
            new SkillItem()
            {
              Code = new AssetLocation("density"),
              Name = Lang.Get("Density Search Mode (Long range, chance based search)")
            },
            new SkillItem()
            {
              Code = new AssetLocation("node"),
              Name = Lang.Get("Node Search Mode (Short range, exact search)")
            }
          };
        else
          skillItemArray = new SkillItem[1]
          {
            new SkillItem()
            {
              Code = new AssetLocation("density"),
              Name = Lang.Get("Density Search Mode (Long range, chance based search)")
            }
          };
        if (capi != null)
        {
          skillItemArray[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/heatmap.svg"), 48, 48, 5, new int?(-1)));
          skillItemArray[0].TexturePremultipliedAlpha = false;
          if (skillItemArray.Length > 1)
          {
            skillItemArray[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/rocks.svg"), 48, 48, 5, new int?(-1)));
            skillItemArray[1].TexturePremultipliedAlpha = false;
          }
        }
        return skillItemArray;
      }));
      if (api.Side != EnumAppSide.Server)
        return;
      this.ppws = ObjectCacheUtil.GetOrCreate<ProPickWorkSpace>(api, "dowsingrodworkspace", (CreateCachableObjectDelegate<ProPickWorkSpace>) (() =>
      {
        ProPickWorkSpace proPickWorkSpace = new ProPickWorkSpace();
        proPickWorkSpace.OnLoaded(api);
        return proPickWorkSpace;
      }));
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
      int amount = 1;
      if (toolMode == 1 && radius > 0)
      {
        this.ProbeBlockNodeMode(byEntity.World, byEntity, itemslot, blockSel, radius);
        amount = 2;
      }
      else
        this.ProbeBlockDensityMode(byEntity.World, byEntity, itemslot, blockSel);
      if (this.DamagedBy != null && this.DamagedBy.Contains<EnumItemDamageSource>(EnumItemDamageSource.BlockBreaking))
        this.DamageItem(byEntity.World, byEntity, itemslot, amount);
      handling = EnumHandHandling.PreventDefault;
    }
    
    public override SkillItem[] GetToolModes(
      ItemSlot slot,
      IClientPlayer forPlayer,
      BlockSelection blockSel)
    {
      return this.toolModes;
    }

    public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
      return Math.Min(this.toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode"));
    }

    public override void SetToolMode(
      ItemSlot slot,
      IPlayer byPlayer,
      BlockSelection blockSel,
      int toolMode)
    {
      slot.Itemstack.Attributes.SetInt(nameof (toolMode), toolMode);
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

    protected virtual void ProbeBlockDensityMode(
      IWorldAccessor world,
      Entity byEntity,
      ItemSlot itemslot,
      BlockSelection blockSel)
    {
      IPlayer byPlayer = (IPlayer) null;
      if (byEntity is EntityPlayer)
        byPlayer = world.PlayerByUid(((EntityPlayer) byEntity).PlayerUID);
      if (!(byPlayer is IServerPlayer splr))
        return;
      this.PrintProbeResults(world, splr, itemslot, blockSel.Position);
    }

    protected virtual void PrintProbeResults(
      IWorldAccessor world,
      IServerPlayer splr,
      ItemSlot itemslot,
      BlockPos pos)
    {
      PropickReading results = this.GenProbeResults(world, pos);
      string humanReadable = results.ToHumanReadable(splr.LanguageCode, this.ppws.pageCodes);
      splr.SendMessage(GlobalConstants.InfoLogChatGroup, humanReadable, EnumChatType.Notification);
      world.Api.ModLoader.GetModSystem<ModSystemOreMap>()?.DidProbe(results, splr);
    }

    protected virtual PropickReading GenProbeResults(IWorldAccessor world, BlockPos pos)
    {
      if (this.api.ModLoader.GetModSystem<GenDeposits>()?.Deposits == null)
        return (PropickReading) null;
      int regionSize = world.BlockAccessor.RegionSize;
      IMapRegion mapRegion = world.BlockAccessor.GetMapRegion(pos.X / regionSize, pos.Z / regionSize);
      int num1 = pos.X % regionSize;
      int num2 = pos.Z % regionSize;
      pos = pos.Copy();
      pos.Y = world.BlockAccessor.GetTerrainMapheightAt(pos);
      int[] rockColumn = this.ppws.GetRockColumn(pos.X, pos.Z);
      PropickReading propickReading = new PropickReading();
      propickReading.Position = new Vec3d((double) pos.X, (double) pos.Y, (double) pos.Z);
      foreach (KeyValuePair<string, IntDataMap2D> oreMap in mapRegion.OreMaps)
      {
        IntDataMap2D intDataMap2D = oreMap.Value;
        int innerSize = intDataMap2D.InnerSize;
        int unpaddedColorLerped = intDataMap2D.GetUnpaddedColorLerped((float) num1 / (float) regionSize * (float) innerSize, (float) num2 / (float) regionSize * (float) innerSize);
        if (this.ppws.depositsByCode.ContainsKey(oreMap.Key))
        {
          BtCore.Logger.Warning("Key: " + oreMap.Key + " is in depositsByCode)");
          double ppt;
          double totalFactor;
          this.ppws.depositsByCode[oreMap.Key].GetPropickReading(pos, unpaddedColorLerped, rockColumn, out ppt, out totalFactor);
          if (totalFactor > 0.0)
            propickReading.OreReadings[oreMap.Key] = new OreReading()
            {
              TotalFactor = totalFactor,
              PartsPerThousand = ppt
            };
        }
      }
      return propickReading;
    }

    public override void OnUnloaded(ICoreAPI api)
    {
      base.OnUnloaded(api);
      if (api is ICoreServerAPI coreServerApi)
      {
        this.ppws?.Dispose(api);
        coreServerApi.ObjectCache.Remove("dowsingrodworkspace");
      }
      for (int index = 0; this.toolModes != null && index < this.toolModes.Length; ++index)
        this.toolModes[index]?.Dispose();
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
