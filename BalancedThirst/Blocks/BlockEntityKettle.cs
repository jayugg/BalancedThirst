using Vintagestory.API.MathTools;

namespace BalancedThirst.Blocks;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

public class BlockEntityKettle : BlockEntityBucket
{
    MeshData currentRightMesh;
    BlockKettle ownBlock;
    public bool isSealed;
    public float MeshAngle;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        ownBlock = Block as BlockKettle;


        if (Api.Side == EnumAppSide.Client)
        {
            currentRightMesh = GenRightMesh();
            MarkDirty(true);
        }
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        if (byItemStack != null) isSealed = byItemStack.Attributes.GetBool("isSealed");

        if (Api.Side == EnumAppSide.Client)
        {
            currentRightMesh = GenRightMesh();
            MarkDirty(true);
        }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
        isSealed = tree.GetBool("isSealed");

        if (Api?.Side == EnumAppSide.Client)
        {
            currentRightMesh = GenRightMesh();
            MarkDirty(true);
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetFloat("meshAngle", this.MeshAngle);
        tree.SetBool("isSealed", isSealed);
    }

    internal MeshData GenRightMesh()
    {
        //if (ownBlock == null || ownBlock.Code.Path.Contains("clay")) return null;

        MeshData mesh = ownBlock.GenRightMesh(Api as ICoreClientAPI, GetContent(), Pos, isSealed);

        if (mesh.CustomInts != null)
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
        if (this.currentRightMesh != null)
            mesher.AddMeshData(this.currentRightMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0.0f, this.MeshAngle, 0.0f));
        return true;
    }

    public void RedoMesh()
    {
        if (Api.Side == EnumAppSide.Client)
        {
            currentRightMesh = GenRightMesh();
        }
    }


    public override float GetPerishRate()
    {
        return base.GetPerishRate() * (isSealed ? Block.Attributes["lidPerishRate"].AsFloat(0.5f) : 1f);
    }
}