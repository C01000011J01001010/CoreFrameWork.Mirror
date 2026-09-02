using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CoreEngine.Data;

namespace CoreEngine.Manager
{
    /// <summary>
    /// 하나의 정적 데이터(Table)를 관리하는 매니저의 기반구조
    /// </summary>
    public abstract class BaseStaticDataManager<TDataObject> : BaseManager
        where TDataObject : BaseData
    {
        // 🌟 Addressable 에셋의 고유 주소 (자식 클래스에서 명시)
        protected abstract string CatalogAddress { get; }

        // 아이템 ID를 키(Key)로 사용하여 빠르게 검색하기 위한 딕셔너리
        protected Dictionary<int, TDataObject> database = new Dictionary<int, TDataObject>();

        public override IEnumerator Initialize()
        {
            // 상향식 자동 등록 및 부모 초기화
            yield return base.Initialize();

            bool isLoadComplete = false;

            // 1. 방금 구축한 순수 리소스 매니저를 통해 정적 DB 에셋 비동기 로드
            // (게임 종료 시까지 유지되어야 하므로 GlobalAsset으로 로드)
            ResourceManager.Inst.LoadGlobalAssetAsync<ScriptableObject>(CatalogAddress, (loadedAsset) =>
            {
                OnLoadedDataBase(loadedAsset);
                isLoadComplete = true;
            });

            // 2. 비동기 로딩이 완전히 끝날 때까지 다음 초기화 단계를 안전하게 블로킹(대기)
            yield return new WaitUntil(() => isLoadComplete);
            
            Debug.Log($"[{this.GetType().Name}] 데이터베이스 초기화 완료. 총 {database.Count}개 레코드.");
        }

        public override void Exit()
        {
            // 실제 에셋의 메모리 해제는 ResourceManager가 전담하므로 (ReleaseGlobalAssets),
            // 여기서는 자료구조(전화번호부)만 깔끔하게 비워줍니다.
            database.Clear();
            base.Exit();
        }

        /// <summary>
        /// 로드된 ScriptableObject 에셋을 파싱하여 database 딕셔너리에 매핑하는 추상 메서드
        /// </summary>
        protected abstract void OnLoadedDataBase(ScriptableObject loadedAsset);

        /// <summary>
        /// ID를 통해 DB의 특정 레코드(데이터)를 가져옴 (O(1) 탐색)
        /// </summary>
        public TDataObject GetRecord(int id)
        {
            if (database.TryGetValue(id, out TDataObject record))
            {
                return record;
            }

            Debug.LogWarning($"[{this.GetType().Name}] 해당 ID의 레코드(데이터)를 찾을 수 없음: {id}");
            return null;
        }
    }
}