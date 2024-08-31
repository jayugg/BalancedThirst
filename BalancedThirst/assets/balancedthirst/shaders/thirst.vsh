#version 330 core

layout(location = 0) in vec3 vertex;

uniform vec2 frameSize;

out vec2 texCoord;
out vec2 invFrameSize;

void main(void)
{
    gl_Position = vec4(vertex.xy, 0.0, 1.0);
    texCoord = (vertex.xy + 1.0) / 2.0;
    invFrameSize = 1.0 / frameSize;
}