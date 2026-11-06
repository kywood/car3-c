using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 차선 개수와 폭으로 각 차선 중심의 좌우 위치를 계산한다. 도로 중심은 x = 0.
    /// </summary>
    public sealed class LaneLayout
    {
        private readonly int _laneCount;
        private readonly float _laneWidth;

        public int LaneCount => _laneCount;
        public float LaneWidth => _laneWidth;
        public float RoadWidth => _laneCount * _laneWidth;
        public int CenterLane => (_laneCount - 1) / 2;

        public LaneLayout(int laneCount, float laneWidth)
        {
            if (laneCount < 1) throw new ArgumentOutOfRangeException(nameof(laneCount));
            if (laneWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(laneWidth));
            _laneCount = laneCount;
            _laneWidth = laneWidth;
        }

        public float GetLaneCenter(int lane)
        {
            if (lane < 0 || lane >= _laneCount) throw new ArgumentOutOfRangeException(nameof(lane));
            return (lane - (_laneCount - 1) * 0.5f) * _laneWidth;
        }

        /// <summary>차선 경계선 위치. boundary 0은 왼쪽 끝, LaneCount는 오른쪽 끝.</summary>
        public float GetBoundary(int boundary)
        {
            if (boundary < 0 || boundary > _laneCount) throw new ArgumentOutOfRangeException(nameof(boundary));
            return (boundary - _laneCount * 0.5f) * _laneWidth;
        }
    }
}
