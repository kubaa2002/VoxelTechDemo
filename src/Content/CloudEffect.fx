// Shader constants
float4x4 WorldViewProj;
float4 FogVector;
float3 FogColor;

// Vertex shader input
struct VSInput
{
    float3 Pos         : POSITION0;
    float3 IPos        : POSITION1;
    float IColor       : COLOR1;
};

// Vertex shader output
struct VSOutput
{
    float4 Position   : SV_Position;
    float4 Color      : COLOR0;
};

// Vertex shader
VSOutput VSCloudEffect(VSInput vin)
{
    VSOutput vout;

    float4 worldPos = float4(vin.IPos + vin.Pos,1.0);
    vout.Position = mul(worldPos, WorldViewProj);
    float FogFactor = saturate(dot(worldPos, FogVector));
    vout.Color = float4(lerp(vin.IColor, FogColor * vin.IColor, FogFactor),vin.IColor);

    return vout;
}

// Pixel shader
float4 PSCloudEffect(VSOutput pin) : SV_Target0
{
    return pin.Color;
}

technique CustomEffect { pass { VertexShader = compile vs_3_0 VSCloudEffect(); PixelShader = compile ps_3_0 PSCloudEffect(); } }
