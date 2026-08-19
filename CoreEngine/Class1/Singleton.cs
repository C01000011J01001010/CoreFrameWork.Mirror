using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine
{ 
    public class Singleton<T> : MonoBehaviour 
        where T : Singleton<T>
    {
        private static T _inst;

        public static T Inst
        {
            get
            {
                // 이미 찾아놓은 객체가 있다면 바로 반환
                if (_inst != null) return _inst;

                // 아직 Awake로 등록되지 않았을 경우, 씬에서 강제로 찾음 (Lazy Init)
                _inst = FindFirstObjectByType<T>();

                // 씬을 다 뒤졌는데도 없으면 그때 경고 발생
                if (_inst == null)
                {
                    UtilityLog.LogWarningDontInstance<T>();
                }

                return _inst;
            }
        }

        protected virtual void Awake()
        {
            var asT = this as T;
            if(!UtilityDesignPattern.TryMakeSingleton<T>(asT, ref _inst))
            {
                // 몸통에 다른 객체가 붙어있을 수 있으니 GameObject는 살려둠
                Destroy(this);
            }
        }

        protected virtual void OnDestroy()
        {
            // 씬이 종료되거나 내가 정상적으로 파괴될 때만 싱글톤 참조 해제
            if (_inst == this)
            {
                _inst = null;
            }
        }
    }
}
