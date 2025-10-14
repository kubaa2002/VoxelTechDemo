// Shader constants
sampler2D Texture;
float4 FogVector;
float3 FogColor;
float4x4 WorldViewProj;
float4x4 Rotations[6];
float AnimationFrame;
float CurrentSkyLightLevel;

// Vertex shader input
struct VSInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float3 IPosition : POSITION1;
    float4 ILight    : COLOR1;
    float2 ITexCoord : TEXCOORD1;
    float2 IRotation  : NORMAL1;
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
    
    float4 Pos = float4(mul(vin.Position, Rotations[int(vin.IRotation.x)]).xyz + vin.IPosition, 1.0);
    vout.PositionPS = mul(Pos, WorldViewProj);
    vout.FogFactor = saturate(dot(Pos, FogVector));
    float2 TexCoord;
    if(vin.IRotation.y == 0.0) TexCoord = vin.TexCoord;
    else if(vin.IRotation.y == 1.0) TexCoord = float2(vin.TexCoord.y, 1.0/16.0 - vin.TexCoord.x);
    else if(vin.IRotation.y == 2.0) TexCoord = float2(1.0/16.0 - vin.TexCoord.x, 1.0/16.0 - vin.TexCoord.y);
    else if(vin.IRotation.y == 3.0) TexCoord = float2(1.0/16.0 - vin.TexCoord.y, vin.TexCoord.x);
    TexCoord = TexCoord + vin.ITexCoord;
    vout.TexCoord = float2(TexCoord.x, TexCoord.y + AnimationFrame);
    vout.Diffuse = float4(max(vin.ILight.xyz,vin.ILight.w - CurrentSkyLightLevel), 1);

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

technique CustomEffect { pass { VertexShader = compile vs_3_0 VSCustomEffect(); PixelShader = compile ps_3_0 PSCustomEffect(); } }
