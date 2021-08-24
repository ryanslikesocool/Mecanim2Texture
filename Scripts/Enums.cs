#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Mec2Tex
{
    internal enum ColorMode
    {
        LDR,
        HDR
    }

    internal enum BakeMode
    {
        Single,
        //Selection,
        [InspectorName("All (Individual)")] AllIndividual,
    }

    internal enum UVLayer
    {
        UV0,
        UV1,
        UV2,
        UV3,
        UV4,
        UV5,
        UV6,
        UV7,
    }

    internal enum Error
    {
        MissingRigObject = 0,
        MissingSkinnedMeshRenderer,
        MissingAnimator,
        MissingRuntimeAnimatorController,
        MissingUVMesh,
        MissingMesh,
        MissingTexture,

        NoAnimationClips = 100,
        NoAnimationsSelected,
        UVAlreadyExists,
        PixelOutOfRange,
        CurveOutOfRange
    }

    /*[Flags]
    internal enum FlagEnum
    {
        None = 0,
        _1 = 1,
        _2 = 2,
        _4 = 4,
        _8 = 8,
        _16 = 16,
        _32 = 32,
        _64 = 64,
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192,
        _16384 = 16384,
        _32768 = 32768,
        _65536 = 65536,
        _131072 = 131072,
        _262144 = 262144,
        _524288 = 524288,
        _1048576 = 1048576,
        _2097152 = 2097152,
        _4194304 = 4194304,
        _8388608 = 8388608,
        _16777216 = 16777216,
        _33554432 = 33554432,
        _67108864 = 67108864,
        _134217728 = 134217728,
        _268435456 = 268435456,
        _536870912 = 536870912,
        _1073741824 = 1073741824
    }*/
}
#endif