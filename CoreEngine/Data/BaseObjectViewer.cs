using UnityEngine;

namespace CoreEngine.Data
{
    /// <summary>
    /// View 역할의 객체에 사용 (ui를 포함, 게임에 등장하는 오브젝트도 가능)
    /// </summary>
    /// <typeparam name="ViewTarget"></typeparam>
    public abstract class BaseObjectViewer<ViewTarget> : MonoBehaviour where ViewTarget : class
    {
        private ViewTarget connectObject = null;
        public ViewTarget ConnectObject => connectObject;

        public ViewTarget GetConnectedObject() => connectObject;

        protected virtual void OnConnected() { UpdateView();}
        public bool Connect(ViewTarget target) 
        {
            //연결이 이미 되었다면 끊고 진행!
            if (connectObject != null) Disconnect();

            //연결 대상이 없으면 비우고 끝
            if (target == null) return false;

            //만약 연결이 가능하다면
            connectObject = target; // 연결 후
            OnConnected(); // 연결됐을때 처리
            UpdateView(); // 마지막으로 view를 최신상태로 갱신
            return true;
        }

        protected virtual void OnDisconnected(ViewTarget target) { UpdateView(); }

        public void Disconnect() 
        {
            //연결이 안되어있을 때
            if (connectObject == null) 
            {
                OnDisconnected(null);
                return;
            }
            OnDisconnected(connectObject);
            connectObject = null;
        }

        public abstract void UpdateView();

        /// <summary>
        /// 연결된 대상이 없는지 체크
        /// </summary>
        /// <returns></returns>
        protected bool IsEmpty()
        {
            if (ConnectObject == null)
            {
                Debug.LogError($"{gameObject.name}({GetType()})에 연결된 객체 없음");
                return true;
            }
            return false;
        }
    }
}
