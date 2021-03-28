// CRT SHADOWMASK SHADER
//
// This shader is originally written by Timmothy Lottes, however I have adapted it to be a bit more
// compact for use in Red Team. The original shader adds a little bit of bloom, however Red Team's post-processor
// takes care of bloom before this shader gets a turn. The user can also disable bloom that way, if it's not an effect
// they particularly like. Likewise, this shader can also be disabled.

// Boiler-plate
#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Parameters
float MaskDark;
float MaskLight;
float BrightnessBoost;       // Brightness boost.
float HardScan;             // Hard scan
float HardPix;              // Not sure.
float2 TextureSize;         // Size of the screen.
float2 OutputSize;          
Texture2D SpriteTexture;    // The screen.

// Texture samplers.
sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

// Vertex shader output - this shouldn't be fucked with.
struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

// Helper methods

float3 ToSrgb(float3 color)
{
    return sqrt(color);
}

// Nearest emulated sample given floating point position and texel offset.
// Also zero's off screen.
float3 Fetch(float2 texCoord, float2 offset, float2 texSize)
{
    texCoord = (floor(texCoord * texSize.xy + offset) + float2(0.5, 0.5)) / texSize.xy;

    return BrightnessBoost * pow(tex2D(SpriteTextureSampler, texCoord).rgb, 2);
}

// Distance in emulated pixels to nearest texel.
float2 Dist(float2 pos, float2 texture_size)
{
    pos=pos*texture_size.xy;
    return -(frac(pos)-float2(0.5, 0.5));
}
    
// 1D Gaussian.
float Gaus(float pos, float scale)
{
    return exp2(scale*pos * pos);
}

// Return scanline weight.
float Scan(float2 pos,float off, float2 texture_size)
{
    float dst=Dist(pos, texture_size).y;
    return Gaus(dst + off, HardScan);
}

float3 Horz3(float2 pos, float off, float2 texture_size)
{
    float3 b = Fetch(pos,float2(-1.0,off),texture_size);
    float3 c = Fetch(pos,float2( 0.0,off),texture_size);
    float3 d = Fetch(pos,float2( 1.0,off),texture_size);
    float dst = Dist(pos, texture_size).x;
  
    // Convert distance to weight.
    float scale = HardPix;
    float wb = Gaus(dst-1.0,scale);
    float wc = Gaus(dst+0.0,scale);
    float wd = Gaus(dst+1.0,scale);
    
    // Return filtered sample.
    return (b * wb + c * wc + d * wd) / (wb + wc +wd);
}
  
// 5-tap Gaussian filter along horz line.
float3 Horz5(float2 pos, float off, float2 texture_size)
{
    float3 a = Fetch(pos,float2(-2.0,off),texture_size);
    float3 b = Fetch(pos,float2(-1.0,off),texture_size);
    float3 c = Fetch(pos,float2( 0.0,off),texture_size);
    float3 d = Fetch(pos,float2( 1.0,off),texture_size);
    float3 e = Fetch(pos,float2( 2.0,off),texture_size);
    float dst = Dist(pos, texture_size).x;
    
    // Convert distance to weight.
    float scale = HardPix;
    float wa = Gaus(dst-2.0,scale);
    float wb = Gaus(dst-1.0,scale);
    float wc = Gaus(dst+0.0,scale);
    float wd = Gaus(dst+1.0,scale);
    float we = Gaus(dst+2.0,scale);
    
    // Return filtered sample.
    return (a * wa + b * wb + c * wc + d * wd + e * we) / (wa + wb + wc + wd + we);
}


// Allow nearest 3 lines to affect pixel
float3 Tri(float2 texCoord, float2 texSize)
{
    float3 a = Horz3(texCoord, -1.0, texSize);
    float3 b = Horz5(texCoord, 0.0, texSize);
    float3 c = Horz3(texCoord, 1.0, texSize);
    float wa = Scan(texCoord, -1.0, texSize);
    float wb = Scan(texCoord, 0.0, texSize);
    float wc = Scan(texCoord, 1.0, texSize);
    return a * wa + b * wb + c * wc;
}

// Shadow mask 
float3 Mask(float2 pos)
{
    float3 mask = MaskDark;
  
    // VGA style shadow mask.
    pos.xy = floor(pos.xy * float2(1.0, 0.5));
    pos.x += pos.y * 3.0;
    pos.x = frac(pos.x / 6.0);

    if (pos.x < 0.333) 
    {
        mask.r = MaskLight;
    }
    else if (pos.x < 0.666) 
    {
        mask.g = MaskLight;
    }
    else 
    {
        mask.b = MaskLight;
    }

    return mask;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float3 color = Tri(input.TextureCoordinates, TextureSize);
	color.rgb *= Mask(floor(input.TextureCoordinates.xy * OutputSize.xy) + 0.5);
	return float4(ToSrgb(color), 1.0);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};