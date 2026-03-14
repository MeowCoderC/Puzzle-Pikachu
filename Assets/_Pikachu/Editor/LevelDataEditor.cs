namespace CahtFramework.Pikachu.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;

    [CustomEditor(typeof(LevelData), true)]
    public class LevelDataEditor : IdentifiedObjectEditor
    {
        private SerializedProperty columnsProperty;
        private SerializedProperty rowsProperty;
        private SerializedProperty availablePiecesProperty;
        private SerializedProperty entranceModuleProperty;
        private SerializedProperty shiftModuleProperty;
        private SerializedProperty generatorModuleProperty;

        private int amountToLoad = 20;

        protected override void OnEnable()
        {
            base.OnEnable();

            this.columnsProperty         = this.serializedObject.FindProperty("columns");
            this.rowsProperty            = this.serializedObject.FindProperty("rows");
            this.availablePiecesProperty = this.serializedObject.FindProperty("availablePieces");
            this.generatorModuleProperty = this.serializedObject.FindProperty("generatorModule");
            this.entranceModuleProperty  = this.serializedObject.FindProperty("entranceModule");
            this.shiftModuleProperty     = this.serializedObject.FindProperty("shiftModule");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            this.serializedObject.Update();

            if (this.DrawFoldoutTitle("Level Configuration"))
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    EditorGUILayout.LabelField("Board Size", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(this.columnsProperty);
                    EditorGUILayout.PropertyField(this.rowsProperty);
                    EditorGUI.indentLevel--;

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Piece Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    EditorGUILayout.BeginHorizontal();
                    this.amountToLoad = EditorGUILayout.IntField(new GUIContent("Amount To Load", "Số lượng Piece ngẫu nhiên muốn lấy từ Database"), this.amountToLoad);

                    GUI.backgroundColor = new Color(0.5f, 0.85f, 0.5f);
                    if (GUILayout.Button("↻ Load Random", GUILayout.Width(110))) this.LoadRandomPieces(this.amountToLoad);
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(2f);

                    EditorGUILayout.PropertyField(this.availablePiecesProperty, true);

                    EditorGUI.indentLevel--;

                    EditorGUILayout.Space();
                    CustomEditorUtility.DrawUnderline();
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(this.generatorModuleProperty);
                    EditorGUILayout.Space(5f);

                    EditorGUILayout.PropertyField(this.entranceModuleProperty);
                    EditorGUILayout.Space(5f);
                    EditorGUILayout.PropertyField(this.shiftModuleProperty);
                }
                EditorGUILayout.EndVertical();
            }

            this.serializedObject.ApplyModifiedProperties();
        }

        private void LoadRandomPieces(int amount)
        {
            var dbPath        = $"{PikachuConstants.BASE_RESOURCE_PATH}/Database/PieceDataDatabase.asset";
            var pieceDatabase = AssetDatabase.LoadAssetAtPath<IODatabase>(dbPath);

            if (pieceDatabase == null)
            {
                Debug.LogError($"[LevelDataEditor] Cannot find PieceDataDatabase at {dbPath}.");

                return;
            }

            var allPieces = new List<PieceData>();
            foreach (var data in pieceDatabase.Datas)
                if (data is PieceData pieceData)
                    allPieces.Add(pieceData);

            if (allPieces.Count == 0)
            {
                Debug.LogWarning("[LevelDataEditor] No PieceData found in database. Please create some pieces first.");

                return;
            }

            for (var i = 0; i < allPieces.Count; i++)
            {
                var temp        = allPieces[i];
                var randomIndex = Random.Range(i, allPieces.Count);
                allPieces[i]           = allPieces[randomIndex];
                allPieces[randomIndex] = temp;
            }

            var levelData = this.target as LevelData;

            Undo.RecordObject(levelData, "Load Random Pieces Into Level");

            levelData.availablePieces.Clear();

            var loadCount = Mathf.Min(amount, allPieces.Count);
            for (var i = 0; i < loadCount; i++) levelData.availablePieces.Add(allPieces[i]);

            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[LevelDataEditor] Successfully loaded {loadCount} random pieces into Level '{levelData.CodeName}'.");
        }
    }
}