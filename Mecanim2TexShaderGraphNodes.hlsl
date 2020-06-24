#include "HLSLSupport.cginc"

int2 GetTextureSize(Texture2D texIn)
{
    int x;
    int y;
    texIn.GetDimensions(x, y);
    return int2(x, y);
}

int3 GetSamplePosition_float(Texture2D texIn, uint pixel)
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

int3 GetSamplePosition_half(Texture2D texIn, uint pixel)
{
    int2 size = GetTextureSize(texIn);
    uint sizeX = (uint)size.x;

    return int3
    (
        pixel % sizeX,
        (uint)((half)pixel / size.x),
        0
    );
}

void AnimationTexture_float(int FrameIndex, float2 VertexIDUV, int TotalFrameCount, Texture2D TexIn, int VertexCount, out float3 PosOut)
{
    uint frameIndex = (uint)FrameIndex;
    uint totalFrameCount = (uint)TotalFrameCount;
    frameIndex = frameIndex % totalFrameCount;
    int stride = frameIndex * VertexCount;
    uint pixel = (int)VertexIDUV.x + stride;

    int3 samplePosition = GetSamplePosition_float(TexIn, pixel);
    float3 positions = (float3)TexIn.Load(samplePosition);

    positions -= float3(0.5, 0.5, 0.5);
    PosOut = positions;
}

void AnimationTexture_half(int FrameIndex, half2 VertexIDUV, int TotalFrameCount, Texture2D TexIn, int VertexCount, out half3 PosOut)
{
    uint frameIndex = (uint)FrameIndex;
    uint totalFrameCount = (uint)TotalFrameCount;
    frameIndex = frameIndex % totalFrameCount;
    int stride = frameIndex * VertexCount;
    uint pixel = (int)VertexIDUV.x + stride;

    int3 samplePosition = GetSamplePosition_half(TexIn, pixel);
    half3 positions = (half3)TexIn.Load(samplePosition);

    positions -= half3(0.5, 0.5, 0.5);
    PosOut = positions;
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

void RemapUV_half(half time, half4 vertexIdUv, int vertexCount, int framesPerSecond, int textureSize, int frames, out half4 uvOut) {
    int frameCount = floor(time * framesPerSecond);

    int thisFrameCount = fmod(frameCount, frames);
    int pixelOffset = thisFrameCount * vertexCount;
    half4 uv = half4(0.5, 0.5, 0, 0);
    uv.x += floor((vertexIdUv.x + pixelOffset) / textureSize);
    uv.y += fmod(vertexIdUv.x + pixelOffset, textureSize);
    uv.x /= textureSize;
    uv.y /= textureSize;

    uvOut = uv;
}