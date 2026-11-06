using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 현재 차선 번호를 관리하고 범위를 벗어나는 이동을 거부한다.
    /// </summary>
    public sealed class LaneController
    {
        private readonly int _laneCount;
        private int _currentLane;

        public int CurrentLane => _currentLane;

        public LaneController(int laneCount, int initialLane)
        {
            if (laneCount < 1) throw new ArgumentOutOfRangeException(nameof(laneCount));
            if (initialLane < 0 || initialLane >= laneCount) throw new ArgumentOutOfRangeException(nameof(initialLane));
            _laneCount = laneCount;
            _currentLane = initialLane;
        }

        public bool TryMove(int delta)
        {
            int nextLane = _currentLane + delta;
            if (delta == 0 || nextLane < 0 || nextLane >= _laneCount) return false;
            _currentLane = nextLane;
            return true;
        }
    }
}
