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

void AnimateVertices_half(float TimeOffset, float4 VertexIDUV, float ColorMode, float FramesPerSecond, float AnimationFrames, Texture2D TexIn, float2 TexSize, float Scaler, float VertexCount, SamplerState TexSampler, out float3 PosOut)
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

void AnimationTexture_float(Texture2DArray textures, float time, float4 vertexIdUv, int vertexCount, int framesPerSecond, float scaler, int textureSize, float lerpTime, int indexFrom, int indexTo, int framesFrom, int framesTo, SamplerState samplerState, out float3 positionOut)
{
    float2 lod = float2(0, 0);
    int frameCount = floor(time * framesPerSecond);

    int frameCountFrom = fmod(frameCount, framesFrom);
    int pixelOffsetFrom = frameCountFrom * vertexCount;
    float3 uvFrom = float3(0.5, 0.5, 0);
    uvFrom.x += floor((vertexIdUv.x + pixelOffsetFrom) / textureSize);
    uvFrom.y += fmod(vertexIdUv.x + pixelOffsetFrom, textureSize);
    uvFrom.x /= textureSize;
    uvFrom.y /= textureSize;
    uvFrom.z = indexFrom;
    float3 positionsFrom = (float3)textures.SampleLevel(samplerState, uvFrom, lod);

    int frameCountTo = fmod(frameCount, framesTo);
    int pixelOffsetTo = frameCountTo * vertexCount;
    float3 uvTo = float3(0.5, 0.5, 0);
    uvTo.x += floor((vertexIdUv.x + pixelOffsetTo) / textureSize);
    uvTo.y += fmod(vertexIdUv.x + pixelOffsetTo, textureSize);
    uvTo.x /= textureSize;
    uvTo.y /= textureSize;
    uvTo.z = indexTo;
    float3 positionsTo = (float3)textures.SampleLevel(samplerState, uvTo, lod);

    float3 positions = lerp(positionsFrom, positionsTo, lerpTime);
    positions -= float3(0.5, 0.5, 0.5);
    positions /= scaler;
    positionOut = positions;
}

void AnimationTexture_half(Texture2DArray textures, float time, float4 vertexIdUv, int vertexCount, int framesPerSecond, float scaler, int textureSize, float lerpTime, int indexFrom, int indexTo, int framesFrom, int framesTo, SamplerState samplerState, out float3 positionOut)
{
    float2 lod = float2(0, 0);
    int frameCount = floor(time * framesPerSecond);

    int frameCountFrom = fmod(frameCount, framesFrom);
    int pixelOffsetFrom = frameCountFrom * vertexCount;
    float3 uvFrom = float3(0.5, 0.5, 0);
    uvFrom.x += floor((vertexIdUv.x + pixelOffsetFrom) / textureSize);
    uvFrom.y += fmod(vertexIdUv.x + pixelOffsetFrom, textureSize);
    uvFrom.x /= textureSize;
    uvFrom.y /= textureSize;
    uvFrom.z = indexFrom;
    float3 positionsFrom = (float3)textures.SampleLevel(samplerState, uvFrom, lod);

    int frameCountTo = fmod(frameCount, framesTo);
    int pixelOffsetTo = frameCountTo * vertexCount;
    float3 uvTo = float3(0.5, 0.5, 0);
    uvTo.x += floor((vertexIdUv.x + pixelOffsetTo) / textureSize);
    uvTo.y += fmod(vertexIdUv.x + pixelOffsetTo, textureSize);
    uvTo.x /= textureSize;
    uvTo.y /= textureSize;
    uvTo.z = indexTo;
    float3 positionsTo = (float3)textures.SampleLevel(samplerState, uvTo, lod);

    float3 positions = lerp(positionsFrom, positionsTo, lerpTime);
    positions -= float3(0.5, 0.5, 0.5);
    positions /= scaler;
    positionOut = positions;
}
