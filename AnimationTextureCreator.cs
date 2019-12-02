using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimationTextureCreator : EditorWindow
{
    private GameObject riggedPrefab;
    private string errorMessage;
    private List<SelectedChild> finalChildren;
    private bool finalChildFoldout;
    private List<SelectedChild> selectedFinalChildren;
    private int framesPerSecondCapture = 60;
    private float scaler = 0.5f;

    private Vector2 scrollPos = Vector2.zero;

    [MenuItem("ifelse/Animation Texture Creator")]
    static void Init()
    {
        AnimationTextureCreator window = (AnimationTextureCreator)EditorWindow.GetWindow(typeof(AnimationTextureCreator));
    }

    void OnGUI()
    {
        riggedPrefab = (GameObject)EditorGUILayout.ObjectField("Rigged Prefab", riggedPrefab, typeof(GameObject), true);

        if (riggedPrefab == null)
        {
            return;
        }
        errorMessage = null;
        if (riggedPrefab.GetComponentInChildren<SkinnedMeshRenderer>() == null)
        {
            errorMessage = "ERROR: Could not find a Skinned Mesh Renderer\nin the object's children.";
        }
        if (riggedPrefab.GetComponentInChildren<Animator>() == null)
        {
            errorMessage = "ERROR: Could not find an Animator\nin the object's children.";
        }
        if (riggedPrefab.GetComponentInChildren<Animator>().runtimeAnimatorController == null)
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

        if (finalChildren == null || finalChildren.Count == 0)
        {
            finalChildren = new List<SelectedChild>();
            GetFinalChild(riggedPrefab.transform, finalChildren);
        }

        finalChildFoldout = EditorGUILayout.Foldout(finalChildFoldout, "Final Children");
        if (finalChildFoldout)
        {
            if (selectedFinalChildren == null)
            {
                selectedFinalChildren = new List<SelectedChild>();
            }

            for (int i = 0; i < finalChildren.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(finalChildren[i].child.name);
                finalChildren[i].selected = EditorGUILayout.Toggle("", finalChildren[i].selected);
                EditorGUILayout.EndHorizontal();
            }
        }

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
                CreateAnimationTexture(textureSize, vertexCount, totalFrames, framesPerSecondCapture, animationClips[0]);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void GetFinalChild(Transform child, List<SelectedChild> finalChildren)
    {
        if (child.childCount == 0)
        {
            finalChildren.Add(new SelectedChild
            {
                selected = false,
                child = child
            });
        }
        else
        {
            for (int i = 0; i < child.childCount; i++)
            {
                GetFinalChild(child.GetChild(i), finalChildren);
            }
        }
    }

    void CreateAnimationTexture(int size, int vertexCount, int frames, int fps, AnimationClip clip)
    {
        GameObject prefab = Instantiate(riggedPrefab);
        Animator animator = prefab.GetComponentInChildren<Animator>();

        SkinnedMeshRenderer skinnedMesh = prefab.GetComponentInChildren<SkinnedMeshRenderer>();

        Texture2D result = new Texture2D(size, size, UnityEngine.Experimental.Rendering.DefaultFormat.LDR, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);

        animator.Play(clip.name, 0);

        Color[] clearColors = new Color[size * size];
        for (int i = 0; i < clearColors.Length; i++)
        {
            clearColors[i] = Color.clear;
        }
        result.SetPixels(clearColors);

        int y = 0;
        int x = 0;
        for (int i = 0; i < frames; i++)
        {
            //red = x
            //green = y
            //blue = z
            //alpha = weight (not implemented, so defaults to 1)
            for (int j = 0; j < vertexCount; j++)
            {
                Color pixel = Color.clear;
                Vector3 position = skinnedMesh.sharedMesh.vertices[j] * scaler;
                pixel.r = position.x;
                pixel.g = position.y;
                pixel.b = position.z;
                pixel.a = 1;

                result.SetPixel(x, y, pixel);

                y++;
                if (y == size)
                {
                    x++;
                    y = 0;
                }
            }

            animator.Update(1f / fps);
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

    public class SelectedChild
    {
        public bool selected;
        public Transform child;
    }
}