// Shader constants
sampler2D Texture;
float4 FogVector;
float3 FogColor;
float4x4 WorldViewProj;
float AnimationFrame;
float CurrentSkyLightLevel;

// Vertex shader input
struct VSInput
{
    float4 Position : POSITION;
    float4 Light    : COLOR0;
    float2 TexCoord : TEXCOORD;
};

// Vertex shader output
struct VSOutput
{
    float4 PositionPS : SV_Position;
    float4 Diffuse    : COLOR0;
    float FogFactor   : COLOR1;
    float2 TexCoord   : TEXCOORD0;
};

// Vertex shader
VSOutput VSCustomEffect(VSInput vin)
{
    VSOutput vout;
    
    vout.PositionPS = mul(vin.Position, WorldViewProj);
    vout.FogFactor = saturate(dot(vin.Position, FogVector));
    vout.TexCoord = float2(vin.TexCoord.x, vin.TexCoord.y + AnimationFrame);
    vout.Diffuse = float4(max(vin.Light.xyz,vin.Light.w - CurrentSkyLightLevel), 1);

    return vout;
}

// Pixel shader
float4 PSCustomEffect(VSOutput pin) : SV_Target0
{
    float4 color = tex2D(Texture, pin.TexCoord) * pin.Diffuse;
    if(color.a <= 0) discard;
    color.rgb = lerp(color.rgb, FogColor * color.a, pin.FogFactor);

    return color;
}

technique CustomEffect { pass { VertexShader = compile vs_4_0_level_9_3 VSCustomEffect(); PixelShader = compile ps_4_0_level_9_3 PSCustomEffect(); } }
