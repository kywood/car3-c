using System;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 세계 랜드마크(각국 상징 조형)를 LandmarkSchedule 순서대로 먼 배경에 하나씩 지나가게 한다.
    /// 랜드마크마다 인스턴스 하나를 미리 두고, 보이는 거리 안에 들어온 것만 켜서 z를 옮긴다.
    /// </summary>
    public sealed class LandmarkView : MonoBehaviour
    {
        [Serializable]
        private struct Entry
        {
            [SerializeField] private Transform _instance;
            [Tooltip("도로 중심에서의 좌우 거리(m). 음수 = 왼쪽")]
            [SerializeField] private float _x;
            [Tooltip("y축 회전(도). 정면이 도로를 보게")]
            [SerializeField] private float _yaw;

            public Transform Instance => _instance;
            public float X => _x;
            public float Yaw => _yaw;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        [SerializeField] private float _spacing = 260f;
        [SerializeField] private float _firstDistance = 320f;
        [SerializeField] private float _visibleAhead = 900f;
        [SerializeField] private float _visibleBehind = 200f;

        private LandmarkSchedule _schedule;
        private SpeedController _speed;

        public void Initialize(SpeedController speed)
        {
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            if (_entries.Length == 0) return;
            _schedule = new LandmarkSchedule(_entries.Length, _spacing, _firstDistance);
            for (int i = 0; i < _entries.Length; ++i)
                if (_entries[i].Instance != null) _entries[i].Instance.localRotation = Quaternion.Euler(0f, _entries[i].Yaw, 0f);
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (_schedule == null) return;
            double distance = _speed.Distance;
            for (int i = 0; i < _entries.Length; ++i)
            {
                Transform t = _entries[i].Instance;
                if (t == null) continue;
                float z = _schedule.GetLocalZ(i, distance, _visibleBehind);
                bool visible = z <= _visibleAhead;
                if (t.gameObject.activeSelf != visible) t.gameObject.SetActive(visible);
                if (visible) t.localPosition = new Vector3(_entries[i].X, 0f, z);
            }
        }
    }
}
