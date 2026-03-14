namespace CahtFramework.Pikachu.Editor
{
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    public static class PieceDataBulkImporter
    {
        public static void BulkCreatePiecesFromFolder()
        {
            var dbPath   = $"{PikachuConstants.BASE_RESOURCE_PATH}/Database/PieceDataDatabase.asset";
            var database = AssetDatabase.LoadAssetAtPath<IODatabase>(dbPath);

            if (database == null)
            {
                Debug.LogError($"[PieceDataImporter] Cannot find PieceDataDatabase at {dbPath}. Please open Pikachu System Window to initialize it first.");

                return;
            }

            var absolutePath = EditorUtility.OpenFolderPanel("Select Sprite Folder", "Assets", "");

            if (string.IsNullOrEmpty(absolutePath)) return;

            if (!absolutePath.StartsWith(Application.dataPath))
            {
                Debug.LogError("[PieceDataImporter] Please select a folder inside the Assets directory.");

                return;
            }

            var relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
            var guids        = AssetDatabase.FindAssets("t:Sprite", new[] { relativePath });

            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Texture2D", new[] { relativePath });

            if (guids.Length == 0)
            {
                Debug.LogWarning($"[PieceDataImporter] No Sprites or Textures found in {relativePath}. Make sure your images are imported as Sprite (2D and UI).");

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

                var newPiece = ScriptableObject.CreateInstance<PieceData>();

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
                Debug.Log($"[PieceDataImporter] Successfully bulk created {createdCount} PieceData from {relativePath}. Open Pikachu System Window to verify.");
            }
            else
            {
                Debug.Log($"[PieceDataImporter] No new PieceData created. The sprites might already exist in the database.");
            }
        }
    }
}