#include "HLSLSupport.cginc"

int2 GetTextureSize(Texture2D texIn)
{
    int x;
    int y;
    texIn.GetDimensions(x, y);
    return int2(x, y);
}

int3 GetSamplePosition(Texture2D texIn, uint pixel)
{
    int2 size = GetTextureSize(texIn);
    uint sizeX = (uint)size.x;

    return int3
    (
        pixel % sizeX,
        (int)((float)pixel / size.x),
        0
    );
}

void AnimationTexture_float(int FrameIndex, float2 VertexIDUV, int TotalFrameCount, Texture2D TexIn, float Scaler, int VertexCount, out float3 PosOut)
{
    uint frameIndex = (uint)FrameIndex;
    uint totalFrameCount = (uint)TotalFrameCount;
    frameIndex = frameIndex % totalFrameCount;
    int stride = frameIndex * VertexCount;
    uint pixel = (int)VertexIDUV.x + stride;

    int3 samplePosition = GetSamplePosition(TexIn, pixel);
    float3 positions = (float3)TexIn.Load(samplePosition);

    positions -= float3(0.5, 0.5, 0.5);
    positions /= Scaler;
    PosOut = positions;
}

void AnimationTexture_half(int FrameIndex, half2 VertexIDUV, int TotalFrameCount, Texture2D TexIn, half Scaler, int VertexCount, out half3 PosOut)
{
    uint frameIndex = (uint)FrameIndex;
    uint totalFrameCount = (uint)TotalFrameCount;
    frameIndex = frameIndex % totalFrameCount;
    int stride = frameIndex * VertexCount;
    uint pixel = (int)VertexIDUV.x + stride;

    int3 samplePosition = GetSamplePosition(TexIn, pixel);
    half3 positions = (half3)TexIn.Load(samplePosition);

    positions -= half3(0.5, 0.5, 0.5);
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

void RemapUV_float(float time, float4 vertexIdUv, int vertexCount, int framesPerSecond, int textureSize, int frames, out float4 uvOut) {
    int frameCount = floor(time * framesPerSecond);

    int thisFrameCount = fmod(frameCount, frames);
    int pixelOffset = thisFrameCount * vertexCount;
    float4 uv = float4(0.5, 0.5, 0, 0);
    uv.x += floor((vertexIdUv.x + pixelOffset) / textureSize);
    uv.y += fmod(vertexIdUv.x + pixelOffset, textureSize);
    uv.x /= textureSize;
    uv.y /= textureSize;

    uvOut = uv;
}