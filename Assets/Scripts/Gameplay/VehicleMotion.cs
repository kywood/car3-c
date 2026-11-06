using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 플레이어 차량의 차선과 좌우 위치. 전진(도로 스크롤)과 앞뒤 위치는 SpeedController가 담당한다.
    /// </summary>
    public sealed class VehicleMotion
    {
        private readonly LaneLayout _layout;
        private readonly LaneController _lanes;
        private readonly float _lateralSpeed;
        private float _lateralOffset;
        private float _lateralVelocity;

        public float LateralOffset => _lateralOffset;
        public float LateralVelocity => _lateralVelocity;
        public int CurrentLane => _lanes.CurrentLane;
        public bool IsChangingLane => _lateralOffset != _layout.GetLaneCenter(_lanes.CurrentLane);

        public VehicleMotion(LaneLayout layout, int initialLane, float laneChangeDuration)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (laneChangeDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(laneChangeDuration));
            _lanes = new LaneController(layout.LaneCount, initialLane);
            _lateralSpeed = layout.LaneWidth / laneChangeDuration;
            _lateralOffset = layout.GetLaneCenter(initialLane);
        }

        /// <summary>delta가 -1이면 왼쪽, +1이면 오른쪽. 끝 차선에서 바깥으로는 이동하지 않는다.</summary>
        public bool RequestLaneChange(int delta)
        {
            return _lanes.TryMove(delta);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                _lateralVelocity = 0f;
                return;
            }

            float target = _layout.GetLaneCenter(_lanes.CurrentLane);
            float maxStep = _lateralSpeed * deltaTime;
            float difference = target - _lateralOffset;
            float step = Math.Abs(difference) <= maxStep ? difference : Math.Sign(difference) * maxStep;
            _lateralOffset += step;
            _lateralVelocity = step / deltaTime;
        }
    }
}
