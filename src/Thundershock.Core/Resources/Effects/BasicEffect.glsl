// "program" defines the name of the actual effect.
#pragma ts program BasicEffect
    #pragma ts shader VertexShader
        #version 330 core

        layout(location = 0) in vec4 position;
        layout(location = 1) in vec4 color;
        layout(location = 2) in vec2 texCoord;

        uniform mat4 projection;

        out vec2 fragTexture;
        out vec4 fragColor;

        void main() {
            gl_Position = position * projection;
            fragTexture = texCoord;
            fragColor = color;
        }
    #pragma ts end

    #pragma ts shader FragmentShader
        #version 330 core

        in vec2 fragTexture;
        in vec4 fragColor;

        uniform sampler2D tex0;

        out vec4 color;

        void main() {
            color = texture(tex0, fragTexture) * fragColor;
        }
    #pragma ts end

    #pragma ts compile vert VertexShader
    #pragma ts compile frag FragmentShader
#pragma ts end