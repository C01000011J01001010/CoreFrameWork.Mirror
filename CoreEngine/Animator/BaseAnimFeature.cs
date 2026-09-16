using UnityEngine;
using CoreEngine.Actor;
using CoreEngine.Helpers;

namespace CoreEngine.Animation
{
    public abstract class BaseAnimFeature : BaseActorFeature
    {
        [SerializeField, Tooltip("데이터를 할당하지 않으면 BaseAnimFeature.OnValidate호출시 자신 포함 자식 객체에서 자동추적")]
        private Animator _animator;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            if(!Host.TryGetComponent(out _animator))
            {
                LogHelper.LogError($"{Host.gameObject.name}에 {nameof(Animator)} 컴포넌트가 없음");
            }
            GetAnimPrarmHash();
        }

        protected abstract void GetAnimPrarmHash();

        protected void SetParam(int ParamHash, bool value) => _animator.SetBool(ParamHash, value);
        protected void SetParam(int ParamHash, float value) => _animator.SetFloat(ParamHash, value);
        protected void SetParam(int ParamHash, int value) => _animator.SetInteger(ParamHash, value);
        protected void SetParam(int ParamHash) => _animator.SetTrigger(ParamHash);

        public void OnValidate()
        {
            if(_animator == null)
            {
                _animator = Host.GetComponentInChildren<Animator>();
            }
        }
    }
}

