using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(ButtonCustomBase), true)]
public class ButtonCustomBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Lấy reference
        SerializedProperty button = serializedObject.FindProperty("button");
        SerializedProperty distanceOnClick = serializedObject.FindProperty("distanceOnClick");
        SerializedProperty interactable = serializedObject.FindProperty("interactable");
        SerializedProperty buttonType = serializedObject.FindProperty("buttonType");
        SerializedProperty colorButtonSettings = serializedObject.FindProperty("colorButtonSettings");

        // --- Hiển thị các field cơ bản ---
        EditorGUILayout.PropertyField(button);
        EditorGUILayout.PropertyField(distanceOnClick);
        EditorGUILayout.PropertyField(interactable);
        EditorGUILayout.PropertyField(buttonType);

        // --- Hiện phần tùy chỉnh theo loại Button ---
        EditorGUILayout.Space(10);
        EButtonType type = (EButtonType)buttonType.enumValueIndex;

        switch (type)
        {
            case EButtonType.None:
                EditorGUILayout.HelpBox("No specific settings for this button type.", MessageType.Info);
                break;

            case EButtonType.ColorButton:
                EditorGUILayout.LabelField("Color Button Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(colorButtonSettings.FindPropertyRelative("normalColor"));
                EditorGUILayout.PropertyField(colorButtonSettings.FindPropertyRelative("pressedColor"));
                EditorGUILayout.PropertyField(colorButtonSettings.FindPropertyRelative("fadeDuration"));
                break;

            default:
                EditorGUILayout.HelpBox("Unsupported button type.", MessageType.Warning);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
