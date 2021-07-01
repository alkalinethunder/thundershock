#version 330 core

out vec4 color;

uniform sampler2D ts_textureSampler;

varying vec2 fragTexture;
varying vec4 fragColor;

void main() {
    color = texture(ts_textureSampler, fragTexture) * fragColor;
}