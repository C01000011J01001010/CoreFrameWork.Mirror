using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreEngine.GameData
{
    // 에셋 아이디의 공통 규격을 정의하는 인터페이스입니다
    public interface IAssetId<TAsset> where TAsset : Object
    {
        int Id { get; }
        TAsset Get();
    }

    // 제네릭을 사용하여 단 하나의 구조체로 모든 에셋 타입을 커버합니다
    // 유니티 2020.1 이상부터 제네릭 구조체의 인스펙터 직렬화를 완벽히 지원합니다
    [Serializable]
    public struct AssetId<TAsset> : IAssetId<TAsset>, IEquatable<AssetId<TAsset>>
        where TAsset : Object
    {
        [SerializeField]
        private int id;

        public int Id => id;

        // CSV 컨버터의 리플렉션 로직이 호출할 단일 정수 생성자입니다
        public AssetId(int id)
        {
            this.id = id;
        }

        public TAsset Get()
        {
            // ID가 0이거나 음수이면 에셋이 할당되지 않은 것으로 간주하고 즉시 null을 반환합니다
            if (id <= 0) return null;

            // 라우터를 통해 레지스트리의 동기 조회(GetAsset)를 호출합니다
            return AssetRouter<TAsset>.Get(id);
        }

        // 향후 로딩 씬에서 비동기로 미리 불러올 때 사용할 메서드입니다
        public Task<TAsset> LoadAsync()
        {
            if (id <= 0) return Task.FromResult<TAsset>(null);

            // 라우터를 통해 레지스트리의 비동기 로드(LoadAssetAsync)를 호출합니다
            return AssetRouter<TAsset>.LoadAsync(id);
        }

        // 정수형 데이터를 자연스럽게 구조체로 변환하기 위한 암시적 형변환입니다
        public static implicit operator AssetId<TAsset>(int id) => new AssetId<TAsset>(id);

        // 값 타입 구조체이므로 동등성 비교 최적화를 위해 IEquatable을 구현합니다
        public bool Equals(AssetId<TAsset> other) => id == other.id;

        public override bool Equals(object obj) => obj is AssetId<TAsset> other && Equals(other);

        public override int GetHashCode() => id.GetHashCode();

        public static bool operator ==(AssetId<TAsset> left, AssetId<TAsset> right) => left.Equals(right);

        public static bool operator !=(AssetId<TAsset> left, AssetId<TAsset> right) => !left.Equals(right);
    }
}