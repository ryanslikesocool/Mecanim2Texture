int3 GetSamplePosition_float(Texture2D texIn, int pixel)
{
    int x;
    int y;
    texIn.GetDimensions(x, y);

    return int3
    (
        pixel % x,
        (int)((float)pixel / x),
        0
    );
}

int3 GetSamplePosition_half(Texture2D texIn, int pixel)
{
    int x;
    int y;
    texIn.GetDimensions(x, y);

    return int3
    (
        pixel % x,
        (int)((half)pixel / x),
        0
    );
}

void AnimationTexture_float(float FrameIndex, float2 VertexIDUV, int TotalFrameCount, Texture2D TexIn, int VertexCount, out float3 PosOut)
{
    int frameIndex = (int)FrameIndex;
    int totalFrameCount = TotalFrameCount;
    frameIndex = frameIndex % totalFrameCount;
    int stride = frameIndex * VertexCount;
    int pixel = (int)VertexIDUV.x + stride;

    int3 samplePosition = GetSamplePosition_float(TexIn, pixel);
    float3 positions = (float3)TexIn.Load(samplePosition);

    positions -= float3(0.5, 0.5, 0.5);
    PosOut = positions;
}

void AnimationTexture_half(half FrameIndex, half2 VertexIDUV, int TotalFrameCount, Texture2D TexIn, int VertexCount, out half3 PosOut)
{
    int frameIndex = (int)FrameIndex;
    int totalFrameCount = TotalFrameCount;
    frameIndex = frameIndex % totalFrameCount;
    int stride = frameIndex * VertexCount;
    int pixel = (int)VertexIDUV.x + stride;

    int3 samplePosition = GetSamplePosition_half(TexIn, pixel);
    half3 positions = (half3)TexIn.Load(samplePosition);

    positions -= half3(0.5, 0.5, 0.5);
    PosOut = positions;
}