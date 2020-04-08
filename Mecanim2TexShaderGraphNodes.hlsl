#include "HLSLSupport.cginc"

void AnimationTexture_float(float TimeOffset, float4 VertexIDUV, float ColorMode, float FramesPerSecond, float AnimationFrames, Texture2D TexIn, float2 TexSize, float Scaler, float VertexCount, SamplerState TexSampler, out float3 PosOut)
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

void AnimationTexture_half(float TimeOffset, float4 VertexIDUV, float ColorMode, float FramesPerSecond, float AnimationFrames, Texture2D TexIn, float2 TexSize, float Scaler, float VertexCount, SamplerState TexSampler, out float3 PosOut)
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

void AnimationTexturev2_float(Texture2DArray textures, float3 vertexPositions, float time, float4 vertexIdUv, int vertexCount, int framesPerSecond, float scaler, int textureSize, float4 lerper, int4 index, int4 frames, SamplerState samplerState, out float3 positionOut)
{
    float2 lod = float2(0, 0);
    int frameCount = floor(time * framesPerSecond);

    int frameCountA = fmod(frameCount, frames.x);
    int pixelOffsetA = frameCountA * vertexCount;
    float3 uvA = float3(0.5, 0.5, index.x);
    uvA.x += floor((vertexIdUv.x + pixelOffsetA) / textureSize);
    uvA.y += fmod(vertexIdUv.x + pixelOffsetA, textureSize);
    uvA.x /= textureSize;
    uvA.y /= textureSize;
    float3 positionsA = (float3)textures.SampleLevel(samplerState, uvA, lod);

    int frameCountB = fmod(frameCount, frames.y);
    int pixelOffsetB = frameCountB * vertexCount;
    float3 uvB = float3(0.5, 0.5, index.y);
    uvB.x += floor((vertexIdUv.x + pixelOffsetB) / textureSize);
    uvB.y += fmod(vertexIdUv.x + pixelOffsetB, textureSize);
    uvB.x /= textureSize;
    uvB.y /= textureSize;
    float3 positionsB = (float3)textures.SampleLevel(samplerState, uvB, lod);

    int frameCountC = fmod(frameCount, frames.z);
    int pixelOffsetC = frameCountC * vertexCount;
    float3 uvC = float3(0.5, 0.5, index.z);
    uvC.x += floor((vertexIdUv.x + pixelOffsetC) / textureSize);
    uvC.y += fmod(vertexIdUv.x + pixelOffsetC, textureSize);
    uvC.x /= textureSize;
    uvC.y /= textureSize;
    float3 positionsC = (float3)textures.SampleLevel(samplerState, uvC, lod);

    int frameCountD = fmod(frameCount, frames.w);
    int pixelOffsetD = frameCountD * vertexCount;
    float3 uvD = float3(0.5, 0.5, index.z);
    uvD.x += floor((vertexIdUv.x + pixelOffsetD) / textureSize);
    uvD.y += fmod(vertexIdUv.x + pixelOffsetD, textureSize);
    uvD.x /= textureSize;
    uvD.y /= textureSize;
    float3 positionsD = (float3)textures.SampleLevel(samplerState, uvD, lod);

    float3 positions = lerp(positionsA, positionsB, lerper.x);
    positions = lerp(positions, positionsC, lerper.y);
    positions = lerp(positions, positionsD, lerper.z);
    positions = lerp(positions, vertexPositions, lerper.w);

    positions -= float3(0.5, 0.5, 0.5);
    positions /= scaler;
    positionOut = positions;
}

void AnimationTexturev2_half(Texture2DArray textures, float3 vertexPositions, float time, float4 vertexIdUv, int vertexCount, int framesPerSecond, float scaler, int textureSize, float4 lerper, int4 index, int4 frames, SamplerState samplerState, out float3 positionOut)
{
    float2 lod = float2(0, 0);
    int frameCount = floor(time * framesPerSecond);

    int frameCountA = fmod(frameCount, frames.x);
    int pixelOffsetA = frameCountA * vertexCount;
    float3 uvA = float3(0.5, 0.5, index.x);
    uvA.x += floor((vertexIdUv.x + pixelOffsetA) / textureSize);
    uvA.y += fmod(vertexIdUv.x + pixelOffsetA, textureSize);
    uvA.x /= textureSize;
    uvA.y /= textureSize;
    float3 positionsA = (float3)textures.SampleLevel(samplerState, uvA, lod);

    int frameCountB = fmod(frameCount, frames.y);
    int pixelOffsetB = frameCountB * vertexCount;
    float3 uvB = float3(0.5, 0.5, index.y);
    uvB.x += floor((vertexIdUv.x + pixelOffsetB) / textureSize);
    uvB.y += fmod(vertexIdUv.x + pixelOffsetB, textureSize);
    uvB.x /= textureSize;
    uvB.y /= textureSize;
    float3 positionsB = (float3)textures.SampleLevel(samplerState, uvB, lod);

    int frameCountC = fmod(frameCount, frames.z);
    int pixelOffsetC = frameCountC * vertexCount;
    float3 uvC = float3(0.5, 0.5, index.z);
    uvC.x += floor((vertexIdUv.x + pixelOffsetC) / textureSize);
    uvC.y += fmod(vertexIdUv.x + pixelOffsetC, textureSize);
    uvC.x /= textureSize;
    uvC.y /= textureSize;
    float3 positionsC = (float3)textures.SampleLevel(samplerState, uvC, lod);

    int frameCountD = fmod(frameCount, frames.w);
    int pixelOffsetD = frameCountD * vertexCount;
    float3 uvD = float3(0.5, 0.5, index.w);
    uvD.x += floor((vertexIdUv.x + pixelOffsetD) / textureSize);
    uvD.y += fmod(vertexIdUv.x + pixelOffsetD, textureSize);
    uvD.x /= textureSize;
    uvD.y /= textureSize;
    float3 positionsD = (float3)textures.SampleLevel(samplerState, uvD, lod);

    float3 positions = lerp(positionsA, positionsB, lerper.x);
    positions = lerp(positions, positionsC, lerper.y);
    positions = lerp(positions, positionsD, lerper.z);
    positions = lerp(positions, vertexPositions, lerper.w);

    positions -= float3(0.5, 0.5, 0.5);
    positions /= scaler;
    positionOut = positions;
}