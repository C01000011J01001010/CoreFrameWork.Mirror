using System;
using System.Collections.Generic;
using UnityEngine;
using CoreEngine.Director; // BaseDirector 상속
using CoreEngine.EventBus;
using CoreEngine.Utility;

namespace CoreEngine.TimeSystem
{
    public readonly struct WorldTimeScaleChangedEvent : IEvent
    {
        public readonly float NewScale;
        public WorldTimeScaleChangedEvent(float newScale) => NewScale = newScale;
    }

    public readonly struct WorldPauseStateChangedEvent : IEvent
    {
        public readonly bool IsPaused;
        public WorldPauseStateChangedEvent(bool isPaused) => IsPaused = isPaused;
    }

    [DefaultExecutionOrder((int)ExecutionOrder.TimeDirector)]
    public sealed class TimeDirector : BaseDirector<TimeDirector>, ITickable
    {
        // --- 시계 채널 변수 ---
        public float WorldTimeScale { get; private set; } = 1.0f;
        public float UITimeScale { get; private set; } = 1.0f;

        public bool IsWorldPaused { get; private set; } = false;
        public bool IsUIPaused { get; private set; } = false;

        // --- 누적 가상 시간 ---
        public double TotalWorldTime { get; private set; }
        public double TotalUITime { get; private set; }
        public double TotalUnscaledTime { get; private set; }

        // --- Delta Time 프로퍼티 ---
        public float WorldDeltaTime => IsWorldPaused ? 0f : Time.unscaledDeltaTime * WorldTimeScale;
        public float UIDeltaTime => IsUIPaused ? 0f : Time.unscaledDeltaTime * UITimeScale;
        public float UnscaledDeltaTime => Time.unscaledDeltaTime;

        // ITickable 구현: 프레임에서 가장 먼저 시간을 측정함
        public TickGroup TickGroup => TickGroup.Initial;

        // --- 타이머 엔진 ---
        private readonly MinHeapTimerQueue _timerQueue = new MinHeapTimerQueue();
        private readonly Dictionary<long, TimerTask> _timerMap = new Dictionary<long, TimerTask>(128);
        private long _nextTimerId = 1;

        // --- HitStop 연출 변수 ---
        private bool _isHitStopping = false;
        private float _hitStopTimer = 0f;
        private float _preHitStopScale = 1.0f;

        private void OnEnable()
        {
            this.RegisterTick();
        }
        private void OnDisable()
        {
            this.UnregisterTick();
        }

        protected override  void Awake()
        {
             base.Awake();
            _timerQueue.Clear();
            _timerMap.Clear();
            _nextTimerId = 1;

            TotalWorldTime = 0;
            TotalUITime = 0;
            TotalUnscaledTime = 0;

            WorldTimeScale = 1.0f;
            UITimeScale = 1.0f;
            IsWorldPaused = false;
            IsUIPaused = false;
        }

        // --- UpdateDirector에 의해 매 프레임 스케줄링 호출 ---
        public void Tick(float unscaledDeltaTime)
        {
            // 1. 시간 축 갱신
            float worldDelta = IsWorldPaused ? 0f : unscaledDeltaTime * WorldTimeScale;
            float uiDelta = IsUIPaused ? 0f : unscaledDeltaTime * UITimeScale;

            TotalWorldTime += worldDelta;
            TotalUITime += uiDelta;
            TotalUnscaledTime += unscaledDeltaTime;

            // 2. HitStop 타이머 처리
            ProcessHitStop(unscaledDeltaTime);

            // 3. 타이머 스케줄러 처리 (Min-Heap 기반 $O(1)$ 검사)
            ProcessTimers();
        }

        #region [Timer Engine API]

        /// <summary>
        /// 고성능 타이머를 등록합니다.
        /// </summary>
        public TimerHandle RegisterTimer(float duration, Action callback, TimeChannel channel = TimeChannel.World, bool isLoop = false, Action<float> onProgress = null)
        {
            if (duration <= 0f)
            {
                callback?.Invoke();
                return TimerHandle.Invalid;
            }

            long id = _nextTimerId++;
            TimerHandle handle = new TimerHandle(id);

            double currentChannelTime = GetCurrentTime(channel);
            TimerTask task = new TimerTask
            {
                Handle = handle,
                Duration = duration,
                TargetTime = currentChannelTime + duration,
                Callback = callback,
                OnUpdateProgress = onProgress,
                Channel = channel,
                IsLoop = isLoop,
                IsCancelled = false
            };

            _timerMap[id] = task;
            _timerQueue.Push(task);

            return handle;
        }

