using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 세계 랜드마크를 주행 거리에 따라 하나씩 차례로 배치하는 일정(먼 배경 연출).
    /// 랜드마크 k는 주행 거리 First + k × Spacing에 놓이고, Count × Spacing마다 같은 순서로 다시 나온다.
    /// 주행 거리만으로 정해져 네트워크 동기화가 필요 없다. 맵 툴 도입 시 맵 데이터의 배치로 대체된다.
    /// </summary>
    public sealed class LandmarkSchedule
    {
        private readonly double _first;

        public LandmarkSchedule(int count, double spacing, double first = 0d)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (spacing <= 0d) throw new ArgumentOutOfRangeException(nameof(spacing));
            Count = count;
            Spacing = spacing;
            _first = first;
        }

        public int Count { get; }
        public double Spacing { get; }
        public double CycleDistance => Count * Spacing;

        /// <summary>
        /// 랜드마크 index가 차 앞 몇 m(z)에 있는지. 가장 가까운 등장 위치를 [-behind, Cycle - behind) 범위로 돌려준다.
        /// </summary>
        public float GetLocalZ(int index, double playerDistance, float behind)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            double z = _first + index * Spacing - playerDistance;
            double cycle = CycleDistance;
            double shifted = z + behind;
            shifted -= Math.Floor(shifted / cycle) * cycle;
            return (float)(shifted - behind);
        }
    }
}
