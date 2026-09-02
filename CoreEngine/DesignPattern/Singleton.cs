using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CoreEngine.Helpers;

namespace CoreEngine.DesignPattern.Singleton
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
                    LogHelper.LogWarningDontInstance<T>();
                }

                return _inst;
            }
        }

        protected virtual void Awake()
        {
            var asT = this as T;

            // 이미 싱글톤이 존재하는데, 나와 다른 객체인 경우 (확장 씬 병합 시점)
            if (_inst != null && _inst != this)
            {
                Type currentType = _inst.GetType();
                Type myType = this.GetType();

                // 내가 기존 인스턴스를 상속받은 '확장(파생) 클래스'라면 기존 것을 덮어쓴다
                if (myType.IsSubclassOf(currentType))
                {
                    LogHelper.Log($"[Singleton] 확장 시스템 감지! 기존 {currentType.Name}을(를) 파괴하고 {myType.Name}로 교체합니다.");

                    // 몸통에 다른 객체가 붙어있을 수 있으니 GameObject는 살려두고 컴포넌트만 파괴
                    Destroy(_inst);

                    // 내가 새로운 전역 싱글톤의 주인이 됨
                    _inst = asT;
                    return;

                    // *******LoadingDirector는 로딩 도중 사라지면 큰 사고이니 sealed로 막아뒀음
                }
                // 반대로 이미 더 구체적인 확장 클래스가 자리를 잡고 있다면 나는 조용히 파괴
                else if (currentType.IsSubclassOf(myType))
                {
                    LogHelper.Log($"[Singleton] 확장 시스템 감지! 기존 {myType.Name}을(를) 파괴하고 {currentType.Name}을 유지합니다.");
                    Destroy(this);
                    return;
                }
            }

            // 그 외의 일반적인 싱글톤 검증 (최초 생성 or 완벽히 동일한 클래스의 중복 생성 방지)
            if (!DesignPatternHelper.TryMakeSingleton<T>(asT, ref _inst))
            {
                // 몸통(GameObject)은 살려두고 중복된 컴포넌트(this)만 파괴[cite: 1]
                LogHelper.LogWarningSingleTon(this);
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