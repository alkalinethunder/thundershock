// THUNDERSHOCK POST-PROCESSOR SHADER PROGRAMS
//
// This is a multi-program effect containing all of the necessary
// shaders for the Thundershock post-processor. Each program is for a separate
// pass of the post-process pipeline.

// We start with the bloom effect's brightness threshold pass.
// This will render an image where any fragments below a certain brightness are
// discarded.

#pragma ts program BloomThreshold
    #pragma ts shader VertexShader
        #version 330 core
        
        layout(location = 0) in vec4 position;
        layout(location = 1) in vec4 color;
        layout(location = 2) in vec2 texCoord;

        uniform mat4 transform;

        out vec2 fragTexture;
        out vec4 fragColor;

        void main() {
            gl_Position = position * transform; // No need for projection inside of a PP effect.
            fragColor = color;
            fragTexture = texCoord;
        }
    #pragma ts end
    
    #pragma ts shader BloomThresholdFrag
        #version 330 core
        
        in vec2 fragTexture;
        in vec4 fragColor;
        
        uniform float threshold;
        uniform sampler2D tex1;
        
        out vec4 color;
        
        void main() {
            vec4 texColor = texture(tex1, fragTexture) * fragColor;
            float brightness = dot(vec3(texColor), vec3(0.3, 0.59, 0.11));
            if (brightness < threshold) {
                texColor.r = 0;
                texColor.g = 0;
                texColor.b = 0;
            }
            
            color = texColor;
        }
    #pragma ts end
    
    #pragma ts compile vert VertexShader
    #pragma ts compile frag BloomThresholdFrag
#pragma ts end

// Gaussian blur shader for the bloom effect.
#pragma ts program BloomGaussian
    #pragma ts shader VertexShader
        #version 330 core
        
        layout(location = 0) in vec4 position;
        layout(location = 1) in vec4 color;
        layout(location = 2) in vec2 texCoord;

        uniform mat4 transform;

        out vec2 fragTexture;
        out vec4 fragColor;

        void main() {
            gl_Position = position * transform; // No need for projection inside of a PP effect.
            fragColor = color;
            fragTexture = texCoord;
        }
    #pragma ts end
    
    #pragma ts shader Gaussian
        #version 330 core
        #define Pi 3.14159265
        #define KernelSize 15
        
        in vec4 fragColor;
        in vec2 fragTexture;
        
        uniform sampler2D tex0;
        uniform float weights[KernelSize];
        uniform vec2 offsets[KernelSize];        
        uniform float blurAmount;
        
        out vec4 color;
        
        void main() {
            vec4 c = vec4(0,0,0,0);
            for (int i = 0; i < KernelSize; i++) {
                c += texture(tex0, fragTexture - offsets[i]) * weights[i];
            }
            color = c;
        }
    #pragma ts end

    #pragma ts compile vert VertexShader
    #pragma ts compile frag Gaussian
#pragma ts end

// Actual bloom pass that takes the gaussian-blurred, brightness-thresholded scene and
// makes it pop.
#pragma ts program Bloom
    #pragma ts shader VertexShader
        #version 330 core
        
        layout(location = 0) in vec4 position;
        layout(location = 1) in vec4 color;
        layout(location = 2) in vec2 texCoord;

        uniform mat4 transform;

        out vec2 fragTexture;
        out vec4 fragColor;

        void main() {
            gl_Position = position * transform; // No need for projection inside of a PP effect.
            fragColor = color;
            fragTexture = texCoord;
        }
    #pragma ts end
    
    #pragma ts shader BloomFragment
        #version 330 core
        
        in vec4 fragColor;
        in vec2 fragTexture;
        
        uniform sampler2D tex0; // non-blurred scene
        uniform sampler2D tex1; // blurred scene
        uniform float bloomIntensity;
        uniform float bloomSaturation;
        uniform float baseIntensity;
        uniform float baseSaturation;
        
        out vec4 color;
        
        vec4 adjustSaturation(vec4 color, float saturation) {
            float grayDot = dot(color, vec4(0.3, 0.59, 0.11, 0));
            return mix(vec4(grayDot), color, vec4(saturation));
        }
        
        // vec4 adjustSaturation(vec4 color, float saturation) {
        //     float grayDot = dot(color.rgb, vec3(0.3, 0.59, 0.11));
        //     vec4 gray = vec4(grayDot);
        //     return mix(gray, color, vec4(saturation, saturation, saturation, 1));
        // }
        
        void main() {
            vec4 base = texture(tex0, fragTexture);
            vec4 bloom = texture(tex1, fragTexture);
        
            bloom = adjustSaturation(bloom, bloomSaturation) * bloomIntensity;
            base = adjustSaturation(base, baseSaturation) * baseIntensity;
        
            // base *= vec4(1) - clamp(bloom, vec4(0), vec4(1));
        
            base.r *= 1 - clamp(bloom.r, 0, 1);
            base.g *= 1 - clamp(bloom.g, 0, 1);
            base.b *= 1 - clamp(bloom.b, 0, 1);
            base.a *= 1 - clamp(bloom.a, 0, 1);
        
            color = base + bloom;
        }
    #pragma ts end
    
    #pragma ts compile vert VertexShader
    #pragma ts compile frag BloomFragment
#pragma ts end