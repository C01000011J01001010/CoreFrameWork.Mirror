using FishNet.Connection;
using FishNet.Object;
using CoreEngine.EventBus;
using CoreEngine.Manager;
using UnityEngine;

namespace CoreEngine.Network
{
    public enum NetworkTickTarget
    {
        None,
        ServerOnly,
        ClientOnly,
        OwnerOnly,
        ServerAndClient
    }

    public abstract class BaseLeafNetworked : NetworkBehaviour
    {
        // 어느 Context 산하로 들어갈지 결정
        [SerializeField] protected ContextScope myScope;

        public void SetScope(ContextScope scope)
        {
            myScope = scope;
            OnSetScope(scope);
        }

        protected virtual void OnSetScope(ContextScope scope)
        {

        }

        /// <summary>
        /// 상속받는 쪽에서 결정하도록
        /// </summary>
        protected abstract NetworkTickTarget networkTickTarget { get;}

        private bool _isRegistered = false;

        // ==========================================
        // 1. 유니티 로컬 생명주기 제어 (로컬에서 껐다 킬 때)
        // ==========================================
        protected virtual void OnEnable()
        {
            // 객체가 네트워크 상에 완전히 스폰된 상태라면 등록 시도
            if (base.IsSpawned) TryRegisterTick();
        }

        protected virtual void OnDisable()
        {
            TryUnregisterTick();
        }

        // ==========================================
        // 2. FishNet 네트워크 생명주기 제어 (진입점)
        // ==========================================
        // 서버로서 스폰되었을 때
        public override void OnStartServer() => TryRegisterTick();
        public override void OnStopServer() => TryUnregisterTick();

        // 클라이언트로서 스폰되었을 때
        public override void OnStartClient() => TryRegisterTick();
        public override void OnStopClient() => TryUnregisterTick();

        // 런타임에 서버가 나에게 소유권을 주거나 빼앗을 때!
        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            // 소유권이 바뀌었으므로 기존 틱을 해제하고, 내 새로운 권한에 맞춰 재등록 시도
            TryUnregisterTick();
            TryRegisterTick();
        }

        // ==========================================
        // 3. 등록 / 해제 공통 로직
        // ==========================================
        private void TryRegisterTick()
        {
            if (_isRegistered) return; // 중복 등록 방지
            if (!EvaluateNetworkCondition()) return; // 권한이 없으면 스킵

            _isRegistered = true;

            // 우리가 완성해둔 인터페이스 기반의 상향식(Bottom-up) 등록 진행
            if (this is ITickable tickable)
                EventBus<R_TickEvent>.Publish(new R_TickEvent(tickable, tickable.TickGroup, true));

            if (this is ILateTickable lateTickable)
                EventBus<R_LateTickEvent>.Publish(new R_LateTickEvent(lateTickable, lateTickable.LateTickGroup, true));

            if (this is IFixedTickable fixedTickable)
                EventBus<R_FixedTickEvent>.Publish(new R_FixedTickEvent(fixedTickable, fixedTickable.FixedTickGroup, true));
        
        }

        private void TryUnregisterTick()
        {
            if (!_isRegistered) return;
            _isRegistered = false;

            if (this is ITickable tickable)
                EventBus<R_TickEvent>.Publish(new R_TickEvent(tickable, tickable.TickGroup, false));

            if (this is ILateTickable lateTickable)
                EventBus<R_LateTickEvent>.Publish(new R_LateTickEvent(lateTickable, lateTickable.LateTickGroup, false));

            if (this is IFixedTickable fixedTickable)
                EventBus<R_FixedTickEvent>.Publish(new R_FixedTickEvent(fixedTickable, fixedTickable.FixedTickGroup, false));
        }

        private bool EvaluateNetworkCondition()
        {
            return networkTickTarget switch
            {
                NetworkTickTarget.None => false,
                NetworkTickTarget.ServerOnly => base.IsServerInitialized,
                NetworkTickTarget.ClientOnly => base.IsClientInitialized,
                NetworkTickTarget.OwnerOnly => base.IsOwner,
                NetworkTickTarget.ServerAndClient => base.IsServerInitialized || base.IsClientInitialized,
                _ => false
            };
        }

#if UNITY_EDITOR
        // 유니티 에디터에서 값이 변경되거나, 씬에 배치될 때 자동 호출되는 함수
        protected override void OnValidate()
        {
            base.OnValidate();
            // 아직 스코프가 None(미지정) 상태일 때만 자동 추론을 작동시킵니다.
            if (myScope == ContextScope.None)
            {
                // 현재 이 스크립트가 물리적으로 배치된 씬의 이름을 확인합니다.
                // 하이라키에 올려두는 순간 즉시 판별됩니다.
                string mySceneName = gameObject.scene.name;

                if (mySceneName == Constants.SCENE_GlobalScene)
                {
                    myScope = ContextScope.Project;
                    // 에디터 인스펙터 창의 값을 강제로 갱신하고 저장 상태로 만듭니다.
                    UnityEditor.EditorUtility.SetDirty(this);
                }
                else if (!string.IsNullOrEmpty(mySceneName))
                {
                    // 글로벌 씬이 아닌 일반 씬에 배치되었다면 안전하게 Scene 소속으로 고정해 줍니다.
                    myScope = ContextScope.Scene;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}