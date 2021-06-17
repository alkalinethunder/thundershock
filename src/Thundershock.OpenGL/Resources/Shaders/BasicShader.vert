#version 330 core

layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 texCoord;

varying vec2 fragTexture;
varying vec4 fragColor;

void main() {
    gl_Position = position;
    fragTexture = texCoord;
    fragColor = color;
}