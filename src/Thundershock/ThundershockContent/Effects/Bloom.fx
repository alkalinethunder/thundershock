#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
Texture2D BloomTexture;
float BloomIntensity;
float BaseIntensity;
float BloomSaturation;
float BaseSaturation;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

sampler2D BloomTextureSampler = sampler_state
{
    Texture = <BloomTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 AdjustSaturation(float4 color, float saturation)
{
    float4 gray = dot(color, float3(0.3, 0.59, 0.11));
    return lerp(gray, color, saturation);
}


float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 base = tex2D(SpriteTextureSampler, input.TextureCoordinates);
	float4 bloom = tex2D(BloomTextureSampler, input.TextureCoordinates);

    bloom = AdjustSaturation(bloom, BloomSaturation) * BloomIntensity;
    base = AdjustSaturation(base, BaseSaturation) * BaseIntensity;
	
	base *= (1 - saturate(bloom));
	
	return base + bloom;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};