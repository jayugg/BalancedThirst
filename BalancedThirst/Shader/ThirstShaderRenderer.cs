using Vintagestory.API.Client;

namespace BalancedThirst.Shader;

public class ThirstShaderRenderer : IRenderer
{
    private MeshRef quadRef;
    private ICoreClientAPI capi;
    public IShaderProgram overlayShaderProg;

    public ThirstShaderRenderer(ICoreClientAPI capi, IShaderProgram overlayShaderProg)
    {
        this.capi = capi;
        this.overlayShaderProg = overlayShaderProg;

        var quadMesh = QuadMeshUtil.GetCustomQuadModelData(-1, -1, 0, 2, 2);
        quadMesh.Rgba = null;

        quadRef = capi.Render.UploadMesh(quadMesh);
    }

    public double RenderOrder => 1.1;

    public int RenderRange { get { return 1; } }

    public void Dispose()
    {
        capi.Render.DeleteMesh(quadRef);
        overlayShaderProg.Dispose();
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        var curShader = capi.Render.CurrentActiveShader;
        curShader?.Stop();
        overlayShaderProg?.Use();
        capi.Render.GlToggleBlend(true);
        
        overlayShaderProg?.Uniform("vomitVignetting", 100f);
        overlayShaderProg?.Uniform("dehydrationVignetting", 100f);
        overlayShaderProg?.BindTexture2D("primaryScene",
            capi.Render.FrameBuffers[(int)EnumFrameBuffer.Primary].ColorTextureIds[0], 0);

        capi.Render.RenderMesh(quadRef);
        overlayShaderProg?.Stop();
        curShader?.Use();
    }
}