#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 TextureSize;
float Intensity;
float Skew;

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    if (Intensity * 0 != Intensity) 
    {
        float2 texelSize = float2(1, 1) / TextureSize;
        float2 offset = texelSize * Intensity;
    
        // attempt to grab the pixel coordinates of the texel
        float2 pixel = TextureSize * input.TextureCoordinates;
        
        // calculate skew based on pixel
        float2 skew = float2(pixel.x - (Skew * pixel.y), pixel.y) * texelSize;
    
        float4 posColor = tex2D(SpriteTextureSampler, skew + offset);
        float4 negColor = tex2D(SpriteTextureSampler, skew - offset);
    
        return lerp(negColor, posColor, 0.5);
    }
    else 
    {
        return tex2D(SpriteTextureSampler, input.TextureCoordinates);
    }
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};