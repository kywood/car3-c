using System;
using System.Collections.Generic;

namespace Car3C.Gameplay.Launch
{
    /// <summary>
    /// 로켓 발사 연출의 진행 상태(카운트다운 → 점화 → 상승 → 완료).
    /// 시작 시각(StartTime)과 현재 시각만으로 상태를 계산한다. 멀티플레이에서는 호스트가 정한 시작 시각 하나만
    /// 동기화하면 모든 클라이언트가 같은 장면을 재생한다. 게임 규칙(속도·충돌)과는 연결되지 않는다.
    /// </summary>
    public sealed class RocketLaunchSequence
    {
        private readonly LaunchTimeline _timeline;
        private readonly List<ILaunchSequenceListener> _listeners = new List<ILaunchSequenceListener>(4);
        private double _startTime;
        private int _lastCountdown = -1;

        public LaunchPhase Phase { get; private set; } = LaunchPhase.Idle;
        public bool IsRunning => Phase != LaunchPhase.Idle && Phase != LaunchPhase.Complete;
        public double StartTime => _startTime;
        public LaunchSnapshot Current { get; private set; }

        public RocketLaunchSequence(LaunchTimeline timeline)
        {
            _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        }

        public void AddListener(ILaunchSequenceListener listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (!_listeners.Contains(listener)) _listeners.Add(listener);
        }

        public void RemoveListener(ILaunchSequenceListener listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>카운트다운 시작 시각을 정한다(로컬 시각 또는 네트워크로 받은 시각).</summary>
        public void Begin(double startTime)
        {
            _startTime = startTime;
            _lastCountdown = -1;
            SetPhase(LaunchPhase.Countdown);
        }

        public void Reset()
        {
            _lastCountdown = -1;
            Current = default;
            SetPhase(LaunchPhase.Idle);
        }

        public void Update(double now)
        {
            if (Phase == LaunchPhase.Idle) return;

            LaunchSnapshot snapshot = Evaluate(_timeline, now - _startTime);
            Current = snapshot;

            if (snapshot.Phase == LaunchPhase.Countdown && snapshot.CountdownSeconds != _lastCountdown)
            {
                _lastCountdown = snapshot.CountdownSeconds;
                for (int i = 0; i < _listeners.Count; ++i) _listeners[i].OnCountdownTick(snapshot.CountdownSeconds);
            }

            SetPhase(snapshot.Phase);
        }

        /// <summary>시작 후 elapsed초 시점의 상태. 부작용 없는 순수 함수.</summary>
        public static LaunchSnapshot Evaluate(LaunchTimeline timeline, double elapsed)
        {
            if (elapsed < 0d) return new LaunchSnapshot(LaunchPhase.Countdown, (int)Math.Ceiling(timeline.CountdownSeconds), 0f, 0f, 0f);

            float t = (float)elapsed;
            if (t < timeline.CountdownSeconds)
            {
                int seconds = (int)Math.Ceiling(timeline.CountdownSeconds - t);
                return new LaunchSnapshot(LaunchPhase.Countdown, seconds, 0f, 0f, 0f);
            }

            t -= timeline.CountdownSeconds;
            if (t < timeline.IgnitionSeconds)
            {
                float ramp = t / timeline.IgnitionSeconds;
                return new LaunchSnapshot(LaunchPhase.Ignition, 0, 0f, 0.35f + 0.65f * ramp, ramp);
            }

            t -= timeline.IgnitionSeconds;
            if (t < timeline.AscentSeconds)
            {
                float altitude = 0.5f * timeline.AscentAcceleration * t * t;
                float shake = timeline.ShakeFadeSeconds > 0f ? Math.Max(0f, 1f - t / timeline.ShakeFadeSeconds) : 0f;
                return new LaunchSnapshot(LaunchPhase.Ascent, 0, altitude, 1f, shake);
            }

            float finalAltitude = 0.5f * timeline.AscentAcceleration * timeline.AscentSeconds * timeline.AscentSeconds;
            return new LaunchSnapshot(LaunchPhase.Complete, 0, finalAltitude, 0f, 0f);
        }

        private void SetPhase(LaunchPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            for (int i = 0; i < _listeners.Count; ++i) _listeners[i].OnLaunchPhaseChanged(phase);
        }
    }
}