        /// <summary>
        /// 진행 중인 타이머를 취소합니다.
        /// </summary>
        public bool CancelTimer(TimerHandle handle)
        {
            if (!handle.IsValid) return false;

            if (_timerMap.TryGetValue(handle.Id, out TimerTask task))
            {
                task.IsCancelled = true; // 지연 삭제 플래그 (힙 재정렬 비용 방지)
                _timerMap.Remove(handle.Id);
                return true;
            }

            return false;
        }

        private void ProcessTimers()
        {
            while (_timerQueue.Count > 0)
            {
                TimerTask top = _timerQueue.Peek();

                // 취소된 타이머는 버림
                if (top.IsCancelled)
                {
                    _timerQueue.Pop();
                    continue;
                }

                double currentTime = GetCurrentTime(top.Channel);

                // 최상단 타이머의 만료 시각에 도달하지 않았으면 즉시 루프 탈출 (O(1) 검사 끝)
                if (currentTime < top.TargetTime)
                {
                    // 진행률 콜백 수행 (필요 시)
                    if (top.OnUpdateProgress != null)
                    {
                        double startTime = top.TargetTime - top.Duration;
                        float progress = Mathf.Clamp01((float)((currentTime - startTime) / top.Duration));
                        top.OnUpdateProgress.Invoke(progress);
                    }
                    break;
                }

                // 만료된 타이머 처리
                _timerQueue.Pop();
                top.Callback?.Invoke();

                if (top.IsLoop && !top.IsCancelled)
                {
                    // 반복 타이머 재등록
                    top.TargetTime = currentTime + top.Duration;
                    _timerQueue.Push(top);
                }
                else
                {
                    _timerMap.Remove(top.Handle.Id);
                }
            }
        }

        #endregion

        #region [Time Controls & HitStop]

        public void SetWorldTimeScale(float scale)
        {
            WorldTimeScale = Mathf.Max(0f, scale);
            EventBus<WorldTimeScaleChangedEvent>.Publish(new WorldTimeScaleChangedEvent(WorldTimeScale));
        }

        public void SetWorldPause(bool isPaused)
        {
            IsWorldPaused = isPaused;
            EventBus<WorldPauseStateChangedEvent>.Publish(new WorldPauseStateChangedEvent(IsWorldPaused));
        }

        /// <summary>
        /// 타격 시 순간적으로 게임 세상을 멈추거나 슬로우 모션을 거는 HitStop 연출
        /// </summary>
        public void DoHitStop(float duration, float targetScale = 0.05f)
        {
            if (_isHitStopping) return;

            _isHitStopping = true;
            _hitStopTimer = duration;
            _preHitStopScale = WorldTimeScale;

            SetWorldTimeScale(targetScale);
        }

        private void ProcessHitStop(float unscaledDeltaTime)
        {
            if (!_isHitStopping) return;

            _hitStopTimer -= unscaledDeltaTime;
            if (_hitStopTimer <= 0f)
            {
                _isHitStopping = false;
                SetWorldTimeScale(_preHitStopScale);
            }
        }

        #endregion

        #region [UTC Offline Utility]

        public double GetCurrentTime(TimeChannel channel)
        {
            return channel switch
            {
                TimeChannel.World => TotalWorldTime,
                TimeChannel.UI => TotalUITime,
                TimeChannel.Unscaled => TotalUnscaledTime,
                _ => TotalWorldTime
            };
        }

        /// <summary>
        /// 서버/로컬 세이브의 UTC 시간과 비교하여 오프라인 경과 시간(초)을 연산합니다.
        /// </summary>
        public float CalculateOfflineDeltaSeconds(DateTime lastSavedUtc)
        {
            TimeSpan delta = DateTime.UtcNow - lastSavedUtc;
            return (float)Math.Max(0, delta.TotalSeconds);
        }

        #endregion
    }
}