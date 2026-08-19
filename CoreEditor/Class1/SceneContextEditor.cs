// 위치: Scripts/Core/Editor/SceneContextEditor.cs
using CoreEngine; // SceneContext가 있는 네임스페이스
using UnityEditor;
using UnityEngine;

namespace CoreEngine
{
    [CustomEditor(typeof(SceneContext), true)]
    public class SceneContextEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SceneContext context = (SceneContext)target;
            GameObject gameObject = context.gameObject;
            Transform transform = context.transform;
            if (!EditorApplication.isPlaying && gameObject.scene.IsValid())
            {
                // 부모 객체가 존재하는지 검사 -> 존재해서는 안됨
                if (transform.parent != null)
                {
                    string messgae = //$"<color=red>[Hierarchy Error]</color>" +
                        $"SceneContext는 반드시 씬의 최상단(Root)에 위치해야 합니다!" +
                        $"\n부모 객체 밖으로 꺼내주세요.";
                    EditorGUILayout.HelpBox(messgae, MessageType.Error);
                }

                // 자식 객체로 SceneTester를 잘 넣어놨는지 검사 -> 존재해야됨
                var tester = gameObject.GetComponentInChildren<CoreEngine.Test.TestDriver>(true);
                if (tester == null)
                {
                    string messgae = //$"<color=red>[Hierarchy Error]</color>" +
                        $"SceneContext의 자식객체에 씬에 SceneTester가 없습니다!" +
                        $"\n정상적인 단독 씬 테스트를 위해 반드시 하위에 추가해 주세요.";
                    EditorGUILayout.HelpBox(messgae, MessageType.Error);
                }
            }

            // 기존의 인스펙터 UI를 그대로 그려줌
            base.OnInspectorGUI();
        }
    }
}
