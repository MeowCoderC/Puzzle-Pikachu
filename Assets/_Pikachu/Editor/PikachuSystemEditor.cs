namespace CahtFramework.Pikachu.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    public class PikachuSystemEditor : EditorWindow
    {
        #region Variables

        private static int                                toolbarIndex          = 0;
        private static Dictionary<Type, Vector2>          scrollPositionsByType = new();
        private static Vector2                            drawingEditorScrollPosition;
        private static Dictionary<Type, IdentifiedObject> selectedObjectsByType   = new();
        private static Dictionary<Type, bool>             isEditingDatabaseByType = new();

        private readonly Dictionary<Type, IODatabase> databasesByType = new();
        private          Type[]                       databaseTypes;
        private          string[]                     databaseTypeNames;

        private Editor cachedEditor;
        private Editor databaseEditor;

        private Texture2D selectedBoxTexture;
        private Texture2D oddRowTexture;
        private Texture2D evenRowTexture;

        private GUIStyle selectedBoxStyle;
        private GUIStyle oddRowStyle;
        private GUIStyle evenRowStyle;

        private string                 searchQuery       = "";
        private List<IdentifiedObject> filteredItems     = new();
        private bool                   needsSearchUpdate = true;
        private Type                   currentDataType;

        #endregion

        #region Setup

        [MenuItem("Tools/Pikachu System")]
        public static void OpenWindow()
        {
            var window = GetWindow<PikachuSystemEditor>("Pikachu System");
            window.titleContent = new GUIContent("Pikachu System");
            window.minSize      = new Vector2(850, 700);
            window.Show();
        }

        private void SetupStyle()
        {
            var isDark = EditorGUIUtility.isProSkin;

            var selectedColor = isDark ? new Color(0.17f, 0.36f, 0.53f) : new Color(0.23f, 0.45f, 0.69f);
            var oddColor      = isDark ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.85f, 0.85f, 0.85f);
            var evenColor     = isDark ? new Color(0.23f, 0.23f, 0.23f) : new Color(0.9f, 0.9f, 0.9f);

            this.selectedBoxTexture = this.CreateColorTexture(selectedColor);
            this.oddRowTexture      = this.CreateColorTexture(oddColor);
            this.evenRowTexture     = this.CreateColorTexture(evenColor);

            this.selectedBoxStyle                   = new GUIStyle();
            this.selectedBoxStyle.normal.background = this.selectedBoxTexture;

            this.oddRowStyle                   = new GUIStyle();
            this.oddRowStyle.normal.background = this.oddRowTexture;

            this.evenRowStyle                   = new GUIStyle();
            this.evenRowStyle.normal.background = this.evenRowTexture;
        }

        private Texture2D CreateColorTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;

            return tex;
        }

        private void SetupDatabases(Type[] dataTypes)
        {
            if (this.databasesByType.Count == 0)
            {
                var dbFolder = $"{PikachuConstants.BASE_RESOURCE_PATH}/Database";
                CustomEditorUtility.EnsureFolderExists(dbFolder);

                foreach (var type in dataTypes)
                {
                    var dbPath   = $"{dbFolder}/{type.Name}Database.asset";
                    var database = AssetDatabase.LoadAssetAtPath<IODatabase>(dbPath);

                    if (database == null)
                    {
                        database = CreateInstance<IODatabase>();
                        AssetDatabase.CreateAsset(database, dbPath);
                        Debug.Log($"[PikachuSystemEditor] Created new database for {type.Name} at {dbPath}");
                    }

                    CustomEditorUtility.EnsureFolderExists($"{PikachuConstants.BASE_RESOURCE_PATH}/{type.Name}");

                    this.databasesByType[type]    = database;
                    scrollPositionsByType[type]   = Vector2.zero;
                    selectedObjectsByType[type]   = null;
                    isEditingDatabaseByType[type] = false;
                }

                this.databaseTypeNames = dataTypes.Select(x => x.Name).ToArray();
                this.databaseTypes     = dataTypes;
            }
        }

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            this.SetupStyle();
            this.SetupDatabases(new[] { typeof(PieceData), typeof(ThemeData), typeof(LevelData) });
        }

        private void OnDisable()
        {
            DestroyImmediate(this.cachedEditor);
            DestroyImmediate(this.databaseEditor);
            DestroyImmediate(this.selectedBoxTexture);
            DestroyImmediate(this.oddRowTexture);
            DestroyImmediate(this.evenRowTexture);
        }

        private void OnGUI()
        {
            if (this.databaseTypeNames == null || this.databaseTypeNames.Length == 0) return;

            EditorGUILayout.Space(5f);
            toolbarIndex = GUILayout.Toolbar(toolbarIndex, this.databaseTypeNames, GUILayout.Height(30f));
            EditorGUILayout.Space(5f);

            CustomEditorUtility.DrawUnderline();
            EditorGUILayout.Space(5f);

            this.DrawDatabase(this.databaseTypes[toolbarIndex]);
        }

        #endregion

        #region Draw Logic

        private void UpdateSearchFilter(Type dataType)
        {
            var database = this.databasesByType[dataType];
            this.filteredItems.Clear();

            if (string.IsNullOrWhiteSpace(this.searchQuery))
            {
                this.filteredItems.AddRange(database.Datas);
            }
            else
            {
                var lowerQuery = this.searchQuery.ToLowerInvariant();
                foreach (var item in database.Datas)
                    if (item != null && !string.IsNullOrEmpty(item.CodeName))
                        if (item.CodeName.ToLowerInvariant().Contains(lowerQuery))
                            this.filteredItems.Add(item);
            }

            this.needsSearchUpdate = false;
        }

        private void DrawDatabase(Type dataType)
        {
            if (this.currentDataType != dataType)
            {
                this.currentDataType   = dataType;
                this.searchQuery       = "";
                this.needsSearchUpdate = true;
                GUI.FocusControl(null);
            }

            var database = this.databasesByType[dataType];
            AssetPreview.SetPreviewTextureCacheSize(Mathf.Max(32, 32 + database.Count));

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(320f));
                {
                    EditorGUILayout.Space(4f);

                    GUI.backgroundColor = isEditingDatabaseByType[dataType] ? new Color(0.4f, 0.7f, 1f) : new Color(0.8f, 0.8f, 0.8f);
                    if (GUILayout.Button($"⚙ Inspect {dataType.Name} Database", GUILayout.Height(24f)))
                    {
                        GUI.FocusControl(null);
                        isEditingDatabaseByType[dataType] = true;
                        selectedObjectsByType[dataType]   = null;
                    }

                    EditorGUILayout.Space(2f);

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUI.backgroundColor = new Color(0.5f, 0.85f, 0.5f);
                        if (GUILayout.Button($"＋ Create New", GUILayout.Height(28f)))
                        {
                            var guid    = Guid.NewGuid();
                            var newData = CreateInstance(dataType) as IdentifiedObject;

                            dataType.BaseType.GetField("codeName", BindingFlags.NonPublic | BindingFlags.Instance)
                                .SetValue(newData, guid.ToString());

                            var assetPath = $"{PikachuConstants.BASE_RESOURCE_PATH}/{dataType.Name}/{dataType.Name.ToUpper()}_{guid}.asset";
                            AssetDatabase.CreateAsset(newData, assetPath);

                            database.Add(newData);
                            EditorUtility.SetDirty(database);
                            AssetDatabase.SaveAssets();

                            isEditingDatabaseByType[dataType] = false;
                            selectedObjectsByType[dataType]   = newData;
                            this.needsSearchUpdate            = true;

                            Debug.Log($"[PikachuSystemEditor] Created new {dataType.Name} at {assetPath}");
                        }

                        if (dataType == typeof(PieceData))
                        {
                            GUI.backgroundColor = new Color(0.7f, 0.6f, 1f);
                            if (GUILayout.Button($"📁 Bulk From Folder", GUILayout.Height(28f))) this.ExecuteBulkPieceImport(database);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    // ---------------------------

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
                        if (GUILayout.Button($"🗑 Remove Last", GUILayout.Height(24f)))
                        {
                            var lastData = database.Count > 0 ? database.Datas.Last() : null;
                            if (lastData)
                            {
                                database.Remove(lastData);
                                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(lastData));
                                EditorUtility.SetDirty(database);
                                AssetDatabase.SaveAssets();
                                this.needsSearchUpdate = true;
                                Debug.Log($"[PikachuSystemEditor] Removed last {dataType.Name}");
                            }
                        }

                        GUI.backgroundColor = new Color(0.5f, 0.7f, 0.9f);
                        if (GUILayout.Button($"↕ Sort By Name", GUILayout.Height(24f)))
                        {
                            database.SortByCodeName();
                            EditorUtility.SetDirty(database);
                            AssetDatabase.SaveAssets();
                            this.needsSearchUpdate = true;
                            Debug.Log($"[PikachuSystemEditor] Sorted {dataType.Name} Database by Name");
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.Space(6f);

                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    EditorGUI.BeginChangeCheck();
                    this.searchQuery = EditorGUILayout.TextField(this.searchQuery, EditorStyles.toolbarSearchField);
                    if (EditorGUI.EndChangeCheck()) this.needsSearchUpdate = true;

                    if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    {
                        this.searchQuery       = "";
                        this.needsSearchUpdate = true;
                        GUI.FocusControl(null);
                    }

                    EditorGUILayout.EndHorizontal();

                    CustomEditorUtility.DrawUnderline();
                    EditorGUILayout.Space(2f);

                    if (this.needsSearchUpdate) this.UpdateSearchFilter(dataType);

                    scrollPositionsByType[dataType] = EditorGUILayout.BeginScrollView(scrollPositionsByType[dataType], false, true,
                        GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
                    {
                        var rowIndex = 0;

                        foreach (var data in this.filteredItems)
                        {
                            if (data == null) continue;

                            var labelWidth = data.Icon != null ? 210f : 255f;
                            var rowStyle   = rowIndex % 2 == 0 ? this.evenRowStyle : this.oddRowStyle;

                            if (selectedObjectsByType[dataType] == data) rowStyle = this.selectedBoxStyle;

                            EditorGUILayout.BeginHorizontal(rowStyle, GUILayout.Height(44f));
                            {
                                if (data.Icon)
                                {
                                    var preview = AssetPreview.GetAssetPreview(data.Icon);
                                    GUILayout.Label(preview, GUILayout.Height(40f), GUILayout.Width(40f));
                                }
                                else
                                {
                                    GUILayout.Space(44f);
                                }

                                EditorGUILayout.LabelField(data.CodeName, EditorStyles.boldLabel, GUILayout.Width(labelWidth), GUILayout.Height(40f));

                                EditorGUILayout.BeginVertical();
                                {
                                    EditorGUILayout.Space(10f);

                                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                                    if (GUILayout.Button("✖", GUILayout.Width(24f), GUILayout.Height(24f)))
                                    {
                                        database.Remove(data);
                                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(data));
                                        EditorUtility.SetDirty(database);
                                        AssetDatabase.SaveAssets();

                                        GUI.backgroundColor    = Color.white;
                                        this.needsSearchUpdate = true;

                                        Debug.Log($"[PikachuSystemEditor] Deleted {data.CodeName}");

                                        break;
                                    }

                                    GUI.backgroundColor = Color.white;
                                }
                                EditorGUILayout.EndVertical();

                                GUILayout.Space(5f);
                            }
                            EditorGUILayout.EndHorizontal();

                            var lastRect = GUILayoutUtility.GetLastRect();
                            if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                            {
                                GUI.FocusControl(null);
                                GUIUtility.keyboardControl = 0;

                                var targetData = data;
                                var targetType = dataType;

                                EditorApplication.delayCall += () =>
                                {
                                    isEditingDatabaseByType[targetType] = false;
                                    selectedObjectsByType[targetType]   = targetData;
                                    drawingEditorScrollPosition         = Vector2.zero;
                                    this.Repaint();
                                };

                                Event.current.Use();
                            }

                            rowIndex++;
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();

                if (isEditingDatabaseByType[dataType])
                {
                    drawingEditorScrollPosition = EditorGUILayout.BeginScrollView(drawingEditorScrollPosition);
                    {
                        EditorGUILayout.Space(4f);
                        EditorGUILayout.LabelField($"⚙ {dataType.Name} Database Settings", EditorStyles.largeLabel);
                        EditorGUILayout.Space(4f);
                        CustomEditorUtility.DrawUnderline();
                        EditorGUILayout.Space(4f);

                        Editor.CreateCachedEditor(database, null, ref this.databaseEditor);
                        EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
                        this.databaseEditor.OnInspectorGUI();
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndScrollView();
                }
                else if (selectedObjectsByType[dataType])
                {
                    drawingEditorScrollPosition = EditorGUILayout.BeginScrollView(drawingEditorScrollPosition);
                    {
                        EditorGUILayout.Space(2f);
                        Editor.CreateCachedEditor(selectedObjectsByType[dataType], null, ref this.cachedEditor);

                        EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
                        this.cachedEditor.OnInspectorGUI();
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region PieceData Specific Tools

        private void ExecuteBulkPieceImport(IODatabase database)
        {
            var absolutePath = EditorUtility.OpenFolderPanel("Select Sprite Folder", "Assets", "");

            if (string.IsNullOrEmpty(absolutePath)) return;

            if (!absolutePath.StartsWith(Application.dataPath))
            {
                Debug.LogError("[PikachuSystemEditor] Please select a folder inside the Assets directory.");

                return;
            }

            var relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
            var guids        = AssetDatabase.FindAssets("t:Sprite", new[] { relativePath });

            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Texture2D", new[] { relativePath });

            if (guids.Length == 0)
            {
                Debug.LogWarning($"[PikachuSystemEditor] No Sprites or Textures found in {relativePath}. Make sure images are imported as Sprite (2D and UI).");

                return;
            }

            var createdCount  = 0;
            var iconField     = typeof(IdentifiedObject).GetField("icon", BindingFlags.NonPublic | BindingFlags.Instance);
            var codeNameField = typeof(IdentifiedObject).GetField("codeName", BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var sprite    = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                if (sprite == null) continue;

                var exists = false;
                foreach (var existingItem in database.Datas)
                    if (existingItem.Icon == sprite)
                    {
                        exists = true;

                        break;
                    }

                if (exists) continue;

                var newPiece = CreateInstance<PieceData>();

                if (iconField != null) iconField.SetValue(newPiece, sprite);
                if (codeNameField != null) codeNameField.SetValue(newPiece, sprite.name);

                var newAssetPath = $"{PikachuConstants.BASE_RESOURCE_PATH}/PieceData/PIECEDATA_{sprite.name}.asset";
                newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

                AssetDatabase.CreateAsset(newPiece, newAssetPath);
                database.Add(newPiece);
                createdCount++;
            }

            if (createdCount > 0)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                this.needsSearchUpdate = true;
                Debug.Log($"[PikachuSystemEditor] Successfully bulk created {createdCount} PieceData from {relativePath}.");
            }
            else
            {
                Debug.Log($"[PikachuSystemEditor] No new PieceData created. Sprites might already exist.");
            }
        }

        #endregion
    }
}