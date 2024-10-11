#version 330 core

in vec2 uv;
out vec4 outColor;

uniform sampler2D primaryScene;
uniform float vomitVignetting;

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
    
    if (vomitVignetting > 0) {
        float str = clamp(1 - smoothstep(1.1 - vomitVignetting / 4, 0.75 - 0.45, length(position)), 0, 1) - grayvignette;
        float g = 0;

        g = gnoise(vec3(gl_FragCoord.x / 20.0, gl_FragCoord.y / 20.0, 0)) + 0.5;
        g += gnoise(vec3(gl_FragCoord.x / 5.0, gl_FragCoord.y / 5.0, 0))/5;
        g -= str*2;

        g*=vomitVignetting;

        vec3 vignetteColor = vec3(0.8 * vomitVignetting/2, 0.4, 0.4);
        outColor.rgb = mix(outColor.rgb, vignetteColor, max(0, str - g));
    }

    outColor.rgb = mix(outColor.rgb, vec3(0), grayvignette);
    outColor.a=1;
}

/*
float SmoothStep(float x) { return x * x * (3.0f - 2.0f * x); }


// discontinuous pseudorandom uniformly distributed in [-0.5, +0.5]^3
vec3 random3(vec3 c) {
    float j = 4096.0*sin(dot(c,vec3(17.0, 59.4, 15.0)));
    vec3 r;
    r.z = fract(512.0*j);
    j *= .125;
    r.x = fract(512.0*j);
    j *= .125;
    r.y = fract(512.0*j);
    return r-0.5;
}

// skew constants for 3d simplex functions
const float F3 =  0.3333333;
const float G3 =  0.1666667;

// 3d simplex nois
float gnoise(vec3 p) {
    // 1. find current tetrahedron T and it's four vertices
    // s, s+i1, s+i2, s+1.0 - absolute skewed (integer) coordinates of T vertices
    // x, x1, x2, x3 - unskewed coordinates of p relative to each of T vertices

    // calculate s and x 
    vec3 s = floor(p + dot(p, vec3(F3)));
    vec3 x = p - s + dot(s, vec3(G3));

    // calculate i1 and i2
    vec3 e = step(vec3(0.0), x - x.yzx);
    vec3 i1 = e*(1.0 - e.zxy);
    vec3 i2 = 1.0 - e.zxy*(1.0 - e);

    // x1, x2, x3
    vec3 x1 = x - i1 + G3;
    vec3 x2 = x - i2 + 2.0*G3;
    vec3 x3 = x - 1.0 + 3.0*G3;

    // 2. find four surflets and store them in d
    vec4 w, d;

    // calculate surflet weights
    w.x = dot(x, x);
    w.y = dot(x1, x1);
    w.z = dot(x2, x2);
    w.w = dot(x3, x3);

    // w fades from 0.6 at the center of the surflet to 0.0 at the margin
    w = max(0.6 - w, 0.0);

    // calculate surflet components
    d.x = dot(random3(s), x);
    d.y = dot(random3(s + i1), x1);
    d.z = dot(random3(s + i2), x2);
    d.w = dot(random3(s + 1.0), x3);

    // multiply d by w^4
    w *= w;
    w *= w;
    d *= w;

    // 3. return the sum of the four surflets
    return dot(d, vec4(52.0));
}


void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    float frostVignetting = 10.0;
    // Normalized pixel coordinates (from 0 to 1)
    vec2 uv = fragCoord/iResolution.xy;

    fragColor = texture(iChannel0, uv);
    vec2 position = (uv * 2.0) - vec2(1.0);
    float grayvignette = 1.0 - smoothstep(1.1, 0.75 - 0.45, length(position));

    if (frostVignetting > 0.0) {
        float str = -0.05 + 1.05*clamp(1.0 - smoothstep(1.1 - frostVignetting / 4.0, 0.75 - 0.45, length(position)), 0.0, 1.0) - grayvignette;
        float g = 0.0;

        float wx = gnoise(vec3(fragCoord.x / 20.0, str, fragCoord.x / 11.0 + fragCoord.y / 10.0));
        float wy = gnoise(vec3(fragCoord.x / 20.0, str, fragCoord.x / 10.0 - fragCoord.y / 9.0));

        g = 2.0*gnoise(vec3(wx / 3.0, wy / 3.0, 0.2)) + 0.8;
        g *= gnoise(vec3(fragCoord.x / 20.0, fragCoord.y / 20.0, 1.5)) + 0.2;
        g -= gnoise(vec3(wx * 2.0, wy * 2.0, 1))/5.0;
        g -= str*2.0;
        g *= frostVignetting;

        vec3 vignetteColor = vec3(0.9 + gnoise(vec3(wx, -wy, 0)) / 15.0, 0.9 + gnoise(vec3(wx, wy, 0)) / 15.0, 0.95);

        fragColor.rgb = mix(fragColor.rgb, vignetteColor, max(0.0, str - g) + 0.5*str);
    }

    fragColor.rgb = mix(fragColor.rgb, vec3(0), grayvignette);
    fragColor.a=1.0;
}
*/