#version 330 core

layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 texCoord;

uniform mat4 ts_cam_projectionMatrix;

varying vec2 fragTexture;
varying vec4 fragColor;

void main() {
    gl_Position = position * ts_cam_projectionMatrix;
    fragTexture = texCoord;
    fragColor = color;
}