#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CoreEngine.LevelDesign.Editor
{
    [CustomPropertyDrawer(typeof(LayerOutlineSetting))]
    public class LayerOutlineSettingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            SerializedProperty isUseProp = property.FindPropertyRelative("isUse");
            SerializedProperty layerNameProp = property.FindPropertyRelative("layerName");

            // *요청 반영: 1번째 줄 (토글 + 레이어 이름 표시)
            Rect line1Rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect toggleRect = new Rect(line1Rect.x, line1Rect.y, 20, line1Rect.height);
            Rect labelRect = new Rect(line1Rect.x + 20, line1Rect.y, line1Rect.width - 20, line1Rect.height);

            isUseProp.boolValue = EditorGUI.Toggle(toggleRect, isUseProp.boolValue);

            // 이름은 RenderMask에 의해 자동 결정되므로 읽기 전용 라벨로 예쁘게 그립니다.
            EditorGUI.LabelField(labelRect, layerNameProp.stringValue, EditorStyles.boldLabel);

            // *요청 반영: isUse가 true일 때만 상세 옵션을 노출합니다.
            if (isUseProp.boolValue)
            {
                float h = EditorGUIUtility.singleLineHeight + 2;
                Rect colorRect = new Rect(position.x, position.y + h * 1, position.width, EditorGUIUtility.singleLineHeight);
                Rect thickRect = new Rect(position.x, position.y + h * 2, position.width, EditorGUIUtility.singleLineHeight);
                Rect depthRect = new Rect(position.x, position.y + h * 3, position.width, EditorGUIUtility.singleLineHeight);
                Rect overRect = new Rect(position.x, position.y + h * 4, position.width, EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(colorRect, property.FindPropertyRelative("outlineColor"), new GUIContent("Outline Color"));
                EditorGUI.PropertyField(thickRect, property.FindPropertyRelative("outlineThickness"), new GUIContent("Thickness (Px)"));
                EditorGUI.PropertyField(depthRect, property.FindPropertyRelative("depthThreshold"), new GUIContent("깊이 오차 허용(Threshold)"));
                EditorGUI.PropertyField(overRect, property.FindPropertyRelative("forceEdgeMask"), new GUIContent("강제 경계선 생성 (자르기)"));
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // *요청 반영: isUse에 따른 유동적인 높이 변화
            SerializedProperty isUseProp = property.FindPropertyRelative("isUse");
            if (isUseProp.boolValue)
            {
                return (EditorGUIUtility.singleLineHeight + 2) * 5;
            }

            return EditorGUIUtility.singleLineHeight; // false면 딱 한 줄(이름과 체크박스)만 차지
        }
    }
}
#endif