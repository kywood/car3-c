using System;
using Car3C.Gameplay;
using Car3C.Gameplay.Launch;
using UnityEngine;

namespace Car3C.Presentation.Launch
{
    /// <summary>
    /// 우주 발사장 맵 이벤트의 진입점. 발사장을 도로와 같은 주행 거리로 흘려보내고,
    /// 다가오면 RocketLaunchSequence를 시작한다. 플레이어 속도·충돌 등 게임 규칙에는 관여하지 않는다.
    /// 멀티플레이 확장: 호스트만 LaunchRequested로 시작 시각을 정해 전파하고, 다른 클라이언트는 BeginLaunch(받은 시각)를 호출한다.
    /// </summary>
    public sealed class LaunchSiteController : MonoBehaviour
    {
        [SerializeField] private Transform _siteRoot;
        [SerializeField] private RocketLaunchView _view;
        [SerializeField] private LaunchCountdownView _countdown;
        [SerializeField] private LaunchAudio _audio;
        [SerializeField] private LaunchTimeline _timeline = new LaunchTimeline();
        [SerializeField] private double _firstDistance = 1900d;
        [SerializeField] private double _repeatDistance = 3520d;
        [SerializeField] private float _triggerAhead = 650f;
        [SerializeField] private float _visibleAhead = 900f;
        [SerializeField] private float _visibleBehind = 350f;
        [SerializeField] private bool _isAuthority = true;
        [Tooltip("발사(이륙) 순간 발사장이 차 앞 몇 m에 오게 할지. 속도에 맞춰 카운트다운 시작 시점을 정한다")]
        [SerializeField] private float _liftoffAhead = 80f;

        private SpeedController _speed;
        private LaunchSiteSchedule _schedule;
        private RocketLaunchSequence _sequence;
        private long _currentSite = -1;
        private bool _launchRequested;

        public RocketLaunchSequence Sequence => _sequence;

        /// <summary>권한자(싱글플레이/호스트)가 발사 시작 시각을 정했을 때. 네트워크 전송 계층이 구독한다.</summary>
        public event Action<long, double> LaunchRequested;

        /// <summary>구간 표(MapZoneSchedule)의 발사장 구간 위치와 반복 거리를 그대로 쓴다.</summary>
        public void Initialize(SpeedController speed, double siteDistance, double repeatDistance)
        {
            _firstDistance = siteDistance;
            _repeatDistance = repeatDistance;
            Initialize(speed);
        }

        public void Initialize(SpeedController speed)
        {
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            if (_siteRoot == null) throw new InvalidOperationException($"{nameof(LaunchSiteController)}: 발사장 루트가 없습니다.");
            _schedule = new LaunchSiteSchedule(_firstDistance, _repeatDistance, _triggerAhead, _visibleAhead, _visibleBehind);
            _sequence = new RocketLaunchSequence(_timeline);
            if (_view != null) _view.Bind(_sequence, _siteRoot);
            if (_countdown != null) _sequence.AddListener(_countdown);
            if (_audio != null) _sequence.AddListener(_audio);
            _siteRoot.gameObject.SetActive(false);
        }

        /// <summary>시작 시각을 받아 발사 연출을 재생한다(로컬 권한자 또는 네트워크 수신).</summary>
        public void BeginLaunch(double startTime)
        {
            _launchRequested = true;
            _sequence.Begin(startTime);
        }

        private void LateUpdate()
        {
            if (_sequence == null) return;

            double distance = _speed.Distance;
            long site = _schedule.GetSiteIndex(distance);
            if (site != _currentSite)
            {
                _currentSite = site;
                _launchRequested = false;
                _sequence.Reset();
            }

            float siteZ = _schedule.GetSiteZ(site, distance);
            bool visible = _schedule.IsVisible(siteZ);
            if (_siteRoot.gameObject.activeSelf != visible) _siteRoot.gameObject.SetActive(visible);
            if (visible)
            {
                Vector3 position = _siteRoot.localPosition;
                position.z = siteZ;
                _siteRoot.localPosition = position;
            }

            float secondsToLiftoff = _timeline.CountdownSeconds + _timeline.IgnitionSeconds;
            if (_isAuthority && !_launchRequested && _schedule.ShouldTriggerOnApproach(siteZ, _speed.CurrentSpeed, secondsToLiftoff, _liftoffAhead))
            {
                double startTime = Time.timeAsDouble;
                BeginLaunch(startTime);
                LaunchRequested?.Invoke(site, startTime);
            }

            _sequence.Update(Time.timeAsDouble);
        }
    }
}
