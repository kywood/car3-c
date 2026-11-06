using System;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 고가 경전철 노선 한 구간(맵 일부에만 존재). 구간 표에서 받은 위치에 노선을 흘려보내고,
    /// 열차를 노선을 따라 달리게 한다. 노선 밖으로 빠지는 곡선 구간에서도 열차가 진행 방향으로 회전한다.
    /// </summary>
    public sealed class RailLineView : MonoBehaviour
    {
        [SerializeField] private Transform _lineRoot;
        [SerializeField] private Vector2[] _routePoints = Array.Empty<Vector2>();
        [SerializeField] private Transform[] _wagons = Array.Empty<Transform>();
        [SerializeField] private float _wagonSpacing = 18.2f;
        [SerializeField] private float _trackHeight = 8.45f;
        [SerializeField] private float _trainSpeed = 22f;
        [SerializeField] private float _restSeconds = 4f;
        [SerializeField] private float _visibleAhead = 560f;
        [SerializeField] private float _visibleBehind = 320f;

        private SpeedController _speed;
        private RailRoute _route;
        private double _lineDistance;
        private double _cycleDistance;
        private float _head;
        private float _restTimer;

        /// <param name="lineDistance">노선 로컬 z = 0이 놓이는 첫 주행 거리.</param>
        /// <param name="cycleDistance">노선이 다시 나타나는 간격(구간 표 한 주기).</param>
        public void Initialize(SpeedController speed, double lineDistance, double cycleDistance)
        {
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            if (_lineRoot == null) throw new InvalidOperationException($"{nameof(RailLineView)}: 노선 루트가 없습니다.");
            if (cycleDistance <= 0d) throw new ArgumentOutOfRangeException(nameof(cycleDistance));
            _route = new RailRoute(_routePoints);
            _lineDistance = lineDistance;
            _cycleDistance = cycleDistance;
            _head = _route.Length;
            _lineRoot.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_route == null) return;

            double distance = _speed.Distance;
            double k = Math.Ceiling((distance - _visibleBehind - _lineDistance) / _cycleDistance);
            float z = (float)(_lineDistance + Math.Max(0d, k) * _cycleDistance - distance);
            bool visible = z <= _visibleAhead && z >= -_visibleBehind;
            if (_lineRoot.gameObject.activeSelf != visible) _lineRoot.gameObject.SetActive(visible);
            if (!visible) return;

            Vector3 position = _lineRoot.localPosition;
            position.z = z;
            _lineRoot.localPosition = position;
            MoveTrain(Time.deltaTime);
        }

        private void MoveTrain(float deltaTime)
        {
            if (_restTimer > 0f)
            {
                _restTimer -= deltaTime;
                SetWagonsActive(false);
                return;
            }

            _head -= _trainSpeed * deltaTime;
            float tail = _head + _wagonSpacing * (_wagons.Length - 1);
            if (tail < 0f)
            {
                _head = _route.Length;
                _restTimer = _restSeconds;
                SetWagonsActive(false);
                return;
            }

            for (int i = 0; i < _wagons.Length; ++i)
            {
                float s = _head + i * _wagonSpacing;
                bool onTrack = s >= 0f && s <= _route.Length;
                if (_wagons[i].gameObject.activeSelf != onTrack) _wagons[i].gameObject.SetActive(onTrack);
                if (!onTrack) continue;
                Vector2 p = _route.Sample(s, out Vector2 dir);
                _wagons[i].localPosition = new Vector3(p.x, _trackHeight, p.y);
                _wagons[i].localRotation = Quaternion.LookRotation(new Vector3(-dir.x, 0f, -dir.y), Vector3.up);
            }
        }

        private void SetWagonsActive(bool active)
        {
            for (int i = 0; i < _wagons.Length; ++i)
                if (_wagons[i].gameObject.activeSelf != active) _wagons[i].gameObject.SetActive(active);
        }
    }
}
