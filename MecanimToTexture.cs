#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class MecanimToTexture : EditorWindow
{
    private readonly string[] TabTitles = new string[] {
        "Texture Creator",
        "UV Mapper"
    };
    private int currentTab;
    private Vector2 scrollPosition = Vector2.zero;

    #region Animation Texture Props
    private GameObject animationRigObject;
    private int framesPerSecondCapture = 60;
    private int clipToBakeIndex = 0;
    private float scaler = 0.5f;
    private int minFrame = 0;
    private int maxFrame = 100;
    private ColorMode colorMode = ColorMode.LDR;
    #endregion

    #region UV Map Props
    private Mesh uvMesh;
    private UVLayer uvLayer;
    #endregion

    private List<string> textureErrors = new List<string>();
    private List<string> uvErrors = new List<string>();

    [MenuItem("Window/ifelse/Mecanim2Texture")]
    private static void Init()
    {
        GetWindow(typeof(MecanimToTexture), false, "Mecanim2Texture");
    }

    private void OnGUI()
    {
        GUI.skin.label.wordWrap = true;
        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        currentTab = GUILayout.Toolbar(currentTab, TabTitles);

        EditorGUILayout.BeginScrollView(scrollPosition);
        switch (currentTab)
        {
            case 0:
                AnimationTextureEditor();
                break;
            case 1:
                UVMapEditor();
                break;
        }
        EditorGUILayout.EndScrollView();

        switch(currentTab)
        {
            case 0:
                ErrorView(textureErrors);
                break;
            case 1:
                ErrorView(uvErrors);
                break;
        }
    }

    #region Animation Texture
    private void AnimationTextureEditor()
    {
        animationRigObject = (GameObject)EditorGUILayout.ObjectField("Animation Rig", animationRigObject, typeof(GameObject), true);

        if (animationRigObject == null)
        {
            return;
        }

        bool checkNextError = !SetError(Errors.MissingRigObject, textureErrors, animationRigObject == null);
        if (checkNextError)
        {
            checkNextError = !SetError(Errors.MissingSkinnedMeshRenderer, textureErrors, animationRigObject.GetComponentInChildren<SkinnedMeshRenderer>() == null);
        }
        if (checkNextError)
        {
            checkNextError = !SetError(Errors.MissingAnimator, textureErrors, animationRigObject.GetComponentInChildren<Animator>() == null);
        }
        if (checkNextError)
        {
            checkNextError = !SetError(Errors.MissingRuntimeAnimatorController, textureErrors, animationRigObject.GetComponentInChildren<Animator>().runtimeAnimatorController == null);
        }

        AnimationClip[] animationClips = animationRigObject.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips;
        if (checkNextError)
        {
            checkNextError = !SetError(Errors.NoAnimationClips, textureErrors, animationClips.Length == 0);
        }
        if (!checkNextError)
        {
            return;
        }

        colorMode = (ColorMode)EditorGUILayout.EnumPopup("Color Mode", colorMode);
        clipToBakeIndex = EditorGUILayout.Popup(new GUIContent("Clip to Bake", "Which animation clip will be baked."), clipToBakeIndex, GetAnimationClipNames(animationClips));
        AnimationClip clipToBake = animationClips[clipToBakeIndex];

        framesPerSecondCapture = EditorGUILayout.IntSlider(new GUIContent("FPS Capture", "How many frames per second the clip will be captured at."), framesPerSecondCapture, 24, 120);
        scaler = EditorGUILayout.FloatField(new GUIContent("Bake Scale", "Scale the mesh before baking to reduce the chance of baked pixels being out of range."), scaler);

        float totalTime = clipToBake.length;

        minFrame = EditorGUILayout.IntSlider(new GUIContent("Min Capture Frame", "The frame to start capturing at."), minFrame, 0, maxFrame);
        maxFrame = EditorGUILayout.IntSlider(new GUIContent("Max Capture Frame", "The frame to end capturing at."), maxFrame, minFrame, (int)(totalTime * framesPerSecondCapture));

        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

        int vertexCount = animationRigObject.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.vertexCount;
        int totalFrames = maxFrame - minFrame;
        int squareFrames = Mathf.FloorToInt(Mathf.Sqrt(totalFrames * vertexCount));
        int textureSize = Mathf.NextPowerOfTwo(squareFrames);

        EditorGUILayout.LabelField("Frames to bake: " + totalFrames);
        EditorGUILayout.LabelField("Pixels to fill: " + vertexCount * totalFrames);
        EditorGUILayout.LabelField("Result texture size: " + textureSize + "x" + textureSize);
        if (GUILayout.Button("Bake Animation"))
        {
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(CreateAnimationTexture(textureSize, vertexCount, totalFrames, framesPerSecondCapture, clipToBake), this);
        }
    }

    private string[] GetAnimationClipNames(AnimationClip[] clips)
    {
        string[] result = new string[clips.Length];
        for(int i = 0; i < clips.Length; i++)
        {
            result[i] = clips[i].name;
        }
        return result;
    }

    IEnumerator CreateAnimationTexture(int size, int vertexCount, int frames, int fps, AnimationClip clip)
    {
        GameObject prefab = Instantiate(animationRigObject);
        Animator animator = prefab.GetComponentInChildren<Animator>();
        SkinnedMeshRenderer skinnedMesh = prefab.GetComponentInChildren<SkinnedMeshRenderer>();

        Texture2D result = new Texture2D(size, size, UnityEngine.Experimental.Rendering.DefaultFormat.HDR, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);

        Color[] clearColors = new Color[size * size];
        for (int i = 0; i < clearColors.Length; i++)
        {
            clearColors[i] = Color.clear;
        }
        result.SetPixels(clearColors);

        animator.Play(clip.name, 0, 0);
        yield return null;
        animator.Update(0);

        float animationDeltaTime = 1f / fps;

        int y = 0;
        int x = 0;
        for (int i = 0; i < frames; i++)
        {
            if (i >= minFrame && i < maxFrame)
            {
                Mesh meshFrame = new Mesh();
                skinnedMesh.BakeMesh(meshFrame);

                //red = x
                //green = y
                //blue = z
                for (int j = 0; j < vertexCount; j++)
                {
                    Color pixel = Color.clear;
                    Vector3 position = meshFrame.vertices[j] * scaler + Vector3.one * 0.5f;
                    pixel.r = position.x;
                    pixel.g = position.y;
                    pixel.b = position.z;
                    pixel.a = 1;

                    SetError(Errors.PixelOutOfRange,
                        textureErrors,
                        position.x > 1 || position.x < 0
                     || position.y > 1 || position.y < 0
                     || position.z > 1 || position.z < 0
                    );

                    result.SetPixel(x, y, pixel);

                    y++;
                    if (y == size)
                    {
                        x++;
                        y = 0;
                    }
                }

                DestroyImmediate(meshFrame);
            }

            animator.Update(animationDeltaTime);

            yield return null;
        }

        DestroyImmediate(prefab);

        #region Export
        byte[] encodedTex;
        string extension;
        if (colorMode == ColorMode.HDR)
        {
            encodedTex = result.EncodeToEXR();
            extension = ".exr";
        }
        else
        {
            encodedTex = result.EncodeToPNG();
            extension = ".png";
        }

        string savePath = Path.Combine(Application.dataPath, $"{animationRigObject.name} {clip.name}{extension}");
        using (FileStream stream = File.Open(savePath, FileMode.OpenOrCreate))
        {
            stream.Write(encodedTex, 0, encodedTex.Length);
        }

        string loadPath = $"Assets/{animationRigObject.name} {clip.name}{extension}";
        AssetDatabase.ImportAsset(loadPath);
        DestroyImmediate(result);
        #endregion
    }

    public enum ColorMode
    {
        LDR,
        HDR
    }
    #endregion

    #region UV Map
    private void UVMapEditor()
    {
        uvMesh = (Mesh)EditorGUILayout.ObjectField("Mesh", uvMesh, typeof(Mesh), true);

        if (SetError(Errors.MissingUVMesh, uvErrors, uvMesh == null))
        {
            return;
        }

        uvLayer = (UVLayer)EditorGUILayout.EnumPopup("UV Layer", uvLayer);

        List<Vector2> uvList = new List<Vector2>();
        uvMesh.GetUVs((int)uvLayer, uvList);

        if (uvList.Count > 0)
        {
            GUILayout.Label(Errors.UVAlreadyExists);
        }

        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        if (GUILayout.Button($"Apply UVs to {uvLayer}"))
        {
            ApplyUVToLayer();
        }
    }

    private void ApplyUVToLayer()
    {
        Mesh mesh = Instantiate(uvMesh);
        Vector2[] resultUV = new Vector2[mesh.vertexCount];
        for (int i = 0; i < resultUV.Length; i++)
        {
            resultUV[i].x = i;
        }

        mesh.SetUVs((int)uvLayer, resultUV);
        AssetDatabase.CreateAsset(mesh, $"Assets/{uvMesh.name} Baked.asset");
    }

    public enum UVLayer
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
    #endregion

    #region Error Utility
    private bool SetError(string error, List<string> errorSet, bool condition)
    {
        if (condition && !errorSet.Contains(error))
        {
            errorSet.Add(error);
        }
        else if (!condition && errorSet.Contains(error))
        {
            errorSet.Remove(error);
        }

        return condition;
    }

    private void ErrorView(List<string> errors)
    {
        foreach (string error in errors)
        {
            GUILayout.Label(error);
        }
    }

    private class Errors
    {
        public const string ErrorPrefix = "ERROR: ";
        public const string WarningPrefix = "Warning: ";

        public const string MissingRigObject = ErrorPrefix + "An animation rig object is not assinged for texture creation.  Please assign one.";
        public const string MissingSkinnedMeshRenderer = ErrorPrefix + "Could not find a Skinned Mesh Renderer in the object's hierarchy.";
        public const string MissingAnimator = ErrorPrefix + "Could not find an Animator in the object's hierarchy.";
        public const string MissingRuntimeAnimatorController = ErrorPrefix + "Could not find a Runtime Animator Controller in the Animator's properties.";
        public const string MissingUVMesh = ErrorPrefix + "A mesh is not assigned for UV application.  Please assign one.";

        public const string NoAnimationClips = WarningPrefix + "There are no animation clips on this animator.  You can't bake nonexistant clips.";
        public const string UVAlreadyExists = WarningPrefix + "This mesh already has assigned UVs on this layer.  Applying will overwrite them.";
        public const string PixelOutOfRange = WarningPrefix + "A pixel's value was out of range (less than 0 or greater than 1).  The texture will save with the clamped pixel, but the animation will not look correct.";
    }
    #endregion
}
#endif
