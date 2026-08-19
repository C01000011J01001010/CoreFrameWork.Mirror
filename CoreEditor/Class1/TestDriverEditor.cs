using UnityEngine;
using UnityEditor;
using CoreEngine.Test;

namespace CoreEngine.Test.Editor
{
    [CustomEditor(typeof(TestDriver))]
    public class TestDriverEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 현재 인스펙터에 그려지고 있는 TestDriver 타겟 가져오기
            TestDriver driver = (TestDriver)target;

            if (driver != null)
            {
                // 이 GameObject에 붙어있는 모든 컴포넌트를 가져옴
                Component[] components = driver.GetComponents<Component>();

                // Transform(기본) + TestDriver(자신) = 기본적으로 2개여야 정상
                // 만약 길이가 2보다 크다면 다른 컴포넌트가 붙어있다는 뜻!
                if (components.Length > 2)
                {
                    // 인스펙터 상단에 눈에 띄는 경고창 띄우기
                    EditorGUILayout.HelpBox(
                        "⚠️ [주의] 이 GameObject에 TestDriver 외의 다른 컴포넌트가 감지되었습니다!\n\n" +
                        "TestDriver는 단독 씬 테스트 환경을 구축한 후, 혹은 일반 씬 흐름일 경우 " +
                        "게임 시작 직후(Awake/Start) 스스로 자폭(Destroy)합니다.\n\n" +
                        "따라서 여기에 함께 부착된 다른 컴포넌트들도 모두 강제 파괴되어 " +
                        "정상적으로 동작하지 않습니다. TestDriver는 반드시 단독 객체로 사용해 주세요.",
                        MessageType.Warning);

                    // 경고창 아래에 약간의 여백 추가
                    EditorGUILayout.Space(5);
                }
            }

            // TestDriver의 원래 인스펙터 UI(변수들)를 그대로 그려줌
            base.OnInspectorGUI();
        }
    }
}