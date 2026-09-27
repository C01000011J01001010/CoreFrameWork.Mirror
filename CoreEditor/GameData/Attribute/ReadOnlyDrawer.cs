#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CoreEngine.GameData;

namespace CoreEditor.GameData
{
    // ReadOnlyAttribute가 붙은 변수를 인스펙터에 그리는 방식을 재정의
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        // 접힘/펼침 상태에 따른 높이를 동적으로 계산하여 반환
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            // includeChildren 매개변수(마지막 true)를 통해 하위 요소까지 그람
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif