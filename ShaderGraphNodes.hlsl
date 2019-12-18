#include "HLSLSupport.cginc"

void AnimateVertices_float(float4 VertexIDUV, float FramesPerSecond, float AnimationFrames, Texture2D TexIn, float2 TexSize, float Scaler, float VertexCount, SamplerState TexSampler, out float3 PosOut)
{
    int frameCount = floor(_Time.y * FramesPerSecond);
    frameCount = floor(fmod(frameCount, AnimationFrames));
    int pixelOffset = floor(frameCount * VertexCount);

    float2 uvPos = float2(0.5, 0.5);
    uvPos.x += floor((VertexIDUV.x + pixelOffset) / TexSize.x);
    uvPos.y += fmod(VertexIDUV.x + pixelOffset, TexSize.y);
    uvPos /= TexSize;

    float4 positions = TexIn.SampleLevel(TexSampler, uvPos, 0);
    positions -= float4(0.5, 0.5, 0.5, 0);
    positions /= Scaler;
    PosOut = positions;
}