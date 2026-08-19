using CoreEngine;
using CoreEngine.EventBus;
using UnityEngine;
using UnityEngine.UI;

public struct CloseButtonClickEvent : IEvent
{
    public IUi closedUi;
    public CloseButtonClickEvent(IUi closedUi) => this.closedUi = closedUi;
}

public class CloseButton : MonoBehaviour
{
    protected Button button {  get; private set; }

    private void Awake()
    {
        button = GetComponent<Button>();
        
    }
    private void OnEnable()
    {
        button.onClick.AddListener(ButtonOnClick);
    }
    private void OnDisable()
    {
        button.onClick.RemoveListener(ButtonOnClick);
    }

    public void ButtonOnClick()
    {
        IUi ui = GetComponentInParent<IUi>();

        if (ui != null)
        {
            ui.SetActive(false);
            EventBus<CloseButtonClickEvent>.Publish(new CloseButtonClickEvent(ui));
        }
        else
        {
            Debug.Log("부모 계층에 IUi가 없습니다.");
        }
    }
}
