using CoreEngine.Facades;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreEngine.UI
{
    public abstract class BaseUiManager<TUiEnum> : BaseManager
        where TUiEnum : Enum
    {
        [SerializeField, Tooltip("Scene 초기화가 완료될 시 활성화할 UI 객체들")]
        private TUiEnum[] OpenUiOnSceneLoaded;

        private readonly AddressableUiHandler _addressableUiHandler = new();

        public override IEnumerator Initialize()
        {
            yield return base.Initialize();
            _addressableUiHandler.Initialize();
            foreach (var uiType in OpenUiOnSceneLoaded)
            {
                _ = Show(uiType);
                yield return null;
            }
        }

        public override void Exit()
        {
            _addressableUiHandler.ReleaseAll();
            base.Exit();
        }

        /// <summary>
        /// 지정된 UI를 표시한다.
        /// </summary>
        public async Task<bool> Show(TUiEnum uiEnum)
        {
            if (!TryGetValidUiType(in uiEnum, out var uiType)) return false;

            // 현재 Scene / Project에 이미 존재하는 UI 검색
            IUi ui = CoreFacade.GetUi(uiType) as IUi;

            // 존재하지 않으면 Addressable을 통해 확보
            if (ui == null)
            {
                ui = await _addressableUiHandler.Load(uiType);

                if (ui == null) return false;
            }

            // UI 표시
            ui.Show();
            _addressableUiHandler.OnShow(uiType);

            return true;
        }

        /// <summary>
        /// 지정된 UI를 숨긴다.
        /// </summary>
        public bool Hide(TUiEnum uiEnum)
        {
            if (!TryGetValidUiType(in uiEnum, out var uiType)) return false;

            IUi ui = CoreFacade.GetUi(uiType) as IUi;

            if (ui == null) return false;

            // UI 자체의 Hide 처리
            ui.Hide();
            _addressableUiHandler.OnHide(uiType);

            return true;
        }

        /// <summary>
        /// TUiType에 대응하는 실제 UI 타입을 반환한다.
        /// </summary>
        protected abstract Type GetConcreteUiType(TUiEnum uiEnum);

        public virtual bool TryGetValidUiType(in TUiEnum uiEnum, out Type uiType)
        {
            uiType = GetConcreteUiType(uiEnum);

            if (uiType == null || !typeof(IUi).IsAssignableFrom(uiType))
            {
                uiType = null;
                return false;
            }

            return true;
        }
    }
}