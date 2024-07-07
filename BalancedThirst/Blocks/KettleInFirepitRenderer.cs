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

    ICoreClientAPI _capi;
    ItemStack _stack;

    MultiTextureMeshRef _saucepanRef;
    MultiTextureMeshRef _topRef;
    BlockPos _pos;
    float _temp;

    ILoadedSound _cookingSound;

    bool _isInOutputSlot;
    Matrixf _newModelMat = new Matrixf();

    public KettleInFirepitRenderer(ICoreClientAPI capi, ItemStack stack, BlockPos pos, bool isInOutputSlot)
    {
        this._capi = capi;
        this._stack = stack;
        this._pos = pos;
        this._isInOutputSlot = isInOutputSlot;

        BlockKettle kettleBlock = capi.World.GetBlock(stack.Collectible.CodeWithVariant("type", "fired")) as BlockKettle;
        if (kettleBlock == null) return;

        if (stack.Collectible.CodeWithVariant("type", "fired") == null) { kettleBlock = capi.World.GetBlock(stack.Collectible.CodeWithVariant("metal", "")) as BlockKettle; }

        MeshData kettleMesh;
        capi.Tesselator.TesselateShape(kettleBlock, capi.Assets.TryGet($"{BtCore.Modid}:shapes/block/" + kettleBlock?.FirstCodePart() + "/" + "empty.json").ToObject<Shape>(), out kettleMesh); // Main Shape
        _saucepanRef = capi.Render.UploadMultiTextureMesh(kettleMesh);

        MeshData topMesh;
        capi.Tesselator.TesselateShape(kettleBlock, capi.Assets.TryGet($"{BtCore.Modid}:shapes/block/" + kettleBlock?.FirstCodePart() + "/" + "lid-only.json").ToObject<Shape>(), out topMesh); // Lid
        _topRef = capi.Render.UploadMultiTextureMesh(topMesh);
    
    }

    public void Dispose()
    {
        _saucepanRef?.Dispose();
        _topRef?.Dispose();

        _cookingSound?.Stop();
        _cookingSound?.Dispose();
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        IRenderAPI rpi = _capi.Render;
        Vec3d camPos = _capi.World.Player.Entity.CameraPos;

        rpi.GlDisableCullFace();
        rpi.GlToggleBlend(true);

        IStandardShaderProgram prog = rpi.PreparedStandardShader(_pos.X, _pos.Y, _pos.Z);

        prog.Tex2D = _capi.BlockTextureAtlas.AtlasTextures[0].TextureId;
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

        rpi.RenderMultiTextureMesh(_saucepanRef);

        if (!_isInOutputSlot)
        {
             Origx = GameMath.Sin(_capi.World.ElapsedMilliseconds / 300f) * 8 / 16f;
             Origz = GameMath.Cos(_capi.World.ElapsedMilliseconds / 300f) * 8 / 16f;

            float cookIntensity = GameMath.Clamp((_temp - 50) / 50, 0, 1);

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


            rpi.RenderMultiTextureMesh(_topRef);
        }

        prog.Stop();
    }

    public void OnUpdate(float temperature)
    {
        _temp = temperature;

        float soundIntensity = GameMath.Clamp((_temp - 50) / 50, 0, 1);
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
