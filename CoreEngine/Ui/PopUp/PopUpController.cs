using CoreEngine.EventBus;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CoreEngine.Extentions;

namespace CoreEngine.UI
{
    public class PopUpController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject popUpWindow;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;
        [SerializeField] private TextMeshProUGUI acceptButtonText; // '확인' 또는 '수락' 텍스트 변경용

        public string ConfirmMessage;
        public string AcceptMessage;

        // 팝업 대기열 (LIFO)
        private Stack<SpawnPopUpEvent> _popUpStack = new Stack<SpawnPopUpEvent>();

        // 동일 메시지 중복 방지용 해시셋
        private HashSet<int> _activeMessageHashes = new HashSet<int>();

        private SpawnPopUpEvent _currentPopUp;

        private RectTransform _popUpWinodwRect;

        private void Awake()
        {
            if (string.IsNullOrEmpty(ConfirmMessage)) ConfirmMessage = "Confirm";
            if (string.IsNullOrEmpty(AcceptMessage)) AcceptMessage = "Accept";
            acceptButton.onClick.AddListener(OnAcceptClicked);
            rejectButton.onClick.AddListener(OnRejectClicked);
            _popUpWinodwRect = popUpWindow.GetComponent<RectTransform>();
            popUpWindow.SetActive(false);
        }

        private void OnEnable() => EventBus<SpawnPopUpEvent>.Subscribe(EnqueuePopUp);
        private void OnDisable() => EventBus<SpawnPopUpEvent>.Unsubscribe(EnqueuePopUp);

        private void EnqueuePopUp(SpawnPopUpEvent evt)
        {
            int msgHash = evt.Message.GetHashCode();

            // 해시 검사: 이미 스택에 있거나 화면에 떠 있는 동일 메시지면 무시
            if (!_activeMessageHashes.Add(msgHash)) return;

            _popUpStack.Push(evt);

            // 현재 떠 있는 팝업이 없다면 즉시 출력
            if (!popUpWindow.activeSelf)
            {
                ShowNext();
            }
        }

        private void ShowNext()
        {
            // 스택을 다 비웠을때만 꺼짐
            if (_popUpStack.Count == 0)
            {
                popUpWindow.SetActive(false);
                return;
            }

            _currentPopUp = _popUpStack.Pop();
            messageText.text = _currentPopUp.Message;

            if (_currentPopUp.PopUpType == PopUpType.Confirm)
            {
                rejectButton.gameObject.SetActive(false);
                acceptButtonText.text = ConfirmMessage;
            }
            else
            {
                rejectButton.gameObject.SetActive(true);
                acceptButtonText.text = AcceptMessage;
            }

            // PopUp창을 중앙으로 이동시킨 후 활성화
            _popUpWinodwRect.SetAnchorPivotAndPosition(AnchorPreset.MiddleCenter);
            popUpWindow.SetActive(true);
        }

        private void OnAcceptClicked()
        {
            // 타겟 객체가 파괴되지 않았는지 확인 후 Action 실행 (Fake Null 방어)
            if (_currentPopUp.OnConfirm?.Target != null || _currentPopUp.OnConfirm?.Method.IsStatic == true)
            {
                _currentPopUp.OnConfirm.Invoke();
            }

            CloseCurrentAndShowNext();
        }

        private void OnRejectClicked()
        {
            CloseCurrentAndShowNext();
        }

        private void CloseCurrentAndShowNext()
        {
            // 처리된 메세지를 해시에서 지움으로써 완전히 해소
            int msgHash = _currentPopUp.Message.GetHashCode();
            _activeMessageHashes.Remove(msgHash);

            // 다음 메세지 실행, 스택이 비었으면 종료
            ShowNext();
        }
    }
}