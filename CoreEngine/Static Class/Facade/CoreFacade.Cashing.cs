using System;
using System.Collections.Generic;
using System.Linq;
using CoreEngine.Hub;
using CoreEngine.Manager.Culling;
using UnityEngine;

namespace CoreEngine
{
    /// <summary>
    /// 이카루스 프레임워크의 최상위 창구(Facade) 클래스입니다.
    /// 외부 Leaf(3계층) 및 콘텐츠 로직은 오직 이 파사드를 통해서만 안전하게 다른 모듈과 액터를 찾습니다.
    /// </summary>
    // CoreFacade.Cashing.
    public static partial class CoreFacade
    {
        // 1. 최상위 1계층 Context 캐싱 (싱글톤 레퍼런스 연결)
        // internal로 제한하여 외부 3계층 스크립트가 Context를 직접 조작하는 계층 위반을 원천 차단합니다.
        #region Context
        private static ProjectContext Project => ProjectContext.Inst;
        private static SceneContext Scene => SceneContext.Inst;
        #endregion

        #region SpatialCulling
        private static SpatialCullingManager _spatialCullingManager;
        // 로그 스팸을 막기 위한 플래그 변수 추가
        private static bool _isSpatialCullingErrorLogged = false;
        private static SpatialCullingManager spatialCullingManager
        {
            get
            {
                if (_spatialCullingManager == null)
                {
                    _spatialCullingManager = GetManager<SpatialCullingManager>();

                    // 여전히 null이라면 (못 찾았다면)
                    if (_spatialCullingManager == null)
                    {
                        // 에러 로그가 한 번도 출력된 적 없을 때만 출력!
                        if (!_isSpatialCullingErrorLogged)
                        {
                            UtilityLog.Log("[CoreFacade] 현재 씬이나 프로젝트 환경에 'SpatialCullingManager'가 등록되지 않았습니다! 기본값(Zero)을 반환합니다.", LogColor.Red);
                            _isSpatialCullingErrorLogged = true; // 플래그 잠금
                        }
                        return null;
                    }
                    else
                    {
                        // 매니저를 성공적으로 찾았다면 다시 플래그 초기화 (씬 전환 시 대비)
                        _isSpatialCullingErrorLogged = false;
                    }
                }
                return _spatialCullingManager;
            }
        }
        #endregion
    }
}