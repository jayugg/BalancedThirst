namespace BalancedThirst.Util;

public static class ShaderCodes
{
    public static string GetVertexShaderCode()
    {
        return @"
#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertex;

out vec2 uv;

void main(void)
{
    gl_Position = vec4(vertex.xy, 0, 1);
    uv = (vertex.xy + 1.0) / 2.0;
}
            ";
    }
    
    public static string GetFragmentShaderCode()
    {
        return @"
#version 330 core

in vec2 uv;
out vec4 outColor;
uniform float frostVignetting;

uniform sampler2D primaryScene;

#include noise3d.ash

float SmoothStep(float x) { return x * x * (3.0f - 2.0f * x); }

void main(void)
{
    vec4 color = texture(primaryScene, uv);
    vec2 position = (uv * 2.0) - vec2(1.0);
    float grayvignette = 1.0 - smoothstep(1.1, 0.75 - 0.45, length(position));

    if (frostVignetting > 0) {
        float str = -0.05 + 1.05*clamp(1 - smoothstep(1.1 - frostVignetting / 4, 0.75 - 0.45, length(position)), 0, 1) - grayvignette;
        float g = 0;

        float wx = gnoise(vec3(gl_FragCoord.x / 20.0, str, gl_FragCoord.x / 11.0 + gl_FragCoord.y / 10.0));
        float wy = gnoise(vec3(gl_FragCoord.x / 20.0, str, gl_FragCoord.x / 10.0 - gl_FragCoord.y / 9.0));

        g = 2*gnoise(vec3(wx / 3.0, wy / 3.0, 0.2)) + 0.8;
        g *= gnoise(vec3(gl_FragCoord.x / 20.0, gl_FragCoord.y / 20.0, 1.5)) + 0.2;
        g -= gnoise(vec3(wx * 2.0, wy * 2.0, 1))/5;
        g -= str*2;
        g *= frostVignetting;

        vec3 vignetteColor = vec3(0.9 + gnoise(vec3(wx, -wy, 0)) / 15.0, 0.9 + gnoise(vec3(wx, wy, 0)) / 15.0, 0.95);

        outColor.rgb = mix(outColor.rgb, vignetteColor, max(0, str - g) + 0.5*str);
    }

    outColor.rgb = mix(outColor.rgb, vec3(0), grayvignette);
    outColor.a=1;
}
            ";
    }
    
}