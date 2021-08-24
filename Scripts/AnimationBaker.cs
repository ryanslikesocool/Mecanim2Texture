#if UNITY_EDITOR
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using Unity.EditorCoroutines.Editor;

namespace Mec2Tex
{
    internal class AnimationBaker : EditorView
    {
        private GameObject animationRigContainer;
        private RuntimeAnimatorController animatorController;
        private BakeMode bakeMode = BakeMode.AllIndividual;
        private int framesPerSecondCapture = 24;
        private int clipToBakeIndex = 0;
        private ColorMode animationTextureColorMode = ColorMode.HDR;
        private bool powerOfTwoOptimization = false;
        private int sizeOptimizationIteration = 4;

        #region View
        private float totalTime = 0;
        private int totalFrames = 0;
        private int squareFrames = 0;

        private AnimationClip[] clipsToBake = null;
        private MeshRenderer[] renderers = null;
        private MeshFilter[] filters = null;
        #endregion

        public override void View()
        {
            animationRigContainer = (GameObject)EditorGUILayout.ObjectField("Rig Container", animationRigContainer, typeof(GameObject), true);
            animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator", animatorController, typeof(RuntimeAnimatorController), false);

            if (animationRigContainer == null || animatorController == null) { return; }

            bool checkNextError = !ErrorUtility.SetError(Error.MissingRigObject, animationRigContainer == null);
            if (checkNextError) { checkNextError = !ErrorUtility.SetError(Error.MissingSkinnedMeshRenderer, animationRigContainer.GetComponentInChildren<SkinnedMeshRenderer>() == null); }
            if (checkNextError) { checkNextError = !ErrorUtility.SetError(Error.MissingAnimator, animationRigContainer.GetComponentInChildren<Animator>() == null); }
            if (checkNextError) { checkNextError = !ErrorUtility.SetError(Error.NoAnimationClips, animatorController.animationClips.Length == 0); }

            switch (bakeMode)
            {
                case BakeMode.Single:
                    int idx = EditorGUILayout.Popup(new GUIContent("Clip to Bake", "Which animation clip will be baked."), clipToBakeIndex, GetAnimationClipNames(animatorController.animationClips));
                    clipsToBake = new AnimationClip[] { animatorController.animationClips[idx] };
                    break;
                    //case BakeMode.Selection:
                    //    EditorGUILayout.Popup(new GUIContent("Clips to bake", "Which animation clip will be baked."), )
                    //    break;
            }

            if (checkNextError) { checkNextError = ErrorUtility.SetError(Error.NoAnimationsSelected, clipsToBake == null || clipsToBake.Length == 0); }
            if (!checkNextError) { return; }

            animationTextureColorMode = (ColorMode)EditorGUILayout.EnumPopup("Color Mode", animationTextureColorMode);
            bakeMode = (BakeMode)EditorGUILayout.EnumPopup(new GUIContent("Bake Mode", "Bake mode for faster iteration"), bakeMode);
            framesPerSecondCapture = EditorGUILayout.IntSlider(new GUIContent("FPS Capture", "How many frames per second the clip will be captured at."), framesPerSecondCapture, 1, 120);
            powerOfTwoOptimization = EditorGUILayout.Toggle(new GUIContent("PoT Optimization", "Optimize textures for PoT compression"), powerOfTwoOptimization);
            if (powerOfTwoOptimization)
            {
                sizeOptimizationIteration = EditorGUILayout.IntSlider(new GUIContent("Size Optimization Iterations", "How many times the script try to optimize the texture size."), sizeOptimizationIteration, 0, 8);
            }

            SkinnedMeshRenderer[] meshRenderers = animationRigContainer.GetComponentsInChildren<SkinnedMeshRenderer>();
            Animator[] animators = animationRigContainer.GetComponentsInChildren<Animator>();

            int[] vertexCount = new int[meshRenderers.Length];
            Vector2Int minTextureSize = new Vector2Int(8192, 8192);
            Vector2Int maxTextureSize = Vector2Int.zero;
            Vector2Int[][] textureSizes = new Vector2Int[meshRenderers.Length][];

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                animators[i].runtimeAnimatorController = animatorController;
                vertexCount[i] = meshRenderers[i].sharedMesh.vertexCount;
                textureSizes[i] = new Vector2Int[clipsToBake.Length];
            }

