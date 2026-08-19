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
    // CoreFacade.Get
    public static partial class CoreFacade
    {

        #region 📦 Module (Manager & UI) 조회 API

        /// <summary>
        /// 시스템 논리를 담당하는 매니저 모듈을 조회합니다.
        /// SceneContext의 ManagerHub를 먼저 검색하고, 없으면 ProjectContext의 ManagerHub를 검색합니다.
        /// </summary>
        public static T GetManager<T>() where T : class, IModule
        {
            // 1. 현재 콘텐츠 씬(SceneContext)의 매니저 허브에서 검색 (우선순위 높음)
            if (Scene != null && Scene.managerHub != null)
            {
                T manager = Scene.managerHub.GetModule<T>();
                if (manager != null) return manager;
            }

            // 2. 씬에 없다면 전역 환경(ProjectContext)의 매니저 허브에서 검색 (우선순위 낮음)
            if (Project != null && Project.managerHub != null)
            {
                T manager = Project.managerHub.GetModule<T>();
                if (manager != null) return manager;
            }

            Debug.LogWarning($"[CoreFacade] 요청하신 매니저 '{typeof(T).Name}'를 Scene과 Project 어디에서도 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 화면 연출 및 뷰를 담당하는 UI 모듈을 조회합니다.
        /// SceneContext의 UiHub를 먼저 검색하고, 없으면 ProjectContext의 UiHub를 검색합니다.
        /// </summary>
        public static T GetUi<T>() where T : class, IModule
        {
            // 1. 현재 콘텐츠 씬(SceneContext)의 UI 허브에서 검색 (우선순위 높음)
            if (Scene != null && Scene.uiHub != null)
            {
                T ui = Scene.uiHub.GetModule<T>();
                if (ui != null) return ui;
            }

            // 2. 씬에 없다면 전역 환경(ProjectContext)의 UI 허브에서 검색 (우선순위 낮음)
            if (Project != null && Project.uiHub != null)
            {
                T ui = Project.uiHub.GetModule<T>();
                if (ui != null) return ui;
            }

            Debug.LogWarning($"[CoreFacade] 요청하신 UI 모듈 '{typeof(T).Name}'를 Scene과 Project 어디에서도 찾을 수 없습니다.");
            return null;
        }

        #endregion

        #region 🪽 인게임 Actor 조회 API (책임 연쇄 패턴 적용)

        /// <summary>
        /// 특정 인터페이스나 클래스 타입을 구현한 인게임 액터 중 '첫 번째 요소(단일)'를 안전하게 조회합니다.
        /// </summary>
        public static T GetActor<T>() where T : class, IActor
        {
            // 1. 인게임 액터는 주로 씬 단위로 존재하므로 SceneContext의 ActorHub를 먼저 탐색합니다.
            if (Scene != null && Scene.actorHub != null)
            {
                T actor = Scene.actorHub.GetActor<T>();
                if (actor != null) return actor;
            }

            // 2. 만약 글로벌 관람 카메라나 전역 로비 엔티티 등 전역 액터일 경우를 대비해 ProjectContext도 백업 탐색합니다.
            if (Project != null && Project.actorHub != null)
            {
                T actor = Project.actorHub.GetActor<T>();
                if (actor != null) return actor;
            }

            return null;
        }

        /// <summary>
        /// 특정 인터페이스나 타입을 공유하는 '모든 활성 액터 목록'을 안전하게 가져옵니다.
        /// 내부 컬렉션 순회 중 발생할 수 있는 오염을 방지하기 위해, ToArray() 스냅샷을 뜬 복사본 목록을 반환합니다.
        /// </summary>
        public static IEnumerable<T> GetActors<T>() where T : class, IActor
        {
            // 해시셋 결합 과정에서 중복을 원천 차단하기 위해 HashSet 생성
            HashSet<T> combinedActors = new HashSet<T>();

            // 1. SceneContext 소속 액터들 수집
            if (Scene != null && Scene.actorHub != null)
            {
                var sceneActors = Scene.actorHub.GetActors<T>();
                if (sceneActors != null)
                {
                    foreach (var actor in sceneActors)
                    {
                        combinedActors.Add(actor);
                    }
                }
            }

            // 2. ProjectContext 소속 액터들 수집
            if (Project != null && Project.actorHub != null)
            {
                var projectActors = Project.actorHub.GetActors<T>();
                if (projectActors != null)
                {
                    foreach (var actor in projectActors)
                    {
                        combinedActors.Add(actor);
                    }
                }
            }

            // 지연 실행(Lazy Evaluation) 도중 컬렉션이 Add/Remove 되어 터지는 컬렉션 오염 에러를 막기 위해
            // 이 시점의 완벽한 붕어빵 사진(ToArray)을 구워 리턴합니다.
            return combinedActors.ToArray();
        }

        #endregion

        public static Vector3Int GetGridKey(Vector3 worldPos)
        {
            return spatialCullingManager?.GetGridKey(worldPos) ?? Vector3Int.zero;
        }
    }
}