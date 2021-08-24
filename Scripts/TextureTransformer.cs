#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

namespace Mec2Tex
{
    internal class TextureTransformer : EditorView
    {
        private ColorMode textureColorMode = ColorMode.HDR;
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

        public override void View()
        {
            transformTexture = (Texture2D)EditorGUILayout.ObjectField("Texture", transformTexture, typeof(Texture2D), false);

            if (ErrorUtility.SetError(Error.MissingTexture, transformTexture == null)) { return; }

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
            if (textureColorMode == ColorMode.HDR)
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
            GameObject.DestroyImmediate(result);
            #endregion

            Debug.Log("Finished");
        }
    }
}
#endif