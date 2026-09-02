using CoreEngine.EventBus;
using System.Collections;
using UnityEngine;

namespace CoreEngine.Manager
{
    // 옵션 규약: 프레임워크가 시스템 제어에 반드시 필요로 하는 최소한의 공통 데이터
    public interface ICoreGraphicOption
    {
        int ResolutionWidth { get; }
        int ResolutionHeight { get; }
        bool IsFullScreen { get; }
        int TargetFrameRate { get; }
        int VSyncCount { get; }
        int QualityLevel { get; }
    }

    // 옵션 변경을 알리는 제네릭 이벤트 구조체 (가비지 할당 제로)
    public struct GraphicOptionChangedEvent<TOption> : IEvent where TOption : struct, ICoreGraphicOption
    {
        public TOption Option;
        public GraphicOptionChangedEvent(TOption option) => Option = option;
    }

    // 뼈대가 되는 BaseOptionManager (제네릭을 통한 완벽한 OCP 준수)
    public abstract class BaseOptionManager<TOption> : BaseManager where TOption : struct, ICoreGraphicOption
    {
        public TOption AppliedOption { get; private set; }

        public override IEnumerator Initialize()
        {
            // BaseManager의 상향식 등록 (EventBus 자동 구독)[cite: 14]
            yield return base.Initialize();

            // 자식 클래스에서 세이브 데이터를 읽어오도록 위임
            TOption savedOption = LoadSavedOption();
            ApplyGraphicSetting(savedOption);

            Debug.Log($"[{this.GetType().Name}] 그래픽 옵션 매니저 초기화 완료");
        }

        public void ApplyGraphicSetting(TOption newOption)
        {
            AppliedOption = newOption;

            // [보편성] 1. 유니티 네이티브 시스템 설정 (절대 변하지 않는 진리의 영역)
            QualitySettings.SetQualityLevel(newOption.QualityLevel);
            Screen.SetResolution(newOption.ResolutionWidth, newOption.ResolutionHeight, newOption.IsFullScreen);
            Application.targetFrameRate = newOption.TargetFrameRate;
            QualitySettings.vSyncCount = newOption.VSyncCount;

            // [특수성] 2. 템플릿 메서드 호출 (프로젝트별 고유 렌더링/후처리 로직은 자식에게 위임)[cite: 2, 19]
            ApplyProjectSpecificOptions(newOption);

            // 3. 결합도 0의 EventBus를 통한 변경 사항 브로드캐스트[cite: 1, 3]
            EventBus<GraphicOptionChangedEvent<TOption>>.Publish(new GraphicOptionChangedEvent<TOption>(newOption));
        }

        // 자식 클래스에서 반드시 구현해야 할 세이브 데이터 로드 로직
        protected abstract TOption LoadSavedOption();

        // 자식 클래스에서 오버라이드하여 프로젝트 고유의 렌더링(URP/HDRP Volume 등)을 적용할 메서드
        protected abstract void ApplyProjectSpecificOptions(TOption option);
    }
}