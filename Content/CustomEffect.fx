// Shader constants
sampler2D Texture;
float4 DiffuseColor;
float4 FogVector;
float3 FogColor;
float4x4 WorldViewProj;

// Vertex shader input
struct VSInput
{
    float4 Position : POSITION;
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
    vout.Diffuse = DiffuseColor;
    vout.FogFactor = saturate(dot(vin.Position, FogVector));
    vout.TexCoord = vin.TexCoord;

    return vout;
}

// Pixel shader
float4 PSCustomEffect(VSOutput pin) : SV_Target0
{
    float4 color = tex2D(Texture, pin.TexCoord) * pin.Diffuse;

    clip(color.a - 0.1);

    color.rgb = lerp(color.rgb, FogColor * color.a, pin.FogFactor);

    return color;
}

// Using shader model 3.0 (DX9c)
technique CustomEffect { pass { VertexShader = compile vs_3_0 VSCustomEffect(); PixelShader = compile ps_3_0 PSCustomEffect(); } }
