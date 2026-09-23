using System;
using System.Collections;

namespace CoreEngine
{
    public interface IModule
    {
        // 일반적인 상태 조회용
        bool IsInit {get;}
        bool IsActive { get; }

        // Func<bool> 등에 메서드 그룹으로 전달하기 위한 상태 조회용
        bool GetIsInit();
        bool GetIsActive();

        // 활성화를 메서드로 제어
        void SetActive(bool active);

        // 모듈 초기화
        IEnumerator Initialize();

        // 종료 및 메모리 정리
        void Exit();
    }
}
