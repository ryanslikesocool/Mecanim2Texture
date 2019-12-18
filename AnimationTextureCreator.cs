using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimationTextureCreator : EditorWindow
{
    private readonly string[] TabTitles = new string[] {
        "Animation Texture",
        "UV Map"
    };
    private int currentTab;

    #region Animation Texture Props
    private GameObject riggedPrefab;
    private int framesPerSecondCapture = 60;
    private float scaler = 0.5f;
    #endregion

    #region UV Map Props
    private Texture2D animationTexture;
    private GameObject meshContainer;
    private UVLayer uvLayer;
    private RendererType rendererType;
    #endregion

    private Vector2 scrollPos = Vector2.zero;

    [MenuItem("ifelse/Animation Texture Creator")]
    static void Init()
    {
        AnimationTextureCreator window = (AnimationTextureCreator)EditorWindow.GetWindow(typeof(AnimationTextureCreator));
    }

    void OnGUI()
    {
        currentTab = GUILayout.Toolbar(currentTab, TabTitles);

        switch (currentTab)
        {
            case 0:
                AnimationTextureEditor();
                break;
            case 1:
                UVMapEditor();
                break;
        }
    }

    #region Animation Texture
    void AnimationTextureEditor()
    {
        riggedPrefab = (GameObject)EditorGUILayout.ObjectField("Rigged Prefab", riggedPrefab, typeof(GameObject), true);

        if (riggedPrefab == null)
        {
            return;
        }
        string errorMessage = null;
        if (riggedPrefab.GetComponentInChildren<SkinnedMeshRenderer>() == null)
        {
            errorMessage = "ERROR: Could not find a Skinned Mesh Renderer\nin the object's hierarchy.";
        }
        if (riggedPrefab.GetComponentInChildren<Animator>() == null)
        {
            errorMessage = "ERROR: Could not find an Animator\nin the object's hierarchy.";
        }
        if (riggedPrefab.GetComponentInChildren<Animator>()?.runtimeAnimatorController == null)
        {
            errorMessage = "ERROR: Could not find a Runtime Animator Controller\nin the Animator's settings.";
        }
        if (errorMessage != null)
        {
            EditorGUILayout.LabelField(errorMessage);
            return;
        }
        if (riggedPrefab.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Length > 1)
        {
            errorMessage = "Warning: Only the first Animation Clip will be exported to a texture.";
            EditorGUILayout.LabelField(errorMessage);
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        framesPerSecondCapture = EditorGUILayout.IntSlider("FPS Capture", framesPerSecondCapture, 24, 120);
        scaler = EditorGUILayout.FloatField("Scaler", scaler);

        EditorGUILayout.Space(16);

        AnimationClip[] animationClips = riggedPrefab.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips;
        if (animationClips.Length > 0)
        {
            float totalTime = animationClips[0].length;
            int vertexCount = riggedPrefab.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.vertexCount;
            int totalFrames = Mathf.FloorToInt(totalTime * framesPerSecondCapture);
            int squareFrames = Mathf.FloorToInt(Mathf.Sqrt(totalFrames * vertexCount));
            int textureSize = Mathf.NextPowerOfTwo(squareFrames);
            EditorGUILayout.LabelField("Frames to convert: " + totalFrames);
            EditorGUILayout.LabelField("Pixels to fill: " + vertexCount * totalFrames);
            EditorGUILayout.LabelField("Resulting texture size: " + textureSize + "x" + textureSize);
            if (GUILayout.Button("Create Animation Texture"))
            {
                Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(CreateAnimationTexture(textureSize, vertexCount, totalFrames, framesPerSecondCapture, animationClips[0]), this);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    IEnumerator CreateAnimationTexture(int size, int vertexCount, int frames, int fps, AnimationClip clip)
    {
        GameObject prefab = Instantiate(riggedPrefab);
        Animator animator = prefab.GetComponentInChildren<Animator>();

        SkinnedMeshRenderer skinnedMesh = prefab.GetComponentInChildren<SkinnedMeshRenderer>();

        Texture2D result = new Texture2D(size, size, UnityEngine.Experimental.Rendering.DefaultFormat.LDR, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);


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
            Mesh meshFrame = new Mesh();
            skinnedMesh.BakeMesh(meshFrame);

            //red = x
            //green = y
            //blue = z
            //alpha = weight (not implemented, so defaults to 1)
            for (int j = 0; j < vertexCount; j++)
            {
                Color pixel = Color.clear;
                Vector3 position = (meshFrame.vertices[j] * scaler) + (Vector3.one * 0.5f);
                pixel.r = position.x;
                pixel.g = position.y;
                pixel.b = position.z;
                pixel.a = 1;

                if (position.x > 1 || position.x < 0
                || position.y > 1 || position.y < 0
                || position.z > 1 || position.z < 0)
                {
                    Debug.LogWarning("A pixel was out of range!  It's clamped, but it won't appear correctly");
                }

                result.SetPixel(x, y, pixel);

                y++;
                if (y == size)
                {
                    x++;
                    y = 0;
                }
            }

            DestroyImmediate(meshFrame);

            animator.Update(animationDeltaTime);

            yield return null;
        }

        DestroyImmediate(prefab);

        byte[] png = result.EncodeToPNG();
        string savePath = Path.Combine(Application.dataPath, string.Concat(riggedPrefab.name, ".png"));

        FileStream stream = File.Open(savePath, FileMode.OpenOrCreate);
        stream.Write(png, 0, png.Length);
        stream.Close();

        AssetDatabase.ImportAsset(string.Concat("Assets/", riggedPrefab.name, ".png"));
        DestroyImmediate(result);
    }
    #endregion

    #region UV Map
    void UVMapEditor()
    {
        animationTexture = (Texture2D)EditorGUILayout.ObjectField("Animation Texture", animationTexture, typeof(Texture2D), false);
        meshContainer = (GameObject)EditorGUILayout.ObjectField("Mesh Container", meshContainer, typeof(GameObject), true);
        uvLayer = (UVLayer)EditorGUILayout.EnumPopup("UV Layer", uvLayer);
        rendererType = (RendererType)EditorGUILayout.EnumPopup("Renderer Type", rendererType);

        bool sendUVError = false;

        Mesh mesh = null;
        if (meshContainer != null)
        {
            if (rendererType == RendererType.Normal && meshContainer.GetComponentInChildren<MeshFilter>() != null)
            {
                mesh = meshContainer.GetComponentInChildren<MeshFilter>().sharedMesh;
            }
            else if (meshContainer.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            {
                mesh = meshContainer.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            }
            if (mesh != null)
            {
                List<Vector2> uvList = new List<Vector2>();
                mesh.GetUVs((int)uvLayer, uvList);
                sendUVError = uvList.Count > 0;
            }
        }

        if (sendUVError)
        {
            EditorGUILayout.LabelField("Warning: This mesh already has a UV on this layer.");
            EditorGUILayout.LabelField("Are you sure you want to overwrite it?");
        }

        if (mesh != null)
        {
            EditorGUILayout.Space(16);
            if (GUILayout.Button("Set UV For Layer"))
            {
                ApplyUVToLayer(mesh);
            }
        }
    }

    void ApplyUVToLayer(Mesh originalMesh)
    {
        float gridInterval = 1f / animationTexture.height;
        Vector2 initialOffset = Vector2.one * gridInterval * 0.5f;

        Vector2[] resultUV = new Vector2[originalMesh.vertexCount];
        int x = 0;
        int y = 0;
        for (int i = 0; i < resultUV.Length; i++)
        {
            Vector2 gridPosition = new Vector2(x, y) * gridInterval;

            resultUV[i].x = i;
        }

        originalMesh.SetUVs((int)uvLayer, resultUV);
        Mesh instantiation = Instantiate(originalMesh);
        instantiation.name = originalMesh.name;
        if (rendererType == RendererType.Normal)
        {
            meshContainer.GetComponentInChildren<MeshFilter>().sharedMesh = instantiation;
        }
        else
        {
            meshContainer.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh = instantiation;
        }
    }

    public enum RendererType
    {
        Skinned,
        Normal,
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
}
