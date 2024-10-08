using System;
using System.Linq;
using BalancedThirst.Config;
using BalancedThirst.ModBehavior;
using BalancedThirst.Network;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BalancedThirst.Systems;

public partial class DrinkNetwork
{
    #region Client
    
    private void OnServerPeeMessage(PeeMessage.Response response)
    {
        var entity = _capi.World.Player.Entity;
        SpawnPeeParticles(entity, response.Position, entity.BlockSelection?.HitPosition, ConfigSystem.ConfigClient.UrineColor == "default" ? null : ConfigSystem.ConfigClient.UrineColor);
        EntityBehaviorBladder.PlayPeeSound(entity);
    }
    
    private bool OnPeeKeyPressed(KeyCombination t1)
    {
        var world = _capi?.World;
        var player = world?.Player;
        if (world == null || player == null) return false;
        
        if (ConfigSystem.ConfigClient.PeeMode == EnumPeeMode.None)
        {
            player.IngameError(player, "peemodenotset", Lang.Get(BtCore.Modid+":peemodenotset") );
        }

        // Crutch to have same pee speed as other pee controls
        if ( (player.Entity.World.Side & EnumAppSide.Server) != 0) return false;
        if ( world.ElapsedMilliseconds % 2 != 0 ) return false;
        
        if (ConfigSystem.SyncedConfigData.EnableBladder &&
            !player.Entity.Controls.TriesToMove &&
            player.Entity.RightHandItemSlot.Empty && 
            ConfigSystem.ConfigClient.PeeMode.IsStanding() ||
            (player.Entity.Controls.FloorSitting &&
             ConfigSystem.ConfigClient.PeeMode.IsSitting()))
        {
            _lastPeeTime = world.ElapsedMilliseconds;
            _clientChannel.SendPacket(new PeeMessage.Request()
            {
                Position = player.Entity.BlockSelection?.Position,
                HitPostion = player.Entity.BlockSelection?.HitPosition,
                Color = ConfigSystem.ConfigClient.UrineColor == "default" ? null : ConfigSystem.ConfigClient.UrineColor
            });
            return true;
        }

        return false;
    }
    
    #endregion
    
    #region Server
    
    private void HandlePeeAction(IServerPlayer player, PeeMessage.Request request)
    {
        if (!player.Entity.HasBehavior<EntityBehaviorBladder>()) return;
        if (request.Position == null) return;
        var bh = player.Entity.GetBehavior<EntityBehaviorBladder>();
        if (!bh.Drain(ConfigSystem.ConfigServer.UrineDrainRate)) return;
        EntityBehaviorBladder.PlayPeeSound(player.Entity);
        SpawnPeeParticles(player.Entity, request.Position, player.CurrentBlockSelection?.HitPosition, request.Color);
        
        var world = player.Entity.World;
        var block = world.BlockAccessor.GetBlock(request.Position);
        _serverChannel.SendPacket(new PeeMessage.Response() { Position = request.Position }, player);
        if (block is BlockFarmland)
        {
            FertiliseFarmland(world, request.Position);
        } 
        else if (world.BlockAccessor.GetBlock(request.Position.DownCopy()) is BlockFarmland)
        {
            FertiliseFarmland(world, request.Position.DownCopy());
        }
        else if (block is BlockLiquidContainerBase container )
        {
            var waterStack = new ItemStack(world.GetItem(new AssetLocation(BtCore.Modid+":urineportion")));
            container.TryPutLiquid(request.Position, waterStack, ConfigSystem.ConfigServer.UrineDrainRate*3f/1500); // 3 liters per 1500 hydration
            container.DoLiquidMovedEffects(player, waterStack, waterStack.StackSize, BlockLiquidContainerBase.EnumLiquidDirection.Fill);
        }
        else if (block is BlockToolMold)
        {
            var be = world.BlockAccessor.GetBlockEntity(request.Position) as BlockEntityToolMold; 
            be?.CoolWithWater();
        }
        else if (block is BlockIngotMold)
        {
            var be = world.BlockAccessor.GetBlockEntity(request.Position) as BlockEntityIngotMold; 
            be?.CoolWithWater();
        }
        if (!ConfigSystem.ConfigServer.UrineStains || player.CurrentBlockSelection == null) return;
        if (!player.CurrentBlockSelection.Block.SideIsSolid(player.CurrentBlockSelection.Position, player.CurrentBlockSelection.Face.Index)) return;
        var rand = world.Rand.Next(0, 24);
        var x = rand % 6 + 1;
        var y = rand / 6 + 1;
        Block stain = world.GetBlock(new AssetLocation(BtCore.Modid, $"caveart-stain-urine-1-{x}-{y}"));
        if (SuitableStainPosition(world.BlockAccessor, player.CurrentBlockSelection))
            world.BlockAccessor.SetDecor(stain, request.Position,
                player.CurrentBlockSelection.ToDecorIndex());
    }
    
