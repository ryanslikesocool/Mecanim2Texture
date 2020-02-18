#include "HLSLSupport.cginc"

void AnimateVertices_float(float TimeOffset, float4 VertexIDUV, float ColorMode, float FramesPerSecond, float AnimationFrames, Texture2D TexIn, float2 TexSize, float Scaler, float VertexCount, SamplerState TexSampler, out float3 PosOut)
{
    float time = _Time.y + TimeOffset;
    int frameCount = floor(time * FramesPerSecond);
    frameCount = fmod(frameCount, AnimationFrames);
    int pixelOffset = frameCount * VertexCount;

    float2 uvPos = float2(0.5, 0.5);
    uvPos.x += floor((VertexIDUV.x + pixelOffset) / TexSize.x);
    uvPos.y += fmod(VertexIDUV.x + pixelOffset, TexSize.y);
    uvPos /= TexSize;

    float3 positions = (float3)TexIn.SampleLevel(TexSampler, uvPos, 0);

    //ColorMode 0 is LDR in Linear colorspace
    //ColorMode 1 is HDR in Linear colorspace
    if (ColorMode == 0) {
        positions = pow(positions, 0.454545);
    }

    positions -= float3(0.5, 0.5, 0.5);
    positions /= Scaler;
    PosOut = positions;
}