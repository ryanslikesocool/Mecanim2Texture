#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Experimental.Rendering;
using Unity.EditorCoroutines.Editor;

public class MecanimToTexture : EditorWindow
{
    private readonly string[] TabTitles = new string[] {
        "Animation Baker",
        "Mesh Baker",
        "UV Mapper",
        "Texture Transformer"
    };
    private int currentTab;
    private Vector2 scrollPosition = Vector2.zero;

    #region Animation Texture Props
    private GameObject animationRigContainer;
    private RuntimeAnimatorController animatorController;
    private BakeMode bakeMode = BakeMode.AllIndividual;
    private int framesPerSecondCapture = 24;
    private int clipToBakeIndex = 0;
    private ColorMode animationTextureColorMode = ColorMode.HDR;
    private bool powerOfTwoOptimization = false;
    private int sizeOptimizationIteration = 4;
    #endregion

    #region Mesh Texture Props
    private Mesh textureMesh;
    private ColorMode meshTextureColorMode = ColorMode.HDR;
    private float meshTextureScaler = 1;
    #endregion

    #region UV Map Props
    private Mesh uvMesh;
    private UVLayer uvLayer = UVLayer.UV1;
    private bool combineUVs = false;
    private float uvMeshScale = 1;
    #endregion

    #region Texture Transformer Props
    private Texture2D transformTexture = null;
    private AnimationCurve transformTranslationX = new AnimationCurve();
    private AnimationCurve transformTranslationY = new AnimationCurve();
    private AnimationCurve transformTranslationZ = new AnimationCurve();
    private AnimationCurve transformRotationX = new AnimationCurve();
    private AnimationCurve transformRotationY = new AnimationCurve();
    private AnimationCurve transformRotationZ = new AnimationCurve();
    private AnimationCurve transformScaleX = new AnimationCurve();
    private AnimationCurve transformScaleY = new AnimationCurve();
    private AnimationCurve transformScaleZ = new AnimationCurve();
    #endregion

    private List<string> animationTextureErrors = new List<string>();
    private List<string> meshTextureErrors = new List<string>();
    private List<string> uvErrors = new List<string>();
    private List<string> textureTransformerErrors = new List<string>();

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

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        switch (currentTab)
        {
            case 0:
                AnimationTextureEditor();
                break;
            case 1:
                MeshTextureEditor();
                break;
            case 2:
                UVMapEditor();
                break;
            case 3:
                TextureTransformerEditor();
                break;
        }
        EditorGUILayout.EndScrollView();

