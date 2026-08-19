using System;
using System.Collections;

namespace CoreEngine
{
    public interface IModule
    {
        // 모듈 활성화 여부
        bool IsActive { get; }
        void SetActive(bool active);

        #region 모듈 생명주기

        IEnumerator Initialize();

        //필요에 따라 인터페이스로 추가
        //IEnumerator LateInitialize();

        // 종료 및 메모리 정리
        void Exit();


        #endregion
    }
}
