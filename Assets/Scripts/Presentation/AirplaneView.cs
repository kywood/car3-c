using System;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// SkyTrafficSchedule의 비행을 하늘에 그린다. 비행기는 멀고 높이 떠 있어 도로 스크롤과 상관없이 하늘을 가로지른다.
    /// 비행운(TrailRenderer)은 새 비행마다 지우고, 항법등은 깜빡인다.
    /// </summary>
    public sealed class AirplaneView : MonoBehaviour
    {
        [SerializeField] private Transform _plane;
        [SerializeField] private TrailRenderer[] _trails = Array.Empty<TrailRenderer>();
        [SerializeField] private GameObject _strobe;
        [Tooltip("지나가는 동안 좌우로 이동하는 거리(m)")]
        [SerializeField] private float _crossingWidth = 1200f;
        [SerializeField] private float _strobeInterval = 1.2f;

        private SkyTrafficSchedule _schedule;
        private long _flightIndex = -1;

        public void Initialize(SkyTrafficSchedule schedule)
        {
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            if (_plane != null) _plane.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_schedule == null || _plane == null) return;

            bool flying = _schedule.TryGetFlight(Time.timeSinceLevelLoadAsDouble, out SkyFlight flight);
            if (_plane.gameObject.activeSelf != flying) _plane.gameObject.SetActive(flying);
            if (!flying) return;

            float side = flight.LeftToRight ? 1f : -1f;
            float x = Mathf.Lerp(-0.5f, 0.5f, flight.Progress) * _crossingWidth * side;
            // 살짝 비스듬히 지나가도록 앞뒤로도 조금 움직인다.
            float z = flight.DistanceAhead + (flight.Progress - 0.5f) * 120f;
            float y = flight.Altitude + flight.Climb * flight.Progress;
            Vector3 position = new Vector3(x, y, z);
            Vector3 direction = new Vector3(_crossingWidth * side, flight.Climb, 120f).normalized;
            _plane.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));

            if (flight.Index != _flightIndex)
            {
                _flightIndex = flight.Index;
                for (int i = 0; i < _trails.Length; ++i) if (_trails[i] != null) _trails[i].Clear();
            }

            if (_strobe != null)
            {
                bool on = Mathf.Repeat(Time.time, _strobeInterval) < 0.08f;
                if (_strobe.activeSelf != on) _strobe.SetActive(on);
            }
        }
    }
}
