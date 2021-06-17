#version 330 core

out vec4 color;

varying vec4 fragColor;

void main() {
    color = fragColor;
}