#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

namespace Mec2Tex
{
    internal class MeshBaker : EditorView
    {
        private Mesh textureMesh;
        private ColorMode meshTextureColorMode = ColorMode.HDR;
        private float meshTextureScaler = 1;

        public override void View()
        {
            textureMesh = (Mesh)EditorGUILayout.ObjectField("Mesh", textureMesh, typeof(Mesh), false);

            if (ErrorUtility.SetError(Error.MissingMesh, textureMesh == null)) { return; }

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

            bool pixelOutOfRange = false;

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

                pixelOutOfRange |=
                    position.x > 1 || position.x < 0
                 || position.y > 1 || position.y < 0
                 || position.z > 1 || position.z < 0;

                result.SetPixel(x, y, pixel);

                y++;
                if (y == size)
                {
                    x++;
                    y = 0;
                }
            }

            ErrorUtility.SetError(Error.PixelOutOfRange, pixelOutOfRange);

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
            GameObject.DestroyImmediate(result);
            #endregion
        }
    }
}
#endif