cbuffer SkyboxView : register(b0)
{
    float4x4 InverseViewProjection;
};

Texture2D FacePosX : register(t0);
Texture2D FaceNegX : register(t1);
Texture2D FacePosY : register(t2);
Texture2D FaceNegY : register(t3);
Texture2D FacePosZ : register(t4);
Texture2D FaceNegZ : register(t5);
SamplerState SkyboxSampler : register(s0);

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    float2 ndc = input.TexCoord * 2.0 - 1.0;
    ndc.y = -ndc.y;

    float4 farPoint = mul(float4(ndc, 1.0, 1.0), InverseViewProjection);
    float3 dir = normalize(farPoint.xyz / farPoint.w);

    float3 absDir = abs(dir);
    float2 uv;
    float4 color;

    if (absDir.x >= absDir.y && absDir.x >= absDir.z)
    {
        if (dir.x > 0)
        {
            uv = float2(-dir.z / absDir.x, -dir.y / absDir.x) * 0.5 + 0.5;
            color = FacePosX.Sample(SkyboxSampler, uv);
        }
        else
        {
            uv = float2(dir.z / absDir.x, -dir.y / absDir.x) * 0.5 + 0.5;
            color = FaceNegX.Sample(SkyboxSampler, uv);
        }
    }
    else if (absDir.y >= absDir.x && absDir.y >= absDir.z)
    {
        if (dir.y > 0)
        {
            uv = float2(dir.x / absDir.y, dir.z / absDir.y) * 0.5 + 0.5;
            color = FacePosY.Sample(SkyboxSampler, uv);
        }
        else
        {
            uv = float2(dir.x / absDir.y, -dir.z / absDir.y) * 0.5 + 0.5;
            color = FaceNegY.Sample(SkyboxSampler, uv);
        }
    }
    else
    {
        if (dir.z > 0)
        {
            uv = float2(dir.x / absDir.z, -dir.y / absDir.z) * 0.5 + 0.5;
            color = FacePosZ.Sample(SkyboxSampler, uv);
        }
        else
        {
            uv = float2(-dir.x / absDir.z, -dir.y / absDir.z) * 0.5 + 0.5;
            color = FaceNegZ.Sample(SkyboxSampler, uv);
        }
    }

    return float4(color.rgb, 1.0);
}
