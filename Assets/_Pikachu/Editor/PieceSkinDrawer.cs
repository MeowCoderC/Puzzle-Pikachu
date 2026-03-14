namespace CahtFramework.Pikachu.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(ThemeData.PieceSkin))]
    public class PieceSkinDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 44f; }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var pieceDataProp  = property.FindPropertyRelative("PieceData");
            var skinSpriteProp = property.FindPropertyRelative("SkinSprite");

            var iconRect   = new Rect(position.x, position.y + 2f, 40f, 40f);
            var dataRect   = new Rect(position.x + 45f, position.y + 2f, position.width - 45f, 18f);
            var spriteRect = new Rect(position.x + 45f, position.y + 24f, position.width - 45f, 18f);

            var previewSprite = skinSpriteProp.objectReferenceValue as Sprite;

            if (previewSprite == null && pieceDataProp.objectReferenceValue != null)
            {
                var pd                        = pieceDataProp.objectReferenceValue as PieceData;
                if (pd != null) previewSprite = pd.Icon;
            }

            if (previewSprite != null)
            {
                var previewTex = AssetPreview.GetAssetPreview(previewSprite);
                if (previewTex != null) GUI.DrawTexture(iconRect, previewTex, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Box(iconRect, "None");
            }

            GUI.enabled = false;
            EditorGUI.PropertyField(dataRect, pieceDataProp, GUIContent.none);
            GUI.enabled = true;

            EditorGUI.PropertyField(spriteRect, skinSpriteProp, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}