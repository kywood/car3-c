using System;
using UnityEngine;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 교통 차량 생성·판정 조정값. z는 카메라 기준점(z = 0) 기준 상대 위치이며 앞쪽이 +.
    /// </summary>
    [Serializable]
    public sealed class TrafficConfig
    {
        [SerializeField] private int _capacity = 24;
        [SerializeField] private float _spawnAhead = 140f;
        [SerializeField] private float _despawnBehind = -20f;
        [SerializeField] private float _despawnAhead = 170f;
        [SerializeField] private float _minRowGap = 16f;
        [SerializeField] private float _maxRowGap = 32f;
        [SerializeField, Range(0f, 1f)] private float _secondCarChance = 0.4f;
        [SerializeField] private float _blockWindow = 14f;
        [SerializeField] private float _slowestLaneSpeed = 8f;
        [SerializeField] private float _fastestLaneSpeed = 14f;
        [SerializeField] private float _carLength = 4f;
        [SerializeField] private float _carWidth = 1.8f;
        [SerializeField, Range(0.5f, 1f)] private float _hitboxScale = 0.9f;
        [SerializeField] private float _nearMissLateralDistance = 3.6f;
        [SerializeField] private float _nearMissLongitudinalMargin = 1f;
        [SerializeField] private float _crashRemoveSeconds = 1.2f;
        [SerializeField] private float _jumpClearHeight = 1.3f;

        /// <summary>플레이어가 이 높이 이상 떠 있으면 교통 차량 위를 넘어간다(충돌 없음).</summary>
        public float JumpClearHeight => _jumpClearHeight;

        public int Capacity => _capacity;
        /// <summary>충돌한 차량이 돌며 깜빡이다 사라지기까지의 시간.</summary>
        public float CrashRemoveSeconds => _crashRemoveSeconds;
        public float SpawnAhead => _spawnAhead;
        public float DespawnBehind => _despawnBehind;
        public float DespawnAhead => _despawnAhead;
        /// <summary>줄(row) 생성 간격. 플레이어 주행 거리 기준.</summary>
        public float MinRowGap => _minRowGap;
        public float MaxRowGap => _maxRowGap;
        public float SecondCarChance => _secondCarChance;
        /// <summary>이 z 범위 안의 차량들은 같은 줄로 보고, 반드시 한 차선 이상 비워 둔다.</summary>
        public float BlockWindow => _blockWindow;
        public float SlowestLaneSpeed => _slowestLaneSpeed;
        public float FastestLaneSpeed => _fastestLaneSpeed;
        public float CarLength => _carLength;
        public float CarWidth => _carWidth;
        public float HitboxScale => _hitboxScale;
        /// <summary>좌우 중심 거리가 이 값 이하로 스쳐 지나가면 근접 추월.</summary>
        public float NearMissLateralDistance => _nearMissLateralDistance;
        public float NearMissLongitudinalMargin => _nearMissLongitudinalMargin;

        public TrafficConfig()
        {
        }

        public TrafficConfig(int capacity, float spawnAhead, float despawnBehind, float despawnAhead, float minRowGap,
            float maxRowGap, float secondCarChance, float blockWindow, float slowestLaneSpeed, float fastestLaneSpeed,
            float carLength, float carWidth, float hitboxScale, float nearMissLateralDistance, float nearMissLongitudinalMargin)
        {
            _capacity = capacity;
            _spawnAhead = spawnAhead;
            _despawnBehind = despawnBehind;
            _despawnAhead = despawnAhead;
            _minRowGap = minRowGap;
            _maxRowGap = maxRowGap;
            _secondCarChance = secondCarChance;
            _blockWindow = blockWindow;
            _slowestLaneSpeed = slowestLaneSpeed;
            _fastestLaneSpeed = fastestLaneSpeed;
            _carLength = carLength;
            _carWidth = carWidth;
            _hitboxScale = hitboxScale;
            _nearMissLateralDistance = nearMissLateralDistance;
            _nearMissLongitudinalMargin = nearMissLongitudinalMargin;
        }
    }
}
