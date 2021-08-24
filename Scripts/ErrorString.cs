#if UNITY_EDITOR
using UnityEngine;

namespace Mec2Tex
{
    internal static class ErrorString
    {
        private const string ErrorPrefix = "ERROR: ";
        private const string WarningPrefix = "Warning: ";

        private const string MissingRigObject = ErrorPrefix + "An animation rig object is not assinged for texture creation.  Please assign one.";
        private const string MissingSkinnedMeshRenderer = ErrorPrefix + "Could not find a Skinned Mesh Renderer in the object's hierarchy.";
        private const string MissingAnimator = ErrorPrefix + "Could not find an Animator in the object's hierarchy.";
        private const string MissingRuntimeAnimatorController = ErrorPrefix + "Could not find a Runtime Animator Controller in the Animator's properties.";
        private const string MissingUVMesh = ErrorPrefix + "A mesh is not assigned for UV application.  Please assign one.";
        private const string MissingMesh = ErrorPrefix + "A mesh is not assigned for baking.  Please assign one.";
        private const string MissingTexture = ErrorPrefix + "A texture is not assigned for transforming.  Please assign one.";

        private const string NoAnimationClips = ErrorPrefix + "There are no animation clips on this animator.  You can't bake nonexistant clips.";
        private const string NoAnimationsSelected = ErrorPrefix + "There are no animation clips selected.  You can't bake nonexistant clips.";
        private const string UVAlreadyExists = WarningPrefix + "This mesh already has assigned UVs on this layer.  Applying will overwrite them.";
        private const string PixelOutOfRange = WarningPrefix + "A pixel's value was out of range (less than 0 or greater than 1).  The texture will save with the clamped pixel.";
        private const string CurveOutOfRange = WarningPrefix + "An animation curve has a length that is out of range (less or greater than 0 or 1).  The texture will save while ignoring the out of range values.";

        public static string ToErrorString(this Error error) => error switch
        {
            Error.MissingRigObject => MissingRigObject,
            Error.MissingSkinnedMeshRenderer => MissingSkinnedMeshRenderer,
            Error.MissingAnimator => MissingAnimator,
            Error.MissingRuntimeAnimatorController => MissingRuntimeAnimatorController,
            Error.MissingUVMesh => MissingUVMesh,
            Error.MissingMesh => MissingMesh,
            Error.MissingTexture => MissingTexture,
            Error.NoAnimationClips => NoAnimationClips,
            Error.NoAnimationsSelected => NoAnimationsSelected,
            Error.UVAlreadyExists => UVAlreadyExists,
            Error.PixelOutOfRange => PixelOutOfRange,
            Error.CurveOutOfRange => CurveOutOfRange,
            _ => ErrorPrefix + "An unknown error has occurred."
        };
    }

}
#endif