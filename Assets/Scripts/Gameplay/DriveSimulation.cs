using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 한 판의 주행 규칙을 묶는다: 차선 이동, 점프, 속도, 교통 차량, 근접 추월 가속, 충돌 감속.
    /// 플레이어 차량은 좌우(LateralOffset)와 앞뒤(ForwardOffset)만 움직이고, 도로와 교통 차량이 다가온다.
    /// </summary>
    public sealed class DriveSimulation : ITrafficListener
    {
        private readonly VehicleMotion _vehicle;
        private readonly SpeedController _speed;
        private readonly TrafficSystem _traffic;
        private readonly JumpMotion _jump;

        public VehicleMotion Vehicle => _vehicle;
        public JumpMotion Jump => _jump;
        public SpeedController Speed => _speed;
        public TrafficSystem Traffic => _traffic;
        public int NearMissCount { get; private set; }
        public int CollisionCount { get; private set; }

        public DriveSimulation(LaneLayout layout, int initialLane, DriveConfig driveConfig, TrafficConfig trafficConfig, int randomSeed)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (driveConfig == null) throw new ArgumentNullException(nameof(driveConfig));
            _vehicle = new VehicleMotion(layout, initialLane, driveConfig.LaneChangeDuration);
            _speed = new SpeedController(driveConfig);
            _jump = new JumpMotion(driveConfig.JumpHeight, driveConfig.JumpDuration);
            _traffic = new TrafficSystem(layout, trafficConfig, new Random(randomSeed));
            _traffic.AddListener(this);
        }

        public bool RequestLaneChange(int delta)
        {
            return _vehicle.RequestLaneChange(delta);
        }

        public bool RequestJump()
        {
            return _jump.TryJump();
        }

        public void Tick(float deltaTime)
        {
            _vehicle.Tick(deltaTime);
            _jump.Tick(deltaTime);
            _speed.Tick(deltaTime);
            _traffic.Tick(deltaTime, _speed.CurrentSpeed, _speed.Distance, _vehicle.LateralOffset, _speed.ForwardOffset,
                _speed.IsInvulnerable, _jump.Height);
        }

        void ITrafficListener.OnCarSpawned(TrafficCar car)
        {
        }

        void ITrafficListener.OnCarDespawned(TrafficCar car)
        {
        }

        void ITrafficListener.OnNearMiss(TrafficCar car)
        {
            ++NearMissCount;
            _speed.ApplyNearMiss();
        }

        void ITrafficListener.OnCollision(TrafficCar car)
        {
            if (_speed.ApplyCollision()) ++CollisionCount;
        }
    }
}
