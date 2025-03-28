using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BalancedThirst.Shader;

 public class ThirstShaderProgram : ShaderProgram
  {

    public float DehydrationVignetting
    {
      set => Uniform("dehydrationVignetting", value);
    }
    
    public float VomitVignetting
    {
      set => Uniform("vomitVignetting", value);
    }
    
  }