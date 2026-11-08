using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 한 판의 주행 규칙을 묶는다: 차선 이동, 점프, 속도, 가속 발판, 교통 차량, 근접 추월 가속, 충돌 감속.
    /// 플레이어 차량은 좌우(LateralOffset)와 앞뒤(ForwardOffset)만 움직이고, 도로와 교통 차량이 다가온다.
    /// </summary>
    public sealed class DriveSimulation : ITrafficListener
    {
        private readonly VehicleMotion _vehicle;
        private readonly SpeedController _speed;
        private readonly TrafficSystem _traffic;
        private readonly JumpMotion _jump;
        private readonly BoostPadField _boostPads;
        private readonly float _boostPadSpeedGain;

        public VehicleMotion Vehicle => _vehicle;
        public JumpMotion Jump => _jump;
        public SpeedController Speed => _speed;
        public TrafficSystem Traffic => _traffic;
        public BoostPadField BoostPads => _boostPads;
        public int BoostCount { get; private set; }

        /// <summary>가속 발판을 밟았을 때(발판 번호). HUD·사운드·이펙트가 구독한다.</summary>
        public event Action<long> BoostPadTriggered;
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
            _boostPads = new BoostPadField(layout, driveConfig.BoostPadFirstDistance, driveConfig.BoostPadSpacing,
                driveConfig.BoostPadLength, driveConfig.BoostPadJitter, seed: 4021);
            _boostPadSpeedGain = driveConfig.BoostPadSpeedGain;
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
            if (_boostPads.TryTrigger(_speed.Distance, _vehicle.LateralOffset, _speed.ForwardOffset, _jump.IsAirborne, out long pad))
            {
                ++BoostCount;
                _speed.ApplyBoost(_boostPadSpeedGain);
                BoostPadTriggered?.Invoke(pad);
            }
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
