using BalancedThirst.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BalancedThirst.BlockEntities;

public class BlockEntitySealable : BlockEntityBucket
{
    private MeshData _currentRightMesh;
    private BlockLiquidContainerSealable _ownBlock;
    public bool IsSealed;
    public new float MeshAngle;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        _ownBlock = Block as BlockLiquidContainerSealable;
        container.Inventory.OnAcquireTransitionSpeed += (type, stack, mul) =>
        {
            if (type == EnumTransitionType.Perish)
            {
                return mul * (IsSealed ? Block.Attributes["lidPerishRate"].AsFloat(0.5f) : 1f);
            }
            return mul;
        };


        if (Api.Side == EnumAppSide.Client)
        {
            _currentRightMesh = GenRightMesh();
            MarkDirty(true);
        }
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        if (byItemStack != null) IsSealed = byItemStack.Attributes.GetBool("isSealed");

        if (Api.Side == EnumAppSide.Client)
        {
            _currentRightMesh = GenRightMesh();
            MarkDirty(true);
        }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
        IsSealed = tree.GetBool("isSealed");

        if (Api?.Side == EnumAppSide.Client)
        {
            _currentRightMesh = GenRightMesh();
            MarkDirty(true);
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetFloat("meshAngle", this.MeshAngle);
        tree.SetBool("isSealed", IsSealed);
    }

    internal MeshData GenRightMesh()
    {
        //if (ownBlock == null || ownBlock.Code.Path.Contains("clay")) return null;

        MeshData mesh = _ownBlock?.GenRightMesh(Api as ICoreClientAPI, GetContent(), Pos, IsSealed);

        if (mesh?.CustomInts != null)
        {
            for (int i = 0; i < mesh.CustomInts.Count; i++)
            {
                mesh.CustomInts.Values[i] |= 1 << 27; // Disable water wavy
                mesh.CustomInts.Values[i] |= 1 << 26; // Enabled weak foam
            }
        }

        return mesh;
    }
    
    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        if (this._currentRightMesh != null)
            mesher.AddMeshData(this._currentRightMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0.0f, this.MeshAngle, 0.0f));
        return true;
    }

    public void RedoMesh()
    {
        if (Api.Side == EnumAppSide.Client)
        {
            _currentRightMesh = GenRightMesh();
        }
    }
}