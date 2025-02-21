using System;
using System.Collections.Generic;
using BalancedThirst.BlockEntities;
using BalancedThirst.HarmonyPatches.BlockLiquidContainer;
using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BalancedThirst.Blocks;

public class BlockLiquidContainerSealable : BlockLiquidContainerBase
{

    public virtual float MinFillY => Attributes["minFillY"].AsFloat();

    public virtual float MaxFillY => Attributes["maxFillY"].AsFloat();
    public override bool CanDrinkFrom => true;
    public override bool IsTopOpened => true;
    public override bool AllowHeldLiquidTransfer => true;
    public virtual string EmptyShapeLoc => $"{BtCore.Modid}:shapes/block/{FirstCodePart()}/empty.json";
    public virtual string LidShapeLoc => $"{BtCore.Modid}:shapes/block/{FirstCodePart()}/lid.json";
    public virtual string ContentsShapeLoc => $"{BtCore.Modid}:shapes/block/{FirstCodePart()}/contents.json";
    
    public static WaterTightContainableProps GetInContainerProps(ItemStack stack)
    {
        try
        {
            JsonObject obj = stack?.ItemAttributes?["waterTightContainerProps"];
            if (obj != null && obj.Exists) return obj.AsObject<WaterTightContainableProps>(null, stack.Collectible.Code.Domain);
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        List<ItemStack> liquidContainerStacks = new List<ItemStack>();

        foreach (CollectibleObject obj in api.World.Collectibles)
        {
            if (obj is BlockLiquidContainerTopOpened || obj is ILiquidSource || obj is ILiquidSink || obj is BlockWateringCan)
            {
                List<ItemStack> stacks = obj.GetHandBookStacks((ICoreClientAPI)api);
                if (stacks != null) liquidContainerStacks.AddRange(stacks);
            }
        }

        return new WorldInteraction[]
                {
                new()
                {
                    ActionLangCode = "game:blockhelp-behavior-rightclickpickup",
                    MouseButton = EnumMouseButton.Right,
                    RequireFreeHand = true
                },
                new()
                {
                    ActionLangCode = "game:blockhelp-bucket-rightclick",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = liquidContainerStacks.ToArray()
                },
                new()
                {
                    ActionLangCode = $"{BtCore.Modid}:blockhelp-open", // json lang file. 
                    HotKeyCodes = new[] { "sneak", "sprint" },
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (_, bs, _) => {
                        BlockEntitySealable beSealable = world.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntitySealable;
                        return beSealable is { IsSealed: true };
                    }
                },
                new()
                {
                    ActionLangCode = $"{BtCore.Modid}:blockhelp-close", // json lang file. 
                    HotKeyCodes = new[] { "sneak", "sprint" },
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (_, bs, _) => {
                        BlockEntitySealable beSealable = world.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntitySealable;
                        return beSealable != null && !beSealable.IsSealed;
                    }
                }
        };
    }
    
     // We have overrides for TryPutLiquid, but these are almost carbon copies of the base method, won't remove *yet* incase we do want to write some custom behavior and the base code is a bit harder to read imo
    public override int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres) {
        if (liquidStack == null) return 0;

        var props = GetContainableProps(liquidStack);
        if (props == null) return 0;

        int desiredItems = (int)(props.ItemsPerLitre * desiredLitres);
        int availItems = liquidStack.StackSize;

        ItemStack stack = GetContent(containerStack);
        ILiquidSink sink = containerStack.Collectible as ILiquidSink;
        if (sink == null) return 0;
        if (stack == null)
        {
            if (!props.Containable) return 0;

            int placeableItems = (int)(sink.CapacityLitres * props.ItemsPerLitre);

            ItemStack placedstack = liquidStack.Clone();
            placedstack.StackSize = GameMath.Min(availItems, desiredItems, placeableItems);
            SetContent(containerStack, placedstack);

            return Math.Min(desiredItems, placeableItems);
        }
        else
        {
            if (!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes)) return 0;

            float maxItems = sink.CapacityLitres * props.ItemsPerLitre;
            int placeableItems = (int)(maxItems - stack.StackSize);

            stack.StackSize += GameMath.Min(placeableItems, desiredItems, availItems);
            return Math.Min(placeableItems, desiredItems);
        }
    }

    public override int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres) {
        if (liquidStack == null) return 0;

        var props = GetContainableProps(liquidStack);
        int desiredItems = (int)(props.ItemsPerLitre * desiredLitres);
        float availItems = liquidStack.StackSize;
        float maxItems = CapacityLitres * props.ItemsPerLitre;

        ItemStack stack = GetContent(pos);
        if (stack == null)
        {
            if (!props.Containable) return 0;

            int placeableItems = (int)GameMath.Min(desiredItems, maxItems, availItems);
            int movedItems = Math.Min(desiredItems, placeableItems);

            ItemStack placedstack = liquidStack.Clone();
            placedstack.StackSize = movedItems;
            SetContent(pos, placedstack);

            return movedItems;
        }
        else
        {
            if (!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes)) return 0;

            int placeableItems = (int)Math.Min(availItems, maxItems - stack.StackSize);
            int movedItems = Math.Min(placeableItems, desiredItems);

            stack.StackSize += GameMath.Min(movedItems);
            api.World.BlockAccessor.GetBlockEntity(pos).MarkDirty(true);
            (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer)?.Inventory[GetContainerSlotId(pos)].MarkDirty();

            return GameMath.Min(movedItems);
        }
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {

        BlockEntitySealable sp = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntitySealable;
        BlockPos pos = blockSel.Position;

        if (byPlayer.WorldData.EntityControls.Sneak && byPlayer.WorldData.EntityControls.Sprint)
        {
            if (sp != null && Attributes.IsTrue("canSeal"))
            {
                world.PlaySoundAt(AssetLocation.Create(Attributes["lidSound"].AsString("sounds/block"), Code.Domain), pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f, byPlayer);
                sp.IsSealed = !sp.IsSealed;
                sp.RedoMesh();
                sp.MarkDirty(true);
            }

            return true;
        }

        if (sp?.IsSealed == true) return false;
        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (!hotbarSlot.Empty && hotbarSlot.Itemstack.Collectible.Attributes?.IsTrue("handleLiquidContainerInteract") == true)
        {
            EnumHandHandling handling = EnumHandHandling.NotHandled;
            hotbarSlot.Itemstack.Collectible.OnHeldInteractStart(hotbarSlot, byPlayer.Entity, blockSel, null, true, ref handling);
            if (handling == EnumHandHandling.PreventDefault || handling == EnumHandHandling.PreventDefaultAction) return true;
        }

        if (hotbarSlot.Empty || hotbarSlot.Itemstack.Collectible is not ILiquidInterface) return base.OnBlockInteractStart(world, byPlayer, blockSel);

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
    {
        if (itemslot.Itemstack?.Attributes.GetBool("isSealed") == true) return;

        if (blockSel == null || byEntity.Controls.Sneak)
        {
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
            return;
        }

        if (AllowHeldLiquidTransfer)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;

            ItemStack contentStack = GetContent(itemslot.Itemstack);
            WaterTightContainableProps props = contentStack == null ? null : GetContentProps(contentStack);

            Block targetedBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);

            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byEntity.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
                byPlayer?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
                return;
            }

            if (!TryFillFromBlock(itemslot, byEntity, blockSel.Position))
            {
                BlockLiquidContainerTopOpened targetCntBlock = targetedBlock as BlockLiquidContainerTopOpened;
                if (targetCntBlock != null)
                {
                    if (targetCntBlock.TryPutLiquid(blockSel.Position, contentStack, targetCntBlock.CapacityLitres) > 0)
                    {
                        TryTakeContent(itemslot.Itemstack, 1);
                        if (props != null)
                            byEntity.World.PlaySoundAt(props.FillSpillSound, blockSel.Position.X, blockSel.Position.Y,
                                blockSel.Position.Z, byPlayer);
                    }

                }
                else
                {
                    if (byEntity.Controls.Sprint)
                    {
                        SpillContents(itemslot, byEntity, blockSel);
                    }
                }
            }
        }

        if (CanDrinkFrom)
        {
            if (GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null)
            {
                tryEatBegin(itemslot, byEntity, ref handHandling, "drink", 4);
                return;
            }
        }

        if (AllowHeldLiquidTransfer || CanDrinkFrom)
        {
            // Prevent placing on normal use
            handHandling = EnumHandHandling.PreventDefaultAction;
        }
    }

    protected bool SpillContents(ItemSlot containerSlot, EntityAgent byEntity, BlockSelection blockSel)
    {
        BlockPos pos = blockSel.Position;
        IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
        IBlockAccessor blockAcc = byEntity.World.BlockAccessor;
        BlockPos secondPos = blockSel.Position.AddCopy(blockSel.Face);
        var contentStack = GetContent(containerSlot.Itemstack);

        WaterTightContainableProps props = GetContentProps(containerSlot.Itemstack);

        if (props is not { AllowSpill: true } || props.WhenSpilled == null) return false;

        if (!byEntity.World.Claims.TryAccess(byPlayer, secondPos, EnumBlockAccessFlags.BuildOrBreak))
        {
            return false;
        }

        var action = props.WhenSpilled.Action;
        float currentlitres = GetCurrentLitres(containerSlot.Itemstack);

        if (currentlitres > 0 && currentlitres < 10)
        {
            action = WaterTightContainableProps.EnumSpilledAction.DropContents;
        }

        if (action == WaterTightContainableProps.EnumSpilledAction.PlaceBlock)
        {
            Block waterBlock = byEntity.World.GetBlock(props.WhenSpilled.Stack.Code);

            if (props.WhenSpilled.StackByFillLevel != null)
            {
                JsonItemStack fillLevelStack;
                props.WhenSpilled.StackByFillLevel.TryGetValue((int)currentlitres, out fillLevelStack);
                if (fillLevelStack != null) waterBlock = byEntity.World.GetBlock(fillLevelStack.Code);
            }

            Block currentblock = blockAcc.GetBlock(pos);
            if (currentblock.Replaceable >= 6000)
            {
                blockAcc.SetBlock(waterBlock.BlockId, pos);
                blockAcc.TriggerNeighbourBlockUpdate(pos);
                blockAcc.MarkBlockDirty(pos);
            }
            else
            {
                if (blockAcc.GetBlock(secondPos).Replaceable >= 6000)
                {
                    blockAcc.SetBlock(waterBlock.BlockId, secondPos);
                    blockAcc.TriggerNeighbourBlockUpdate(pos);
                    blockAcc.MarkBlockDirty(secondPos);
                }
                else
                {
                    return false;
                }
            }
        }

        if (action == WaterTightContainableProps.EnumSpilledAction.DropContents)
        {
            props.WhenSpilled.Stack.Resolve(byEntity.World, "liquidcontainerbasespill");

            ItemStack stack = props.WhenSpilled.Stack.ResolvedItemstack.Clone();
            stack.StackSize = contentStack.StackSize;

            byEntity.World.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(blockSel.HitPosition));
        }


        int moved = splitStackAndPerformAction(byEntity, containerSlot, (stack) => { SetContent(stack, null); return contentStack.StackSize; });

        DoLiquidMovedEffects(byPlayer, contentStack, moved, EnumLiquidDirection.Pour);
        BlockLiquidContainerBase_SpillContents_Patch.Postfix(this, true, containerSlot, byEntity, blockSel);
        return true;
    }

    protected int splitStackAndPerformAction(Entity byEntity, ItemSlot slot, System.Func<ItemStack, int> action)
    {
        if (slot.Itemstack == null) return 0;
        if (slot.Itemstack.StackSize == 1)
        {
            int moved = action(slot.Itemstack);

            if (moved > 0)
            {
                (byEntity as EntityPlayer)?.WalkInventory((pslot) =>
                {
                    if (pslot.Empty || pslot is ItemSlotCreative || pslot.StackSize == pslot.Itemstack.Collectible.MaxStackSize) return true;
                    int mergableq = slot.Itemstack.Collectible.GetMergableQuantity(slot.Itemstack, pslot.Itemstack, EnumMergePriority.DirectMerge);
                    if (mergableq == 0) return true;

                    var selfLiqBlock = slot.Itemstack.Collectible as BlockLiquidContainerBase;
                    var invLiqBlock = pslot.Itemstack.Collectible as BlockLiquidContainerBase;

                    if ((selfLiqBlock?.GetContent(slot.Itemstack)?.StackSize ?? 0) != (invLiqBlock?.GetContent(pslot.Itemstack)?.StackSize ?? 0)) return true;

                    slot.Itemstack.StackSize += mergableq;
                    pslot.TakeOut(mergableq);

                    slot.MarkDirty();
                    pslot.MarkDirty();
                    return true;
                });
            }

            return moved;
        }
        else
        {
            ItemStack containerStack = slot.Itemstack.Clone();
            containerStack.StackSize = 1;

            int moved = action(containerStack);

            if (moved > 0)
            {
                slot.TakeOut(1);
                if ((byEntity as EntityPlayer)?.Player.InventoryManager.TryGiveItemstack(containerStack, true) != true)
                {
                    api.World.SpawnItemEntity(containerStack, byEntity.SidedPos.XYZ);
                }

                slot.MarkDirty();
            }

            return moved;
        }
    }
    
    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<int, MultiTextureMeshRef> meshrefs;
        bool isSealed = itemstack.Attributes.GetBool("isSealed");

        object obj;
        
        var variantStringBuilder = new System.Text.StringBuilder();
        foreach (var entry in Variant)
        {
            variantStringBuilder.Append($"{entry.Value}-");
        }
        string variantString = variantStringBuilder.Length > 0 ? variantStringBuilder.ToString(0, variantStringBuilder.Length - 2) : "";
        
        if (capi.ObjectCache.TryGetValue(variantString + "MeshRefs", out obj))
        {
            meshrefs = obj as Dictionary<int, MultiTextureMeshRef>;
        }
        else
        {
            capi.ObjectCache[variantString + "MeshRefs"] = meshrefs = new Dictionary<int, MultiTextureMeshRef>();
        }
        ItemStack contentStack = GetContent(itemstack);
        if (contentStack == null) return;
        int hashcode = GetContainerHashCode(capi.World, contentStack, isSealed);
        MultiTextureMeshRef meshRef = null;
        if (meshrefs != null && !meshrefs.TryGetValue(hashcode, out meshRef))
        {
            MeshData meshdata = GenRightMesh(capi, contentStack, null, isSealed);
            meshrefs[hashcode] = meshRef = capi.Render.UploadMultiTextureMesh(meshdata);

        }
        if (meshRef != null) { renderinfo.ModelRef = meshRef; }

    }
    
    public virtual int GetContainerHashCode(IClientWorldAccessor world, ItemStack contentStack, bool isSealed)
    {
        string s = contentStack.StackSize + "x" + contentStack.Collectible.Code.ToShortString();
        if (isSealed) s += "sealed";
        return s.GetHashCode();
    }
    
    // Works only if the shape hierarchy has been flattened, it must not have any element with children
    public Shape SliceFlattenedShape(Shape fullShape, float fullness)
    {
        var minY = MinFillY;
        var maxY = MaxFillY;
        
        var newMaxY = minY + (maxY - minY) * fullness;
        List<ShapeElement> newElements = new List<ShapeElement>();

        foreach (var element in fullShape.Elements)
        {
            double elementMinY = Math.Min(element.From[1], element.To[1]);
            double elementMaxY = Math.Max(element.From[1], element.To[1]);
        
            if (elementMaxY < minY || elementMinY > newMaxY) continue;
        
            var newElement = element.Clone();
            double adjustedFromY = Math.Max(element.From[1], 0);
            double adjustedToY = Math.Min(element.To[1], newMaxY);
            if (!(adjustedFromY <= adjustedToY)) continue;
            newElement.From[1] = adjustedFromY;
            newElement.To[1] = adjustedToY;
            
            // Calculate the proportion of the adjustment
            double originalHeight = elementMaxY - elementMinY;
            double newHeight = adjustedToY - adjustedFromY;
            double heightProportion = originalHeight > 0 ? newHeight / originalHeight : 0;
            
            for (int i = 0; i < 4; i++)
            {
                var face = newElement.FacesResolved[i];
                if (face != null)
                {
                    double vMin = face.Uv[1];
                    double vMax = face.Uv[3];
                    double vRange = vMax - vMin;
                
                    // Adjust the V values based on the height proportion
                    face.Uv[1] = (float)(vMin + vRange * (1 - heightProportion));
                    face.Uv[3] = (float)vMax;
                }
            }
            newElements.Add(newElement);
        }

        var partialShape = fullShape.Clone();
        partialShape.Elements = newElements.ToArray();
        return partialShape;
    }


    public MeshData GenRightMesh(ICoreClientAPI capi, ItemStack contentStack, BlockPos forBlockPos = null, bool isSealed = false) {
        Shape shape = capi.Assets.TryGet(isSealed && Attributes.IsTrue("canSeal") ? LidShapeLoc : EmptyShapeLoc).ToObject<Shape>();
        MeshData bucketmesh;
        capi.Tesselator.TesselateShape(this, shape, out bucketmesh);

        if (contentStack != null)
        {
            WaterTightContainableProps props = GetInContainerProps(contentStack);
            ContainerTextureSource contentSource = new ContainerTextureSource(capi, contentStack, props.Texture);
            MeshData contentMesh;
            float fullness = contentStack.StackSize / (props.ItemsPerLitre * CapacityLitres);

            if (props.Texture == null) return null;

            shape = capi.Assets.TryGet(ContentsShapeLoc).ToObject<Shape>();
            
            shape = SliceFlattenedShape(shape.FlattenHierarchy(), fullness);
            
            capi.Tesselator.TesselateShape("sealableContainer", shape, out contentMesh, contentSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ));

            if (props.ClimateColorMap != null)
            {
                int col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, 196, 128, false);
                if (forBlockPos != null)
                {
                    col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, false);
                }

                byte[] rgba = ColorUtil.ToBGRABytes(col);

                for (int i = 0; i < contentMesh.Rgba.Length; i++)
                {
                    contentMesh.Rgba[i] = (byte)((contentMesh.Rgba[i] * rgba[i % 4]) / 255);
                }
            }

            for (int i = 0; i < contentMesh.Flags.Length; i++)
            {
                contentMesh.Flags[i] = contentMesh.Flags[i] & ~(1 << 12); // Remove water waving flag
            }

            bucketmesh.AddMeshData(contentMesh);

            // Water flags
            if (forBlockPos != null)
            {
                bucketmesh.CustomInts = new CustomMeshDataPartInt(bucketmesh.FlagsCount);
                bucketmesh.CustomInts.Count = bucketmesh.FlagsCount;
                bucketmesh.CustomInts.Values.Fill(0x4000000); // light foam only

                bucketmesh.CustomFloats = new CustomMeshDataPartFloat(bucketmesh.FlagsCount * 2);
                bucketmesh.CustomFloats.Count = bucketmesh.FlagsCount * 2;
            }
        }
        return bucketmesh;
    }

    public override bool DoPlaceBlock(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        ItemStack byItemStack)
    {
        int num1 = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack) ? 1 : 0;
        if (num1 == 0)
            return false;
        if (!(world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntitySealable blockEntity))
            return true;
        BlockPos blockPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
        double num2 = Math.Atan2(byPlayer.Entity.Pos.X - (blockPos.X + blockSel.HitPosition.X), byPlayer.Entity.Pos.Z - (blockPos.Z + blockSel.HitPosition.Z));
        float num3 = 0.3926991f;
        double num4 = num3;
        float num5 = (int) Math.Round(num2 / num4) * num3;
        blockEntity.MeshAngle = num5;
        return true;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        ItemStack drop = base.OnPickBlock(world, pos);

        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntitySealable sp)
        {
            drop.Attributes.SetBool("isSealed", sp.IsSealed);
        }

        return drop;
    }
}