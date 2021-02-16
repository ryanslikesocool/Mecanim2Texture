int3 GetSamplePosition_float(Texture2D texIn, uint pixel)
{
    uint x, y;
    texIn.GetDimensions(x, y);

    return int3
    (
        pixel % x,
        (int)((float)pixel / x),
        0
    );
}

int3 GetSamplePosition_half(Texture2D texIn, uint pixel)
{
    uint x, y;
    texIn.GetDimensions(x, y);

    return int3
    (
        pixel % x,
        (int)((half)pixel / x),
        0
    );
}

void AnimationTexture_float(float FrameIndex, float2 VertexIDUV, uint TotalFrameCount, Texture2D TexIn, int VertexCount, out float3 PosOut)
{
    uint frameIndex = (uint)FrameIndex;
    frameIndex = frameIndex % TotalFrameCount;
    int stride = frameIndex * VertexCount;
    int pixel = (int)VertexIDUV.x + stride;

    int3 samplePosition = GetSamplePosition_float(TexIn, pixel);
    float3 positions = (float3)TexIn.Load(samplePosition);

#if UNITY_COLORSPACE_GAMMA
    positions = pow(positions, float3(2.2, 2.2, 2.2));
#endif

    positions -= float3(0.5, 0.5, 0.5);
    PosOut = positions;
}

void AnimationTexture_half(half FrameIndex, half2 VertexIDUV, uint TotalFrameCount, Texture2D TexIn, int VertexCount, out half3 PosOut)
{
    uint frameIndex = (uint)FrameIndex;
    frameIndex = frameIndex % TotalFrameCount;
    int stride = frameIndex * VertexCount;
    int pixel = (int)VertexIDUV.x + stride;

    int3 samplePosition = GetSamplePosition_half(TexIn, pixel);
    half3 positions = (half3)TexIn.Load(samplePosition);

#if UNITY_COLORSPACE_GAMMA
    positions = pow(positions, half3(2.2, 2.2, 2.2));
#endif

    positions -= half3(0.5, 0.5, 0.5);
    PosOut = positions;
}

void AnimationTextureNPOT_float(uint FrameIndex, float2 VertexIDUV, Texture2D TexIn, out float3 PosOut)
{   
    uint x, y;
    TexIn.GetDimensions(x, y);

    int3 samplePosition = int3((int)floor(VertexIDUV.x), FrameIndex % y, 0);
    float3 positions = (float3)TexIn.Load(samplePosition);

#if UNITY_COLORSPACE_GAMMA
    positions = pow(positions, float3(2.2, 2.2, 2.2));
#endif

    positions -= float3(0.5, 0.5, 0.5);
    PosOut = positions;
}

void AnimationTextureNPOT_half(uint FrameIndex, half2 VertexIDUV, Texture2D TexIn, out half3 PosOut)
{
    uint x, y;
    TexIn.GetDimensions(x, y);

    int3 samplePosition = int3((int)floor(VertexIDUV.x), FrameIndex % y, 0);
    half3 positions = (half3)TexIn.Load(samplePosition);

#if UNITY_COLORSPACE_GAMMA
    positions = pow(positions, half3(2.2, 2.2, 2.2));
#endif

    positions -= half3(0.5, 0.5, 0.5);
    PosOut = positions;
}