            GetTextureInformation(ref textureSizes, ref minTextureSize, ref maxTextureSize);

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.LabelField($"Animations: {(bakeMode != BakeMode.Single ? clipsToBake.Length : 1)}");
            EditorGUILayout.LabelField($"Frames to bake: {totalFrames}");
            switch (bakeMode)
            {
                case BakeMode.Single:
                    EditorGUILayout.LabelField($"Result texture size: {maxTextureSize}");
                    break;
                default:
                    EditorGUILayout.LabelField($"Result texture size: {minTextureSize} (min), {maxTextureSize} (max)");
                    break;
            }
            EditorGUILayout.LabelField($"Estimated bake time: {totalTime / (60f / framesPerSecondCapture)} seconds");

            if (GUILayout.Button(bakeMode.ToButtonString()))
            {
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    EditorCoroutineUtility.StartCoroutine(CreateAnimationTexturesForRig(textureSizes[i], vertexCount[i], framesPerSecondCapture, clipsToBake, animationRigContainer.transform.GetChild(i).gameObject), this);
                }
            }
        }

        private void GetTextureInformation(ref Vector2Int[][] sizes, ref Vector2Int minSize, ref Vector2Int maxSize)
        {
            foreach (AnimationClip clip in clipsToBake)
            {
                totalTime += clip.length;
            }

            if (powerOfTwoOptimization)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    for (int j = 0; j < clipsToBake.Length; j++)
                    {
                        float clipTime = clipsToBake[j].length;
                        float clipFrames = (int)(clipTime * framesPerSecondCapture);

                        int pixelCount = (int)(clipFrames * filters[i].sharedMesh.vertexCount);
                        squareFrames = Mathf.FloorToInt(Mathf.Sqrt(pixelCount));
                        int pot = Mathf.NextPowerOfTwo(squareFrames);
                        Vector2Int optimizedSquareFrames = new Vector2Int(pot, pot);
                        optimizedSquareFrames = GetSmallestPOT(optimizedSquareFrames, pixelCount, sizeOptimizationIteration);
                        minSize = Vector2Int.Min(optimizedSquareFrames, minSize);
                        maxSize = Vector2Int.Max(optimizedSquareFrames, maxSize);

                        sizes[i][j] = optimizedSquareFrames;
                    }
                }
            }
            else
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    for (int j = 0; j < clipsToBake.Length; j++)
                    {
                        float clipTime = clipsToBake[j].length;
                        int clipFrames = (int)(clipTime * framesPerSecondCapture);

                        sizes[i][j] = new Vector2Int(filters[i].sharedMesh.vertexCount, clipFrames);

                        minSize = Vector2Int.Min(sizes[i][j], minSize);
                        maxSize = Vector2Int.Max(sizes[i][j], maxSize);
                    }
                }
            }
            totalFrames = (int)(totalTime * framesPerSecondCapture);
        }

        #region Util 
        private Vector2Int GetSmallestPOT(Vector2Int unoptimized, int pixelCount, int iterations)
        {
            for (int k = 0; k < iterations; k++)
            {
                if ((unoptimized.x * unoptimized.y) / 2 > pixelCount)
                {
                    unoptimized.x /= 2;
                }
                if (((unoptimized.x / 2) * (unoptimized.y * 2)) > pixelCount)
                {
                    unoptimized.x /= 2;
                    unoptimized.y *= 2;
                }
            }

            return unoptimized;
        }

        private string[] GetAnimationClipNames(AnimationClip[] clips)
        {
            string[] result = new string[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                result[i] = clips[i].name;
            }
            return result;
        }

        private IEnumerator CreateAnimationTexturesForRig(Vector2Int[] sizes, int vertexCount, int fps, AnimationClip[] clips, GameObject original)
        {
            string extension = animationTextureColorMode == ColorMode.HDR ? "exr" : "png";
            string path = EditorUtility.SaveFilePanelInProject("Save Baked Animation Array", original.name, extension, "Please save your baked animations");
            if (path.Length == 0)
            {
                yield break;
            }

            string filePrefix = path.Remove(path.Length - $".{extension}".Length);

            if (!Directory.Exists(filePrefix))
            {
                Directory.CreateDirectory(filePrefix);
            }

            string[] split = filePrefix.Split('/');
            filePrefix = string.Concat(filePrefix, "/", split[split.Length - 1]);

            GameObject prefab = GameObject.Instantiate(original);
            Animator animator = prefab.GetComponentInChildren<Animator>();
            SkinnedMeshRenderer skinnedMesh = prefab.GetComponentInChildren<SkinnedMeshRenderer>();

            for (int clip = 0; clip < clips.Length; clip++)
            {
                Texture2D result = new Texture2D(sizes[clip].x, sizes[clip].y, DefaultFormat.HDR, TextureCreationFlags.None);
                float clipTime = clips[clip].length;
                int frames = (int)(clipTime * framesPerSecondCapture);

                Color[] clearColors = new Color[sizes[clip].x * sizes[clip].y];
                for (int i = 0; i < clearColors.Length; i++)
                {
                    clearColors[i] = Color.clear;
                }
                result.SetPixels(clearColors);

                animator.Play(clips[clip].name, 0, 0);
                yield return null;
                animator.Update(0);

                float animationDeltaTime = 1f / fps;

                bool pixelOutOfRange = false;

                int y = 0;
                int x = 0;
                for (int i = 0; i < frames; i++)
                {
                    Mesh meshFrame = new Mesh();
                    skinnedMesh.BakeMesh(meshFrame);
                    meshFrame.RecalculateBounds();

                    //red = x
                    //green = y
                    //blue = z
                    for (int j = 0; j < vertexCount; j++)
                    {
                        Color pixel = Color.clear;
                        Vector3 position = meshFrame.vertices[j];
                        position = position + Vector3.one * 0.5f;
                        pixel.r = position.x;
                        pixel.g = position.y;
                        pixel.b = position.z;
                        pixel.a = 1;

                        pixelOutOfRange |=
                            position.x > 1 || position.x < 0
                         || position.y > 1 || position.y < 0
                         || position.z > 1 || position.z < 0;

                        result.SetPixel(x, y, pixel);

                        x++;
                        if (x == sizes[clip].x)
                        {
                            y++;
                            x = 0;
                        }
                    }

                    ErrorUtility.SetError(Error.PixelOutOfRange, pixelOutOfRange);

                    GameObject.DestroyImmediate(meshFrame);

                    animator.Update(animationDeltaTime);

                    yield return null;
                }

                #region Export
                string clipPath = $"{filePrefix}@{clips[clip].name} v{vertexCount} f{frames} s{sizes[clip]}.{extension}";
                byte[] encodedTex;
                if (animationTextureColorMode == ColorMode.HDR)
                {
                    encodedTex = result.EncodeToEXR();
                }
                else
                {
                    encodedTex = result.EncodeToPNG();
                }

                using (FileStream stream = File.Open(clipPath, FileMode.OpenOrCreate))
                {
                    stream.Write(encodedTex, 0, encodedTex.Length);
                }

                AssetDatabase.ImportAsset(clipPath);
                GameObject.DestroyImmediate(result);
                #endregion
            }

            GameObject.DestroyImmediate(prefab);
            Debug.Log($"Finished all clips for object {original.name}");
        }
        #endregion
    }
}
#endif