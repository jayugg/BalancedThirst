using System.Collections.Generic;
using BalancedThirst.Systems;
using BalancedThirst.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BalancedThirst.Blocks;
public class KettleInFirepitRenderer : IInFirepitRenderer
{
    public double RenderOrder => 0.5;
    public int RenderRange => 20;

    public float Origx;
    public float Origz;

    private ICoreClientAPI _capi;
    private ItemStack _stack;

    private MultiTextureMeshRef _kettleRef;
    private MultiTextureMeshRef _contentRef;
    private MultiTextureMeshRef _topRef;
    private BlockPos _pos;
    private float _temp;

    private ILoadedSound _cookingSound;

    private bool _isInOutputSlot;
    private Matrixf _newModelMat = new Matrixf();

    public KettleInFirepitRenderer(ICoreClientAPI capi, ItemStack stack, BlockPos pos, bool isInOutputSlot)
    {
        _capi = capi;
        _stack = stack;
        _pos = pos;
        _isInOutputSlot = isInOutputSlot;

        var variant = stack.Collectible.Code.EndVariant();

        var kettleBlock =
            capi.World.GetBlock(stack.Collectible.CodeWithVariant("metal", variant)) as BlockKettle;
        if (kettleBlock == null) return;

        if (stack.Collectible.CodeWithVariant("metal", variant) == null) { kettleBlock = capi.World.GetBlock(stack.Collectible.CodeWithVariant("metal", "")) as BlockKettle; }

        var shape = capi.Assets.TryGet($"{BtCore.Modid}:shapes/block/{kettleBlock?.FirstCodePart()}/empty.json")
            .ToObject<Shape>();
        if (stack.Collectible.Code.Equals(stack.Collectible.CodeWithVariant("metal", "fired")))
            shape = shape.FlattenHierarchy().RemoveReflective();
        MeshData kettleMesh;
        capi.Tesselator.TesselateShape(kettleBlock, shape, out kettleMesh); // Main Shape
        _kettleRef = capi.Render.UploadMultiTextureMesh(kettleMesh);

        MeshData topMesh;
        capi.Tesselator.TesselateShape(kettleBlock, capi.Assets.TryGet($"{BtCore.Modid}:shapes/block/{kettleBlock?.FirstCodePart()}/lid-only.json").ToObject<Shape>(), out topMesh); // Lid
        _topRef = capi.Render.UploadMultiTextureMesh(topMesh);
        
        if (stack.Collectible is not BlockKettle kettle) return;
        var contentStack = kettle.GetContent(stack);
        var props = BlockLiquidContainerSealable.GetInContainerProps(contentStack);
        if (props?.Texture == null) return;
        var contentSource = new ContainerTextureSource(capi, contentStack, props.Texture);
        MeshData contentMesh;
        var fullness = contentStack?.StackSize / (props.ItemsPerLitre * kettleBlock?.CapacityLitres) ?? 0;
        
        var contentShape = capi.Assets.TryGet($"{BtCore.Modid}:shapes/block/{kettleBlock?.FirstCodePart()}/contents.json").ToObject<Shape>();
            
        contentShape = kettle.SliceFlattenedShape(contentShape.FlattenHierarchy(), fullness);
            
        capi.Tesselator.TesselateShape("kettle", contentShape, out contentMesh, contentSource);
        
        _contentRef = capi.Render.UploadMultiTextureMesh(contentMesh);
    }

    public void Dispose()
    {
        _kettleRef?.Dispose();
        _topRef?.Dispose();
        _contentRef?.Dispose();

        _cookingSound?.Stop();
        _cookingSound?.Dispose();
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (_kettleRef == null)
        {
            BtCore.Logger.Warning("Kettle mesh is null, this usually happens if you haven't reset your cache after updating the mod.");
            return;
        }
        
        var rpi = _capi.Render;
        var camPos = _capi.World.Player.Entity.CameraPos;

        rpi.GlDisableCullFace();
        rpi.GlToggleBlend(true);

        var prog = rpi.PreparedStandardShader(_pos.X, _pos.Y, _pos.Z);
        
        prog.DontWarpVertices = 0;
        prog.AddRenderFlags = 0;
        prog.RgbaAmbientIn = rpi.AmbientColor;
        prog.RgbaFogIn = rpi.FogColor;
        prog.FogMinIn = rpi.FogMin;
        prog.FogDensityIn = rpi.FogDensity;
        prog.RgbaTint = ColorUtil.WhiteArgbVec;
        prog.NormalShaded = 1;
        prog.ExtraGodray = 0;
        prog.SsaoAttn = 0;
        prog.AlphaTest = 0.05f;
        prog.OverlayOpacity = 0;


        prog.ModelMatrix = _newModelMat
            .Identity()
            .Translate(_pos.X - camPos.X + 0.001f, _pos.Y - camPos.Y, _pos.Z - camPos.Z - 0.001f)
            .Translate(0f, 1 / 16f, 0f)
            .Values
        ;

        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
        
        rpi.RenderMultiTextureMesh(_kettleRef, "tex");

        if (!_isInOutputSlot)
        {
             Origx = GameMath.Sin(_capi.World.ElapsedMilliseconds / 300f) * 8 / 16f;
             Origz = GameMath.Cos(_capi.World.ElapsedMilliseconds / 300f) * 8 / 16f;

            var cookIntensity = GameMath.Clamp((_temp - 50) / 50, 0, 1);

            prog.ModelMatrix = _newModelMat
                .Identity()
                .Translate(_pos.X - camPos.X, _pos.Y - camPos.Y, _pos.Z - camPos.Z)
                .Translate(0, 1f / 16f, 0)
                .Translate(-Origx, 0, -Origz)
                .RotateX(cookIntensity * GameMath.Sin(_capi.World.ElapsedMilliseconds / 70f) / 100) // moving of the lid
                .RotateZ(cookIntensity * GameMath.Sin(_capi.World.ElapsedMilliseconds / 70f) / 100) //moving of the lidc
                .Translate(Origx, 0, Origz)
                .Values
            ;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;


            rpi.RenderMultiTextureMesh(_topRef, "tex");
        }
        else if (_contentRef != null)
        {
            rpi.RenderMultiTextureMesh(_contentRef, "tex");
        }

        prog.Stop();
    }

    public void OnUpdate(float temperature)
    {
        _temp = temperature;

        var soundIntensity = GameMath.Clamp((_temp - 50) / 50, 0, 1);
        SetCookingSoundVolume(_isInOutputSlot ? 0 : soundIntensity);
    }

    public void OnCookingComplete()
    {
        _isInOutputSlot = true;
    }


    public void SetCookingSoundVolume(float volume)
    {
        if (volume > 0)
        {

            if (_cookingSound == null)
            {
                _cookingSound = _capi.World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("sounds/effect/cooking.ogg"),
                    ShouldLoop = true,
                    Position = _pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = volume
                });
                _cookingSound.Start();
            }
            else
            {
                _cookingSound.SetVolume(volume);
            }

        }
        else
        {
            if (_cookingSound != null)
            {
                _cookingSound.Stop();
                _cookingSound.Dispose();
                _cookingSound = null;
            }

        }

    }
}
