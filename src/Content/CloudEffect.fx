// Shader constants
float4x4 WorldViewProj;
float Scale;
float4 FogVector;
float3 FogColor;

// Vertex shader input
struct VSInput
{
    float3 Pos         : POSITION0;
    float3 IPos        : POSITION1;
    float4 IColor      : COLOR1;
};

// Vertex shader output
struct VSOutput
{
    float4 Position   : SV_Position;
    float4 Color      : COLOR0;
    float FogFactor   : COLOR1;
};

// Vertex shader
VSOutput VSCloudEffect(VSInput vin)
{
    VSOutput vout;

    float3 worldPos = vin.IPos + vin.Pos;
    worldPos.xz *= Scale;
    vout.Position = mul(float4(worldPos, 1.0), WorldViewProj);
    vout.FogFactor = saturate(dot(float4(vin.Pos, 1.0), FogVector));
    vout.Color = vin.IColor;

    return vout;
}

// Pixel shader
float4 PSCloudEffect(VSOutput pin) : SV_Target0
{
    float4 color = float4(lerp(pin.Color.rgb, FogColor * pin.Color.a, pin.FogFactor),pin.Color.a);
    return color;
}

technique CustomEffect { pass { VertexShader = compile vs_3_0 VSCloudEffect(); PixelShader = compile ps_3_0 PSCloudEffect(); } }