        switch (currentTab)
        {
            case 0:
                ErrorView(animationTextureErrors);
                break;
            case 1:
                ErrorView(meshTextureErrors);
                break;
            case 2:
                ErrorView(uvErrors);
                break;
            case 3:
                ErrorView(textureTransformerErrors);
                break;
        }
    }

    #region Animation Texture
    private void AnimationTextureEditor()
    {
        animationRigContainer = (GameObject)EditorGUILayout.ObjectField("Rig Container", animationRigContainer, typeof(GameObject), true);
        animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator", animatorController, typeof(RuntimeAnimatorController), false);

        if (animationRigContainer == null || animatorController == null) { return; }

        bool checkNextError = !SetError(Errors.MissingRigObject, animationTextureErrors, animationRigContainer == null);
        if (checkNextError)
        {
            checkNextError = !SetError(Errors.MissingSkinnedMeshRenderer, animationTextureErrors, animationRigContainer.GetComponentInChildren<SkinnedMeshRenderer>() == null);
        }
        if (checkNextError)
        {
            checkNextError = !SetError(Errors.MissingAnimator, animationTextureErrors, animationRigContainer.GetComponentInChildren<Animator>() == null);
        }

        AnimationClip[] animationClips = animatorController.animationClips;
        if (checkNextError)
        {
            checkNextError = !SetError(Errors.NoAnimationClips, animationTextureErrors, animationClips.Length == 0);
        }
        if (!checkNextError)
        {
            return;
        }
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
        Vector2Int textureSize = Vector2Int.zero;
        Vector2Int minTextureSize = new Vector2Int(8192, 8192);
        Vector2Int[][] textureSizes = new Vector2Int[meshRenderers.Length][];

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            animators[i].runtimeAnimatorController = animatorController;
            vertexCount[i] = meshRenderers[i].sharedMesh.vertexCount;
            textureSizes[i] = new Vector2Int[animationClips.Length];
        }

        float totalTime = 0;
        int totalFrames = 0;
        int squareFrames = 0;

        AnimationClip clipToBake = null;

        switch (bakeMode)
        {
            case BakeMode.Single:
                clipToBakeIndex = EditorGUILayout.Popup(new GUIContent("Clip to Bake", "Which animation clip will be baked."), clipToBakeIndex, GetAnimationClipNames(animationClips));
                clipToBake = animationClips[clipToBakeIndex];

                totalTime = clipToBake.length;

                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    totalFrames = (int)(totalTime * framesPerSecondCapture);
                    if (powerOfTwoOptimization)
                    {
                        int pixelCount = (int)(totalTime * vertexCount[i]);
                        squareFrames = Mathf.FloorToInt(Mathf.Sqrt(pixelCount));
                        int pot = Mathf.NextPowerOfTwo(squareFrames);
                        Vector2Int optimizedSquareFrames = new Vector2Int(pot, pot);
                        optimizedSquareFrames = GetSmallestPOT(optimizedSquareFrames, pixelCount, sizeOptimizationIteration);
                        minTextureSize = Vector2Int.Min(optimizedSquareFrames, minTextureSize);
                        textureSize = Vector2Int.Max(optimizedSquareFrames, textureSize);

                        textureSizes[i][0] = textureSize;
                    }
                    else
                    {
                        for (int j = 0; j < animationClips.Length; j++)
                        {
                            textureSizes[i][0] = new Vector2Int(vertexCount[i], totalFrames);

                            minTextureSize = Vector2Int.Min(textureSizes[i][j], minTextureSize);
                            textureSize = Vector2Int.Max(textureSizes[i][j], textureSize);
                        }
                    }
                }
                break;
            case BakeMode.AllIndividual:
                foreach (AnimationClip clip in animationClips)
                {
                    totalTime += clip.length;
                }

                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    if (powerOfTwoOptimization)
                    {
                        for (int j = 0; j < animationClips.Length; j++)
                        {
                            float clipTime = animationClips[j].length;
                            float clipFrames = (int)(clipTime * framesPerSecondCapture);

                            int pixelCount = (int)(clipFrames * vertexCount[i]);
                            squareFrames = Mathf.FloorToInt(Mathf.Sqrt(pixelCount));
                            int pot = Mathf.NextPowerOfTwo(squareFrames);
                            Vector2Int optimizedSquareFrames = new Vector2Int(pot, pot);
                            optimizedSquareFrames = GetSmallestPOT(optimizedSquareFrames, pixelCount, sizeOptimizationIteration);
                            minTextureSize = Vector2Int.Min(optimizedSquareFrames, minTextureSize);
                            textureSize = Vector2Int.Max(optimizedSquareFrames, textureSize);

                            textureSizes[i][j] = optimizedSquareFrames;
                        }
                    }
                    else
                    {
                        for (int j = 0; j < animationClips.Length; j++)
                        {
                            float clipTime = animationClips[j].length;
                            int clipFrames = (int)(clipTime * framesPerSecondCapture);

                            textureSizes[i][j] = new Vector2Int(vertexCount[i], clipFrames);

                            minTextureSize = Vector2Int.Min(textureSizes[i][j], minTextureSize);
                            textureSize = Vector2Int.Max(textureSizes[i][j], textureSize);
                        }
                    }
                }
                totalFrames = (int)(totalTime * framesPerSecondCapture);
                break;
        }

        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        EditorGUILayout.LabelField($"Animations: {(bakeMode != BakeMode.Single ? animationClips.Length : 1)}");
        EditorGUILayout.LabelField($"Frames to bake: {totalFrames}");
        switch (bakeMode)
        {
            case BakeMode.Single:
                EditorGUILayout.LabelField($"Result texture size: {textureSize}");
                break;
            case BakeMode.AllIndividual:
                EditorGUILayout.LabelField($"Result texture size: {minTextureSize} (min), {textureSize} (max)");
                break;
        }
        EditorGUILayout.LabelField($"Estimated bake time: {totalTime / (60 / framesPerSecondCapture)} seconds");

        switch (bakeMode)
        {
            case BakeMode.Single:
                if (GUILayout.Button("Bake Animation (Single)"))
                {
                    for (int i = 0; i < meshRenderers.Length; i++)
                    {
                        EditorCoroutineUtility.StartCoroutine(CreateSingleAnimationTextureForRig(textureSizes[i][0], vertexCount[i], totalFrames, framesPerSecondCapture, clipToBake, animationRigContainer.transform.GetChild(i).gameObject), this);
                    }
                }
                break;
            case BakeMode.AllIndividual:
                if (GUILayout.Button("Bake Animations (All)"))
                {
                    for (int i = 0; i < meshRenderers.Length; i++)
                    {
                        EditorCoroutineUtility.StartCoroutine(CreateAllAnimationTexturesForRig(textureSizes[i], vertexCount[i], framesPerSecondCapture, animationClips, animationRigContainer.transform.GetChild(i).gameObject), this);
                    }
                }
                break;
        }
    }

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

    private IEnumerator CreateAllAnimationTexturesForRig(Vector2Int[] sizes, int vertexCount, int fps, AnimationClip[] clips, GameObject original)
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

        GameObject prefab = Instantiate(original);
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

                    if (animationTextureColorMode == ColorMode.LDR)
                    {
                        SetError(Errors.PixelOutOfRange,
                            animationTextureErrors,
                            position.x > 1 || position.x < 0
                         || position.y > 1 || position.y < 0
                         || position.z > 1 || position.z < 0
                        );
                    }

                    result.SetPixel(x, y, pixel);

                    x++;
                    if (x == sizes[clip].x)
                    {
                        y++;
                        x = 0;
                    }
                }

                DestroyImmediate(meshFrame);

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
            DestroyImmediate(result);
            #endregion
        }

        DestroyImmediate(prefab);
        Debug.Log($"Finished all clips for object {original.name}");
    }

    private IEnumerator CreateSingleAnimationTextureForRig(Vector2Int size, int vertexCount, int frames, int fps, AnimationClip clip, GameObject original)
    {
        string extension = animationTextureColorMode == ColorMode.HDR ? "exr" : "png";
        string path = EditorUtility.SaveFilePanelInProject("Save Baked Animation Array", original.name, animationTextureColorMode == ColorMode.HDR ? "exr" : "png", "Please save your baked animations");
        if (path.Length == 0)
        {
            yield break;
        }
        string filePrefix = path.Remove(path.Length - $".{extension}".Length);

        GameObject prefab = Instantiate(original);
        Animator animator = prefab.GetComponentInChildren<Animator>();
        SkinnedMeshRenderer skinnedMesh = prefab.GetComponentInChildren<SkinnedMeshRenderer>();

        Texture2D result = new Texture2D(size.x, size.y, DefaultFormat.HDR, TextureCreationFlags.None);

        Color[] clearColors = new Color[size.x * size.y];
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

                SetError(Errors.PixelOutOfRange,
                    animationTextureErrors,
                    position.x > 1 || position.x < 0
                 || position.y > 1 || position.y < 0
                 || position.z > 1 || position.z < 0
                );

                result.SetPixel(x, y, pixel);

                x++;
                if (x == size.x)
                {
                    y++;
                    x = 0;
                }
            }

            DestroyImmediate(meshFrame);
            animator.Update(animationDeltaTime);

            yield return null;
        }

        DestroyImmediate(prefab);

        #region Export
        string clipPath = $"{filePrefix}@{clip.name} v{vertexCount} f{frames} s{size}.{extension}";

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
        DestroyImmediate(result);
        #endregion

        Debug.Log($"Finished clip {clip.name} for object {original.name}");
    }

    public enum ColorMode
    {
        LDR,
        HDR
    }

    public enum BakeMode
    {
        Single,
        [InspectorName("All (Individual)")] AllIndividual,
    }
    #endregion

    #region Mesh Texture
    private void MeshTextureEditor()
    {
        textureMesh = (Mesh)EditorGUILayout.ObjectField("Mesh", textureMesh, typeof(Mesh), false);

        if (SetError(Errors.MissingMesh, meshTextureErrors, textureMesh == null))
        {
            return;
        }

        meshTextureScaler = EditorGUILayout.FloatField("Scaler", meshTextureScaler);
        meshTextureColorMode = (ColorMode)EditorGUILayout.EnumPopup("Color Mode", meshTextureColorMode);

        int squareFrames = Mathf.FloorToInt(Mathf.Sqrt(textureMesh.vertexCount));
        int textureSize = Mathf.NextPowerOfTwo(squareFrames);

        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        EditorGUILayout.LabelField($"Pixels to fill: {textureMesh.vertexCount}");
        EditorGUILayout.LabelField($"Result texture size: {textureSize}");
        if (GUILayout.Button($"Bake Mesh to Texture"))
        {
            BakeMeshToTexture(textureMesh, meshTextureScaler, textureSize, meshTextureColorMode);
        }
    }

    private void BakeMeshToTexture(Mesh mesh, float scaler, int size, ColorMode colorMode)
    {
        string extension = colorMode == ColorMode.HDR ? "exr" : "png";
        string path = EditorUtility.SaveFilePanelInProject("Save Mesh Texture", mesh.name + " Baked", extension, "Please save your baked mesh texture");
        if (path.Length == 0)
        {
            return;
        }
        string filePrefix = path.Remove(path.Length - $".{extension}".Length);

        mesh.RecalculateBounds();

        Texture2D result = new Texture2D(size, size, DefaultFormat.HDR, TextureCreationFlags.None);

        int vertexCount = mesh.vertexCount;
        int x = 0;
        int y = 0;
        for (int j = 0; j < vertexCount; j++)
        {
            Color pixel = Color.clear;
            Vector3 position = mesh.vertices[j] + Vector3.one * 0.5f;
            pixel.r = position.x * scaler;
            pixel.g = position.y * scaler;
            pixel.b = position.z * scaler;
            pixel.a = 1;

            SetError(Errors.PixelOutOfRange,
                meshTextureErrors,
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

        #region Export
        string clipPath = $"{filePrefix} v{vertexCount} s{size}.{extension}";
        byte[] encodedTex;
        if (colorMode == ColorMode.HDR)
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
        DestroyImmediate(result);
        #endregion
    }
    #endregion

    #region UV Map
    private void UVMapEditor()
    {
        uvMesh = (Mesh)EditorGUILayout.ObjectField("Mesh", uvMesh, typeof(Mesh), true);

        if (SetError(Errors.MissingUVMesh, uvErrors, uvMesh == null)) { return; }

        uvLayer = (UVLayer)EditorGUILayout.EnumPopup("UV Layer", uvLayer);
        uvMeshScale = EditorGUILayout.FloatField("Mesh Scale", uvMeshScale);

        List<Vector2> uvList = new List<Vector2>();
        uvMesh.GetUVs((int)uvLayer, uvList);

        if (uvList.Count > 0)
        {
            GUILayout.Label(Errors.UVAlreadyExists);
            combineUVs = EditorGUILayout.Toggle("Combine UVs", combineUVs);
        }

        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        if (GUILayout.Button($"Apply UVs to {uvLayer}"))
        {
            ApplyUVToLayer();
        }
    }

    private void ApplyUVToLayer()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save UV Mesh", uvMesh.name + " UV", "asset", "Please save your UV mesh");
        if (path.Length == 0)
        {
            return;
        }

        Mesh mesh = new Mesh();
        Vector3[] vertices = uvMesh.vertices;
        for (int i = 0; i < uvMesh.vertices.Length; i++)
        {
            vertices[i] *= uvMeshScale;
        }
        mesh.SetVertices(vertices);
        mesh.SetIndices(uvMesh.GetIndices(0), MeshTopology.Triangles, 0);
        mesh.uv = uvMesh.uv;
        mesh.uv2 = uvMesh.uv2;
        mesh.uv3 = uvMesh.uv3;
        mesh.uv4 = uvMesh.uv4;
        mesh.uv5 = uvMesh.uv5;
        mesh.uv6 = uvMesh.uv6;
        mesh.uv7 = uvMesh.uv7;
        mesh.uv8 = uvMesh.uv8;
        mesh.normals = uvMesh.normals;
        mesh.RecalculateBounds();

        List<Vector2> resultUV = new List<Vector2>();
        mesh.GetUVs((int)uvLayer, resultUV);
        if (!combineUVs)
        {
            for (int i = 0; i < resultUV.Count; i++)
            {
                Vector2 uv = resultUV[i];
                uv.x = i;
                resultUV[i] = uv;
            }
        }
        else
        {
            for (int i = 0; i < resultUV.Count; i++)
            {
                Vector2 uv = resultUV[i];
                uv.x += i;
                resultUV[i] = uv;
            }
        }

        mesh.SetUVs((int)uvLayer, resultUV);
        AssetDatabase.CreateAsset(mesh, path);
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

    #region Texture Transformer
    private void TextureTransformerEditor()
    {
        transformTexture = (Texture2D)EditorGUILayout.ObjectField("Texture", transformTexture, typeof(Texture2D), false);

        if (SetError(Errors.MissingTexture, textureTransformerErrors, transformTexture == null)) { return; }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Translation");
        transformTranslationX = EditorGUILayout.CurveField(transformTranslationX);
        transformTranslationY = EditorGUILayout.CurveField(transformTranslationY);
        transformTranslationZ = EditorGUILayout.CurveField(transformTranslationZ);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rotation");
        transformRotationX = EditorGUILayout.CurveField(transformRotationX);
        transformRotationY = EditorGUILayout.CurveField(transformRotationY);
        transformRotationZ = EditorGUILayout.CurveField(transformRotationZ);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Scale");
        transformScaleX = EditorGUILayout.CurveField(transformScaleX);
        transformScaleY = EditorGUILayout.CurveField(transformScaleY);
        transformScaleZ = EditorGUILayout.CurveField(transformScaleZ);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Reset Curves"))
        {
            Keyframe[] keyframes = new Keyframe[]
            {
                new Keyframe(0, 0),
                new Keyframe(1, 0)
            };
            transformTranslationX = new AnimationCurve(keyframes);
            transformTranslationY = new AnimationCurve(keyframes);
            transformTranslationZ = new AnimationCurve(keyframes);
            transformRotationX = new AnimationCurve(keyframes);
            transformRotationY = new AnimationCurve(keyframes);
            transformRotationZ = new AnimationCurve(keyframes);
            transformScaleX = new AnimationCurve(keyframes);
            transformScaleY = new AnimationCurve(keyframes);
            transformScaleZ = new AnimationCurve(keyframes);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        if (GUILayout.Button("Transform Texture"))
        {
            TransformTexture();
        }
    }

    private void TransformTexture()
    {
        string[] extensionSplit = AssetDatabase.GetAssetPath(transformTexture).Split('.');
        string extension = extensionSplit[extensionSplit.Length - 1];
        string path = EditorUtility.SaveFilePanelInProject("Save Transformed Texture", transformTexture.name, extension, "Please save your transformed texture");
        if (path.Length == 0) { return; }

        Texture2D result = new Texture2D(transformTexture.width, transformTexture.height, extension == "png" ? DefaultFormat.LDR : DefaultFormat.HDR, TextureCreationFlags.None);

        Color[] colors = transformTexture.GetPixels();
        for (int f = 0; f < result.width; f++)
        {
            float percent = (float)f / result.height;

            Vector3 translationOffset = new Vector3(transformTranslationX.Evaluate(percent), transformTranslationY.Evaluate(percent), transformTranslationZ.Evaluate(percent));
            Quaternion rotationOffset = Quaternion.Euler(transformRotationX.Evaluate(percent), transformRotationY.Evaluate(percent), transformRotationZ.Evaluate(percent));
            Vector3 scaler = new Vector3(transformScaleX.Evaluate(percent), transformScaleY.Evaluate(percent), transformScaleZ.Evaluate(percent)) + Vector3.one;

            for (int i = 0; i < result.height; i++)
            {
                int index = f * result.height + i;
                Color pixel = colors[index];
                Vector3 p = new Vector3(pixel.r, pixel.g, pixel.b);

                p += translationOffset;
                p = rotationOffset * p;
                p = new Vector3(p.x * scaler.x, p.y * scaler.y, p.z * scaler.z);

                pixel.r = p.x;
                pixel.g = p.y;
                pixel.b = p.z;

                colors[index] = pixel;
            }
        }

        result.SetPixels(colors);

        #region Export
        byte[] encodedTex;
        if (animationTextureColorMode == ColorMode.HDR)
        {
            encodedTex = result.EncodeToEXR();
        }
        else
        {
            encodedTex = result.EncodeToPNG();
        }

        using (FileStream stream = File.Open(path, FileMode.OpenOrCreate))
        {
            stream.Write(encodedTex, 0, encodedTex.Length);
        }

        AssetDatabase.ImportAsset(path);
        DestroyImmediate(result);
        #endregion

        Debug.Log("Finished");
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
        public const string MissingMesh = ErrorPrefix + "A mesh is not assigned for baking.  Please assign one.";
        public const string MissingTexture = WarningPrefix + "A texture is not assigned for transforming.  Please assign one.";

        public const string NoAnimationClips = WarningPrefix + "There are no animation clips on this animator.  You can't bake nonexistant clips.";
        public const string UVAlreadyExists = WarningPrefix + "This mesh already has assigned UVs on this layer.  Applying will overwrite them.";
        public const string PixelOutOfRange = WarningPrefix + "A pixel's value was out of range (less than 0 or greater than 1).  The texture will save with the clamped pixel if set to LDR.";

        public const string CurveOutOfRange = WarningPrefix + "An animation curve has a length that is out of range (less or greater than 0 or 1).  The texture will save while ignoring the out of range values.";
    }
    #endregion
}
#endif
