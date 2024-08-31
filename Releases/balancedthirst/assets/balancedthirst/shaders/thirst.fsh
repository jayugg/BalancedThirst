#version 330 core

uniform sampler2D primaryScene;
uniform float dehydrationVignetting;

in vec2 texCoord;
layout(location = 0) out vec4 outColor;

void main(void)
{
    vec4 color = texture(primaryScene, texCoord);
    vec2 position = (gl_FragCoord.xy / vec2(textureSize(primaryScene, 0))) - vec2(0.5);
    float grayvignette = 1.0 - smoothstep(1.1, 0.75 - 0.45, length(position));

    if (dehydrationVignetting > 0.0) {
        float str = clamp(1.0 - smoothstep(1.1 - dehydrationVignetting / 4.0, 0.75 - 0.45, length(position)), 0.0, 1.0) - grayvignette;
        float g = 0.0;
        g = fract(sin(dot(gl_FragCoord.xy, vec2(12.9898, 78.233))) * 43758.5453) + 0.5;
        g += fract(sin(dot(gl_FragCoord.xy, vec2(26.651, 47.303))) * 43758.5453) / 5.0;
        g -= str * 2.0;
        g *= dehydrationVignetting;
        vec3 vignetteColor = vec3(0.8 * dehydrationVignetting / 2.0, 0.0, 0.0);
        outColor.rgb = mix(color.rgb, vignetteColor, max(0.0, str - g));
    } else {
        outColor = color;
    }
}