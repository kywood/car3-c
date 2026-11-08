using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 도로에 일정 간격으로 깔리는 가속 발판. n번째 발판의 위치(주행 거리)와 차선은 n으로만 정해져(결정적)
    /// 화면 표시와 판정, 향후 멀티플레이가 같은 배치를 쓴다. 플레이어가 발판 위를 지나가면 한 번만 발동한다.
    /// </summary>
    public sealed class BoostPadField
    {
        private readonly LaneLayout _layout;
        private readonly double _firstDistance;
        private readonly double _spacing;
        private readonly float _jitter;
        private readonly int _seed;
        private long _lastTriggered = -1;

        public float Length { get; }
        public float Width => _layout.LaneWidth * 0.8f;
        public double Spacing => _spacing;

        public BoostPadField(LaneLayout layout, double firstDistance, double spacing, float length, float jitter, int seed)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (spacing <= length + 2f * Math.Abs(jitter)) throw new ArgumentOutOfRangeException(nameof(spacing), "발판끼리 겹치지 않아야 합니다.");
            if (length <= 0f) throw new ArgumentOutOfRangeException(nameof(length));
            _firstDistance = firstDistance;
            _spacing = spacing;
            Length = length;
            _jitter = Math.Abs(jitter);
            _seed = seed;
        }

        /// <summary>n번째 발판이 시작되는 주행 거리.</summary>
        public double GetPadDistance(long index)
        {
            return _firstDistance + index * _spacing + (Hash(index, 1) * 2f - 1f) * _jitter;
        }

        public int GetPadLane(long index)
        {
            return (int)(Hash(index, 2) * _layout.LaneCount) % _layout.LaneCount;
        }

        public float GetPadX(long index)
        {
            return _layout.GetLaneCenter(GetPadLane(index));
        }

        /// <summary>주행 거리 distance 부근(앞뒤 포함)에서 화면에 보일 수 있는 첫 발판 번호.</summary>
        public long GetFirstIndexFrom(double distance)
        {
            return Math.Max(0L, (long)Math.Floor((distance - _firstDistance - _jitter - Length) / _spacing));
        }

        /// <param name="playerDistance">누적 주행 거리</param>
        /// <param name="playerX">플레이어 좌우 위치</param>
        /// <param name="playerZ">플레이어 앞뒤 위치(카메라 기준점 기준)</param>
        /// <param name="airborne">점프 중이면 발동하지 않는다</param>
        /// <param name="triggeredIndex">발동한 발판 번호(없으면 -1)</param>
        public bool TryTrigger(double playerDistance, float playerX, float playerZ, bool airborne, out long triggeredIndex)
        {
            triggeredIndex = -1;
            if (airborne) return false;

            double playerWorld = playerDistance + playerZ;
            long center = (long)Math.Floor((playerWorld - _firstDistance) / _spacing);
            for (long index = Math.Max(0L, center - 1); index <= center + 1; ++index)
            {
                if (index == _lastTriggered) continue;
                double start = GetPadDistance(index);
                if (playerWorld < start || playerWorld > start + Length) continue;
                if (Math.Abs(playerX - GetPadX(index)) > Width * 0.5f) continue;

                _lastTriggered = index;
                triggeredIndex = index;
                return true;
            }
            return false;
        }

        private float Hash(long index, int salt)
        {
            unchecked
            {
                ulong h = (ulong)index * 0x9E3779B97F4A7C15UL ^ (ulong)(uint)_seed * 0xC2B2AE3D27D4EB4FUL ^ (ulong)salt * 0x165667B19E3779F9UL;
                h ^= h >> 33; h *= 0xFF51AFD7ED558CCDUL; h ^= h >> 33;
                return (h >> 40) / (float)(1UL << 24);
            }
        }
    }
}
