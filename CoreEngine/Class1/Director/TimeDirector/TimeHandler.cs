using System;

namespace CoreEngine.TimeSystem
{
    /// <summary>
    /// 시간에 영향을 받는 시계 채널
    /// </summary>
    public enum TimeChannel
    {
        World,    // 게임 세상 시간 (Pause, TimeScale, HitStop 적용)
        UI,       // UI 애니메이션 시간 (게임 일시정지에도 작동)
        Unscaled  // 현실 절대 시간 (네트워크, 핑, 시스템 측정용)
    }

    /// <summary>
    /// 발급된 타이머를 식별하고 취소/조작하기 위한 핸들 구조체
    /// </summary>
    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        public readonly long Id;
        public static readonly TimerHandle Invalid = new TimerHandle(-1);

        public TimerHandle(long id)
        {
            Id = id;
        }

        public bool IsValid => Id > 0;
        public bool Equals(TimerHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is TimerHandle other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(TimerHandle left, TimerHandle right) => left.Equals(right);
        public static bool operator !=(TimerHandle left, TimerHandle right) => !left.Equals(right);
    }

    /// <summary>
    /// Min-Heap 큐 내부에서 관리되는 타이머 노드
    /// </summary>
    internal class TimerTask : IComparable<TimerTask>
    {
        public TimerHandle Handle;
        public double TargetTime;   // 만료 예정 시각
        public float Duration;      // 주기
        public Action Callback;
        public Action<float> OnUpdateProgress; // (선택) 진행률 (0~1) 전달
        public TimeChannel Channel;
        public bool IsLoop;
        public bool IsCancelled;

        public int CompareTo(TimerTask other)
        {
            if (other == null) return 1;
            return TargetTime.CompareTo(other.TargetTime);
        }
    }
}