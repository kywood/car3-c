using System;

namespace Car3C.Gameplay.Launch
{
    /// <summary>
    /// 발사장이 도로 옆에 나타나는 주행 거리와 발사 이벤트 시작 조건.
    /// 발사장 n번째는 주행 거리 FirstDistance + n * RepeatDistance 지점(카메라 기준 z = 그 거리 - 현재 주행 거리)에 있다.
    /// </summary>
    public sealed class LaunchSiteSchedule
    {
        private readonly double _firstDistance;
        private readonly double _repeatDistance;
        private readonly float _triggerAhead;
        private readonly float _visibleAhead;
        private readonly float _visibleBehind;

        public LaunchSiteSchedule(double firstDistance, double repeatDistance, float triggerAhead, float visibleAhead, float visibleBehind)
        {
            if (repeatDistance <= visibleAhead + visibleBehind) throw new ArgumentOutOfRangeException(nameof(repeatDistance), "발사장끼리 겹치지 않아야 합니다.");
            if (triggerAhead > visibleAhead) throw new ArgumentOutOfRangeException(nameof(triggerAhead));
            _firstDistance = firstDistance;
            _repeatDistance = repeatDistance;
            _triggerAhead = triggerAhead;
            _visibleAhead = visibleAhead;
            _visibleBehind = visibleBehind;
        }

        /// <summary>현재 주행 거리에서 가장 가까운(아직 완전히 지나치지 않은) 발사장 번호.</summary>
        public long GetSiteIndex(double playerDistance)
        {
            double index = Math.Ceiling((playerDistance - _visibleBehind - _firstDistance) / _repeatDistance);
            return Math.Max(0L, (long)index);
        }

        /// <summary>발사장의 카메라 기준 z(앞이 +).</summary>
        public float GetSiteZ(long siteIndex, double playerDistance)
        {
            return (float)(_firstDistance + siteIndex * _repeatDistance - playerDistance);
        }

        public bool IsVisible(float siteZ)
        {
            return siteZ <= _visibleAhead && siteZ >= -_visibleBehind;
        }

        /// <summary>발사장이 이 거리 안으로 들어오면 카운트다운을 시작한다.</summary>
        public bool ShouldTrigger(float siteZ)
        {
            return siteZ <= _triggerAhead && siteZ >= -_visibleBehind;
        }

        /// <summary>
        /// 차가 가까이 왔을 때 발사되도록: 지금 속도로 secondsToLiftoff 뒤 발사장이 liftoffAhead(m) 앞에 오게 되는 순간 시작한다.
        /// 시작 거리는 triggerAhead를 넘지 않는다(보이기 전에 시작하지 않음).
        /// </summary>
        public bool ShouldTriggerOnApproach(float siteZ, float speed, float secondsToLiftoff, float liftoffAhead)
        {
            float startAhead = Math.Min(_triggerAhead, liftoffAhead + Math.Max(0f, speed) * Math.Max(0f, secondsToLiftoff));
            return siteZ <= startAhead && siteZ >= -_visibleBehind;
        }
    }
}