    private static void FertiliseFarmland(IWorldAccessor world, BlockPos position)
    {
        if (position == null) return;
        var be = world.BlockAccessor.GetBlockEntity(position) as BlockEntityFarmland; 
        be?.WaterFarmland(0.05f);
        if (ConfigSystem.ConfigServer.UrineNutrientChance > world.Rand.NextDouble())
        {
            be?.IncreaseNutrients(ConfigSystem.ConfigServer.UrineNutrientLevels);
        }
    }
    
    public static void SpawnPeeParticles(Entity byEntity, BlockPos pos, Vec3d hitPos, string color = null)
    {
        if (hitPos == null || pos == null) return;
        Vec3d entityPos = byEntity.Pos.XYZ.AddCopy(byEntity.LocalEyePos.SubCopy(0, 0.2, 0));
        Vec3d posVec = pos.ToVec3d().AddCopy(hitPos);
        Vec3d dist = (posVec - entityPos);
        var addVertical = new Vec3f(0, (float)(0.5f*Math.Sqrt(dist.NoY().LengthSq())), 0);
        var velocity = 2.5f * dist.ToVec3f().AddCopy(addVertical).Normalize();
        var xyz = entityPos.AddCopy(0.5 * dist.Normalize());
        var one = new Vec3f(1, 1, 1);

        _waterParticles = new SimpleParticleProperties(1f, 1f, -1, xyz, new Vec3d(), velocity.AddCopy(0.2f*one), velocity.AddCopy(-0.2f*one), minSize: 0.33f, maxSize: 0.75f)
        {
            AddPos = new Vec3d(),
            SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1f),
            ClimateColorMap = "climateWaterTint",
            AddQuantity = 5f,
            GravityEffect = 0.6f,
            ShouldDieInLiquid = true,
        };
        
        if (color != null)
        {
            if (color == "gaymer")
            {
                // Make it change color over time using a sin wave in hsv space
                _waterParticles.Color = ColorUtil.HsvToRgba((int)(Math.Sin(byEntity.World.ElapsedMilliseconds / 1000.0) * 0.5 + 0.5), 1, 1);
            }
            else
            {
                var colors = ColorUtil.Hex2Doubles(color);
                _waterParticles.Color = ColorUtil.ToRgba(120, (int)(colors[0]*255), (int)(colors[1]*255), (int)(colors[2]*255));
            }
        }
        
        byEntity.World.SpawnParticles(_waterParticles, byEntity is EntityPlayer entityPlayer ? entityPlayer.Player : null);
    }
    
    private bool SuitableStainPosition(IBlockAccessor blockAccessor, BlockSelection blockSel)
    {
        Block block = blockAccessor.GetBlock(blockSel.Position);
        if (block.SideSolid[blockSel.Face.Index] || block is BlockMicroBlock && (blockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityMicroBlock blockEntity ? (blockEntity.sideAlmostSolid[blockSel.Face.Index] ? 1 : 0) : 0) != 0)
        {
            EnumBlockMaterial blockMaterial = block.GetBlockMaterial(blockAccessor, blockSel.Position);
            return ConfigSystem.ConfigServer.UrineStainableMaterials.Any(t => blockMaterial == t);
        }
        return false;
    }
    
    #endregion
}