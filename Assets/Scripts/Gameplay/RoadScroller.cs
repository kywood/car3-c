using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 고정 개수의 도로 구간을 재사용하기 위해, 주행 거리에서 보여야 할 첫 구간 번호를 계산한다.
    /// 구간 번호 n은 주행 거리 n * SegmentLength 에서 시작하며 풀의 n % SegmentCount 슬롯을 사용한다.
    /// </summary>
    public sealed class RoadScroller
    {
        private readonly float _segmentLength;
        private readonly int _segmentCount;
        private readonly int _segmentsBehind;

        public float SegmentLength => _segmentLength;
        public int SegmentCount => _segmentCount;

        public RoadScroller(float segmentLength, int segmentCount, int segmentsBehind)
        {
            if (segmentLength <= 0f) throw new ArgumentOutOfRangeException(nameof(segmentLength));
            if (segmentCount < 1) throw new ArgumentOutOfRangeException(nameof(segmentCount));
            if (segmentsBehind < 0 || segmentsBehind >= segmentCount) throw new ArgumentOutOfRangeException(nameof(segmentsBehind));
            _segmentLength = segmentLength;
            _segmentCount = segmentCount;
            _segmentsBehind = segmentsBehind;
        }

        public long GetFirstSegmentIndex(double distance)
        {
            long current = (long)Math.Floor(distance / _segmentLength);
            return Math.Max(0L, current - _segmentsBehind);
        }

        public int GetSlot(long segmentIndex)
        {
            return (int)(segmentIndex % _segmentCount);
        }

        /// <summary>카메라 기준점(z = 0) 기준 구간 시작 z. 큰 거리에서도 정밀도를 잃지 않도록 double로 뺀다.</summary>
        public float GetSegmentLocalZ(long segmentIndex, double distance)
        {
            return (float)(segmentIndex * (double)_segmentLength - distance);
        }
    }
}
