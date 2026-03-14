namespace CahtFramework.Pikachu.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;

    [CustomEditor(typeof(ThemeData), true)]
    public class ThemeDataEditor : IdentifiedObjectEditor
    {
        private SerializedProperty backgroundSpriteProperty;
        private SerializedProperty backgroundMusicProperty;
        private SerializedProperty pieceSkinsProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            this.backgroundSpriteProperty = this.serializedObject.FindProperty("backgroundSprite");
            this.backgroundMusicProperty  = this.serializedObject.FindProperty("backgroundMusic");
            this.pieceSkinsProperty       = this.serializedObject.FindProperty("pieceSkins");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            this.serializedObject.Update();

            if (this.DrawFoldoutTitle("Theme Configuration"))
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    EditorGUILayout.PropertyField(this.backgroundSpriteProperty);
                    EditorGUILayout.PropertyField(this.backgroundMusicProperty);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical("HelpBox");
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Piece Skins Mapping", EditorStyles.boldLabel);

                    GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                    if (GUILayout.Button("📁 Load Skins From Folder", GUILayout.Width(180))) this.LoadSkinsFromFolder();
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();

                    CustomEditorUtility.DrawUnderline();
                    EditorGUILayout.Space();

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(this.pieceSkinsProperty, true);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            this.serializedObject.ApplyModifiedProperties();
        }

        private void LoadSkinsFromFolder()
        {
            var dbPath        = $"{PikachuConstants.BASE_RESOURCE_PATH}/Database/PieceDataDatabase.asset";
            var pieceDatabase = AssetDatabase.LoadAssetAtPath<IODatabase>(dbPath);

            if (pieceDatabase == null)
            {
                Debug.LogError($"[ThemeDataEditor] Cannot find PieceDataDatabase at {dbPath}.");

                return;
            }

            var absolutePath = EditorUtility.OpenFolderPanel("Select Skin Sprite Folder", "Assets", "");

            if (string.IsNullOrEmpty(absolutePath)) return;

            if (!absolutePath.StartsWith(Application.dataPath))
            {
                Debug.LogError("[ThemeDataEditor] Please select a folder inside the Assets directory.");

                return;
            }

            var relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
            var guids        = AssetDatabase.FindAssets("t:Sprite", new[] { relativePath });

            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Texture2D", new[] { relativePath });

            if (guids.Length == 0)
            {
                Debug.LogWarning($"[ThemeDataEditor] No Sprites or Textures found in {relativePath}.");

                return;
            }

            var loadedSprites = new List<Sprite>();
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var sprite    = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null) loadedSprites.Add(sprite);
            }

            loadedSprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));

            var allPieces = new List<PieceData>();
            foreach (var data in pieceDatabase.Datas)
                if (data is PieceData pieceData)
                    allPieces.Add(pieceData);

            var themeData = this.target as ThemeData;
            Undo.RecordObject(themeData, "Load Skins From Folder");

            if (loadedSprites.Count < allPieces.Count)
                Debug.LogError(
                    $"[ThemeDataEditor] Theme '{themeData.CodeName}': Found {loadedSprites.Count} sprites but database has {allPieces.Count} pieces. Missing pieces will fallback to default PieceData Icons.");

            themeData.LoadSkinsFromList(allPieces, loadedSprites);

            EditorUtility.SetDirty(themeData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ThemeDataEditor] Successfully mapped {Mathf.Min(loadedSprites.Count, allPieces.Count)} skins for Theme '{themeData.CodeName}'.");
        }
    }
}