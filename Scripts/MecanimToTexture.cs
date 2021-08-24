#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Mec2Tex
{
    internal class MecanimToTexture : EditorWindow
    {
        private readonly string[] TabTitles = new string[] {
            "Animation Baker",
            "Mesh Baker",
            "UV Mapper",
            "Texture Transformer"
        };
        private int currentTab;
        private Vector2 scrollPosition = Vector2.zero;

        private ErrorUtility errorUtility = new ErrorUtility();
        private AnimationBaker animationBaker = new AnimationBaker();
        private MeshBaker meshBaker = new MeshBaker();
        private UVMapper uvMapper = new UVMapper();
        private TextureTransformer textureTransformer = new TextureTransformer();

        [MenuItem("Tools/ifelse/Mecanim2Texture")]
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
                    animationBaker.View();
                    break;
                case 1:
                    meshBaker.View();
                    break;
                case 2:
                    uvMapper.View();
                    break;
                case 3:
                    textureTransformer.View();
                    break;
            }
            EditorGUILayout.EndScrollView();

            errorUtility.View();
        }
    }
}
#endif