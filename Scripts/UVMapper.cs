#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mec2Tex
{
    internal class UVMapper : EditorView
    {
        private Mesh uvMesh;
        private UVLayer uvLayer = UVLayer.UV1;
        private bool combineUVs = false;
        private float uvMeshScale = 1;

        public override void View()
        {
            uvMesh = (Mesh)EditorGUILayout.ObjectField("Mesh", uvMesh, typeof(Mesh), true);

            if (ErrorUtility.SetError(Error.MissingUVMesh, uvMesh == null)) { return; }

            uvLayer = (UVLayer)EditorGUILayout.EnumPopup("UV Layer", uvLayer);
            uvMeshScale = EditorGUILayout.FloatField("Mesh Scale", uvMeshScale);

            List<Vector2> uvList = new List<Vector2>();
            uvMesh.GetUVs((int)uvLayer, uvList);

            if (uvList.Count > 0)
            {
                GUILayout.Label(Error.UVAlreadyExists.ToErrorString());
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
    }
}
#endif