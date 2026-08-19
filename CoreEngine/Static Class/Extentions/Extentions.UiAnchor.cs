using UnityEngine;

namespace CoreEngine
{
    // Extentions.UiAnchor

    public enum AnchorPreset
    {
        TopLeft, TopCenter, TopRight, TopStretch,
        MiddleLeft, MiddleCenter, MiddleRight, MiddleStretch,
        BottomLeft, BottomCenter, BottomRight, BottomStretch,
        StretchLeft, StretchCenter, StretchRight, StretchAll
    }

    public static partial class Extensions
    {
        private struct Anchors
        {
            public Vector2 min;
            public Vector2 max;

            public Anchors(Vector2 min, Vector2 max)
            {
                this.min = min;
                this.max = max;
            }
        }

        // ===============================================================
        // [Public API] 외부로 열어두는 통합 확장 메서드 (3가지 선택지)
        // ===============================================================

        /// <summary>
        /// 1. 앵커만 변경 (유니티 기본 클릭)
        /// </summary>
        public static void SetAnchor(this RectTransform rt, AnchorPreset preset)
        {
            Anchors anchors = GetAnchorMinMax(preset);

            Vector2 offsetMin = rt.offsetMin;
            Vector2 offsetMax = rt.offsetMax;

            rt.anchorMin = anchors.min;
            rt.anchorMax = anchors.max;

            // Stretch 모드 오프셋 초기화
            switch (preset)
            {
                case AnchorPreset.TopStretch:
                case AnchorPreset.MiddleStretch:
                case AnchorPreset.BottomStretch:
                    offsetMin.x = 0; offsetMax.x = 0;
                    break;

                case AnchorPreset.StretchLeft:
                case AnchorPreset.StretchCenter:
                case AnchorPreset.StretchRight:
                    offsetMin.y = 0; offsetMax.y = 0;
                    break;

                case AnchorPreset.StretchAll:
                    offsetMin = Vector2.zero; offsetMax = Vector2.zero;
                    break;
            }

            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        /// <summary>
        /// 2. [Shift + 클릭] 앵커와 피벗을 동시에 변경
        /// </summary>
        public static void SetAnchorWithPivot(this RectTransform rt, AnchorPreset preset)
        {
            rt.SetAnchor(preset);
            SetPivotInternal(rt, preset);
        }

        /// <summary>
        /// 3. [Alt + 클릭 효과] 앵커를 설정하고 현재 위치를 부모 기준으로 스냅(0,0)
        /// </summary>
        public static void SetAnchorWithPosition(this RectTransform rt, AnchorPreset preset)
        {
            rt.SetAnchor(preset);
            SnapPositionInternal(rt);
        }

        /// <summary>
        /// 4. [Shift + Alt + 클릭] 앵커, 피벗 변경 후 위치까지 스냅 (실무 최다 사용)
        /// </summary>
        public static void SetAnchorPivotAndPosition(this RectTransform rt, AnchorPreset preset)
        {
            rt.SetAnchor(preset);
            SetPivotInternal(rt, preset);
            SnapPositionInternal(rt);
        }

        // ===============================================================
        // [Private Helpers] 외부 조작을 막고 내부에서만 안전하게 사용하는 함수들
        // ===============================================================

        private static void SetPivotInternal(RectTransform rt, AnchorPreset preset)
        {
            Anchors anchors = GetAnchorMinMax(preset);
            rt.pivot = (anchors.min + anchors.max) / 2f;
        }

        private static void SnapPositionInternal(RectTransform rt)
        {
            rt.anchoredPosition = Vector2.zero;
        }

        private static Anchors GetAnchorMinMax(AnchorPreset preset)
        {
            return preset switch
            {
                AnchorPreset.TopLeft => new Anchors(new Vector2(0, 1), new Vector2(0, 1)),
                AnchorPreset.TopCenter => new Anchors(new Vector2(0.5f, 1), new Vector2(0.5f, 1)),
                AnchorPreset.TopRight => new Anchors(new Vector2(1, 1), new Vector2(1, 1)),
                AnchorPreset.TopStretch => new Anchors(new Vector2(0, 1), new Vector2(1, 1)),

                AnchorPreset.MiddleLeft => new Anchors(new Vector2(0, 0.5f), new Vector2(0, 0.5f)),
                AnchorPreset.MiddleCenter => new Anchors(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)),
                AnchorPreset.MiddleRight => new Anchors(new Vector2(1, 0.5f), new Vector2(1, 0.5f)),
                AnchorPreset.MiddleStretch => new Anchors(new Vector2(0, 0.5f), new Vector2(1, 0.5f)),

                AnchorPreset.BottomLeft => new Anchors(new Vector2(0, 0), new Vector2(0, 0)),
                AnchorPreset.BottomCenter => new Anchors(new Vector2(0.5f, 0), new Vector2(0.5f, 0)),
                AnchorPreset.BottomRight => new Anchors(new Vector2(1, 0), new Vector2(1, 0)),
                AnchorPreset.BottomStretch => new Anchors(new Vector2(0, 0), new Vector2(1, 0)),

                AnchorPreset.StretchLeft => new Anchors(new Vector2(0, 0), new Vector2(0, 1)),
                AnchorPreset.StretchCenter => new Anchors(new Vector2(0.5f, 0), new Vector2(0.5f, 1)),
                AnchorPreset.StretchRight => new Anchors(new Vector2(1, 0), new Vector2(1, 1)),
                AnchorPreset.StretchAll => new Anchors(new Vector2(0, 0), new Vector2(1, 1)),

                _ => new Anchors(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f))
            };
        }
    }
}