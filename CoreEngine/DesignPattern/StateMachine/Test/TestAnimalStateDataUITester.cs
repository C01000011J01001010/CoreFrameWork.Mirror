using UnityEngine;
using UnityEngine.UI;
using CoreEngine.DesignPattern.StateMachine.Test;
using TMPro;

namespace CoreEngine.DesignPattern.StateMachine.Test
{
    public class TestAnimalStateDataUITester : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private TestAnimalActor targetActor;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private Button btnMakeHungry;
        [SerializeField] private Button btnSkipTimer;
        [SerializeField] private Button btnMakeFull;

        private void Start()
        {
            if (targetActor == null)
            {
                targetActor = FindFirstObjectByType<TestAnimalActor>();
            }

            if (targetActor == null)
            {
                Debug.LogError("[UITester] AnimalActor를 찾을 수 없습니다!");
                return;
            }

            btnMakeHungry.onClick.AddListener(() =>
            {
                if (targetActor != null)
                {
                    // Blackboard 내부 stats 경로로 접근
                    targetActor.StateController.AnimalBlackboard.stats.hunger = 100f;
                    Debug.Log("[UI] 배고픔 수치를 100으로 조작했습니다.");
                }
            });

            btnSkipTimer.onClick.AddListener(() =>
            {
                if (targetActor != null)
                {
                    // Blackboard 내부 공용 타이머 접근
                    targetActor.StateController.AnimalBlackboard.stateTimer = 10f;
                    Debug.Log("[UI] 타이머를 건너뛰었습니다.");
                }
            });

            btnMakeFull.onClick.AddListener(() =>
            {
                if (targetActor != null)
                {
                    targetActor.StateController.AnimalBlackboard.stats.hunger = 0f;
                    Debug.Log("[UI] 배고픔 수치를 0으로 조작했습니다.");
                }
            });
        }

        private void LateUpdate()
        {
            if (targetActor != null && targetActor.StateController != null)
            {
                var controller = targetActor.StateController;
                var board = controller.AnimalBlackboard;

                infoText.text =
                    $"Current State: {controller.CurrentStateType}\n" +
                    $"Hunger: {board.stats.hunger:F1}\n" +
                    $"Timer: {board.stateTimer:F1}";
            }
        }
    }
}