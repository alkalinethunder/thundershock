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

// CRT Shadow-mask Effect
//
// This is a port of a similar HLSL shader written by Timothy
// Lottes for use in MonoGame. This version is stripped down for
// Thundershock but in future I may port the entire thing over.
#pragma ts program CRT
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


    #pragma ts shader ShadowMask
        #version 330 core
        
        in vec4 fragColor;
        in vec2 fragTexture;
        
        uniform sampler2D tex0;
        uniform float maskDark;
        uniform float maskLight;
        uniform float brightnessBoost;
        uniform float hardScan;
        uniform float hardPix;
        uniform vec2 texSize;
        uniform vec2 outputSize;
        
        out vec4 color;
        
        vec3 toSrgb(vec3 color) {
            return sqrt(color);
        }
        
        // Nearest emulated sample given floating point position and texel offset.
        // Also zero's off screen.
        vec3 fetch(vec2 texCoord, vec2 offset, vec2 size) {
            texCoord = (floor(texCoord * size.xy + offset) + vec2(0.5, 0.5)) / size.xy;
            return brightnessBoost * pow(texture(tex0, texCoord).rgb, vec3(2));
        }
        
        // Distance in emulated pixels to nearest texel.
        vec2 dist(vec2 pos, vec2 size) {
            pos = pos * size;
            return -(fract(pos) - vec2(0.5, 0.5));
        }
        
        // 1D gaussian
        float gaus(float pos, float scale) {
            return exp2(scale * pos * pos);
        }
        
        // Return scanline weight
        float scan(vec2 pos, float off, vec2 size) {
            float dst = dist(pos, size).y;
            return gaus(dst + off, hardScan);
        }
        
        vec3 horz3(vec2 pos, float off, vec2 texture_size)
        {
            vec3 b = fetch(pos, vec2(-1.0,off),texture_size);
            vec3 c = fetch(pos, vec2( 0.0,off),texture_size);
            vec3 d = fetch(pos, vec2( 1.0,off),texture_size);
            float dst = dist(pos, texture_size).x;
  
            // Convert distance to weight.
            float scale = hardPix;
            float wb = gaus(dst-1.0,scale);
            float wc = gaus(dst+0.0,scale);
            float wd = gaus(dst+1.0,scale);
    
            // Return filtered sample.
            return (b * wb + c * wc + d * wd) / (wb + wc +wd);
        }
  
        // 5-tap Gaussian filter along horz line.
        vec3 horz5(vec2 pos, float off, vec2 texture_size)
        {
            vec3 a = fetch(pos,vec2(-2.0,off),texture_size);
            vec3 b = fetch(pos,vec2(-1.0,off),texture_size);
            vec3 c = fetch(pos,vec2( 0.0,off),texture_size);
            vec3 d = fetch(pos, vec2( 1.0,off),texture_size);
            vec3 e = fetch(pos,vec2( 2.0,off),texture_size);
            float dst = dist(pos, texture_size).x;
    
            // Convert distance to weight.
            float scale = hardPix;
            float wa = gaus(dst-2.0,scale);
            float wb = gaus(dst-1.0,scale);
            float wc = gaus(dst+0.0,scale);
            float wd = gaus(dst+1.0,scale);
            float we = gaus(dst+2.0,scale);
    
            // Return filtered sample.
            return (a * wa + b * wb + c * wc + d * wd + e * we) / (wa + wb + wc + wd + we);
        }


        // Allow nearest 3 lines to affect pixel
        vec3 tri(vec2 texCoord, vec2 texSize)
        {
            vec3 a = horz3(texCoord, -1.0, texSize);
            vec3 b = horz5(texCoord, 0.0, texSize);
            vec3 c = horz3(texCoord, 1.0, texSize);
            float wa = scan(texCoord, -1.0, texSize);
            float wb = scan(texCoord, 0.0, texSize);
            float wc = scan(texCoord, 1.0, texSize);
            return a * wa + b * wb + c * wc;
        }

        // Shadow mask 
        vec3 mask(vec2 pos)
        {
            vec3 m = vec3(maskDark);
  
            // VGA style shadow mask.
            pos.xy = floor(pos.xy * vec2(1.0, 0.5));
            pos.x += pos.y * 3.0;
            pos.x = fract(pos.x / 6.0);

            if (pos.x < 0.333) 
            {
                m.r = maskLight;
            }
            else if (pos.x < 0.666) 
            {
                m.g = maskLight;
            }
            else 
            {
                m.b = maskLight;
            }

            return m;
        }
        
        void main() {
        	vec3 c = tri(fragTexture, texSize);
	        c.rgb *= mask(floor(fragTexture.xy * outputSize.xy) + 0.5);
	        color = vec4(toSrgb(c), 1.0);
        }


    #pragma ts end
    
    #pragma ts compile vert VertexShader
    #pragma ts compile frag ShadowMask 
#pragma ts end