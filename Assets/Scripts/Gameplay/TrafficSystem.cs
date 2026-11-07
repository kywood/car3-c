using System;
using System.Collections.Generic;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 교통 차량의 생성·이동·제거와 플레이어와의 충돌/근접 추월 판정.
    /// 차량은 고정 크기 슬롯 배열에서 재사용하며(풀링), 결과는 ITrafficListener로 알린다.
    /// </summary>
    public sealed class TrafficSystem
    {
        private const float MinAlignmentHorizon = 10f;
        private const float MaxAlignmentHorizon = 40f;

        private readonly LaneLayout _layout;
        private readonly TrafficConfig _config;
        private readonly Random _random;
        private readonly TrafficCar[] _cars;
        private readonly bool[] _laneBlocked;
        private readonly bool[] _laneCandidate;
        private readonly List<ITrafficListener> _listeners = new List<ITrafficListener>(2);
        private double _nextRowDistance;

        public IReadOnlyList<TrafficCar> Cars => _cars;
        public float CrashRemoveSeconds => _config.CrashRemoveSeconds;

        public TrafficSystem(LaneLayout layout, TrafficConfig config, Random random)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            if (config.Capacity < 1) throw new ArgumentOutOfRangeException(nameof(config), "Capacity는 1 이상이어야 합니다.");

            _cars = new TrafficCar[config.Capacity];
            for (int slot = 0; slot < _cars.Length; ++slot) _cars[slot] = new TrafficCar(slot);
            _laneBlocked = new bool[layout.LaneCount];
            _laneCandidate = new bool[layout.LaneCount];
            _nextRowDistance = NextRowGap();
        }

        public void AddListener(ITrafficListener listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (!_listeners.Contains(listener)) _listeners.Add(listener);
        }

        public void RemoveListener(ITrafficListener listener)
        {
            _listeners.Remove(listener);
        }

        public int CountActive()
        {
            int count = 0;
            for (int i = 0; i < _cars.Length; ++i) if (_cars[i].IsActive) ++count;
            return count;
        }

        public float GetLaneSpeed(int lane)
        {
            if (_layout.LaneCount == 1) return _config.SlowestLaneSpeed;
            float t = lane / (float)(_layout.LaneCount - 1);
            return _config.SlowestLaneSpeed + (_config.FastestLaneSpeed - _config.SlowestLaneSpeed) * t;
        }

        /// <summary>지정 차선·위치에 차량을 배치한다. 빈 슬롯이 없으면 null.</summary>
        public TrafficCar SpawnCar(int lane, float z)
        {
            TrafficCar car = FindFreeSlot();
            if (car == null) return null;
            car.Activate(lane, _layout.GetLaneCenter(lane), z, GetLaneSpeed(lane));
            for (int i = 0; i < _listeners.Count; ++i) _listeners[i].OnCarSpawned(car);
            return car;
        }

        /// <param name="playerSpeed">플레이어 전진 속도</param>
        /// <param name="playerDistance">플레이어 누적 주행 거리(줄 생성 간격 기준)</param>
        /// <param name="playerX">플레이어 좌우 위치</param>
        /// <param name="playerZ">플레이어 앞뒤 위치(카메라 기준점 기준)</param>
        /// <param name="playerInvulnerable">충돌 후 무적 시간 여부. 무적이면 겹쳐도 충돌을 알리지 않는다.</param>
        public void Tick(float deltaTime, float playerSpeed, double playerDistance, float playerX, float playerZ, bool playerInvulnerable)
        {
            Tick(deltaTime, playerSpeed, playerDistance, playerX, playerZ, playerInvulnerable, 0f);
        }

        /// <param name="playerHeight">점프 높이. JumpClearHeight 이상이면 차량 위를 넘어가며 충돌하지 않고, 넘어간 차량은 근접 추월로 친다.</param>
        public void Tick(float deltaTime, float playerSpeed, double playerDistance, float playerX, float playerZ, bool playerInvulnerable, float playerHeight)
        {
            if (deltaTime > 0f) UpdateCars(deltaTime, playerSpeed, playerX, playerZ, playerInvulnerable, playerHeight);

            while (playerDistance >= _nextRowDistance)
            {
                SpawnRow(playerSpeed);
                _nextRowDistance += NextRowGap();
            }
        }

        private void UpdateCars(float deltaTime, float playerSpeed, float playerX, float playerZ, bool playerInvulnerable, float playerHeight)
        {
            float hitLength = _config.CarLength * _config.HitboxScale;
            float hitWidth = _config.CarWidth * _config.HitboxScale;
            float nearMissLength = _config.CarLength + _config.NearMissLongitudinalMargin;

            for (int i = 0; i < _cars.Length; ++i)
            {
                TrafficCar car = _cars[i];
                if (!car.IsActive) continue;

                car.Z -= (playerSpeed - car.Speed) * deltaTime;
                float dz = car.Z - playerZ;
                float dx = Math.Abs(car.X - playerX);

                if (car.IsCrashed)
                {
                    car.CrashElapsed += deltaTime;
                    if (car.CrashElapsed >= _config.CrashRemoveSeconds)
                    {
                        Despawn(car);
                        continue;
                    }
                }

                bool overlaps = Math.Abs(dz) < hitLength && dx < hitWidth;
                if (!car.IsHit && overlaps && playerHeight >= _config.JumpClearHeight)
                {
                    if (dz > 0f && playerSpeed > car.Speed) car.IsNearMissArmed = true;
                }
                else if (!car.IsHit && overlaps)
                {
                    car.IsHit = true;
                    car.IsNearMissArmed = false;
                    if (!playerInvulnerable)
                    {
                        car.IsCrashed = true;
                        car.CrashElapsed = 0f;
                        for (int l = 0; l < _listeners.Count; ++l) _listeners[l].OnCollision(car);
                    }
                }
                // Only arm after closing in directly behind the car, before dodging sideways.
                else if (!car.IsHit && dz >= hitLength && dz < nearMissLength
                    && dx < hitWidth && dx <= _config.NearMissLateralDistance && playerSpeed > car.Speed)
                {
                    car.IsNearMissArmed = true;
                }

                if (!car.IsPassed && dz < -_config.CarLength)
                {
                    car.IsPassed = true;
                    if (car.IsNearMissArmed && !car.IsHit)
                        for (int l = 0; l < _listeners.Count; ++l) _listeners[l].OnNearMiss(car);
                }

                if (car.Z < _config.DespawnBehind || car.Z > _config.DespawnAhead) Despawn(car);
            }
        }

        /// <summary>
        /// 한 줄에 1~2대를 생성한다. 차선마다 속도가 달라 나중에 다른 줄 차량과 나란히 설 수 있으므로,
        /// 후보 차선마다 "플레이어에게 도달하기 전까지 BlockWindow 안으로 들어올 차선 수"를 계산해
        /// 후보를 넣어도 최소 한 차선이 비는 경우에만 생성한다. 이렇게 하면 다섯 차선이 동시에 막히지 않는다.
        /// </summary>
        private void SpawnRow(float playerSpeed)
        {
            float z = _config.SpawnAhead;
            int laneCount = _layout.LaneCount;
            float closing = Math.Max(1f, playerSpeed - _config.FastestLaneSpeed);
            float horizon = Math.Min(MaxAlignmentHorizon, Math.Max(MinAlignmentHorizon, (z - _config.DespawnBehind) / closing));

            int wanted = _random.NextDouble() < _config.SecondCarChance ? 2 : 1;
            for (int n = 0; n < wanted; ++n)
            {
                int candidateCount = 0;
                for (int lane = 0; lane < laneCount; ++lane)
                {
                    _laneCandidate[lane] = IsSafeToSpawn(lane, z, horizon);
                    if (_laneCandidate[lane]) ++candidateCount;
                }

                if (candidateCount == 0) return;

                int pick = _random.Next(candidateCount);
                for (int lane = 0; lane < laneCount; ++lane)
                {
                    if (!_laneCandidate[lane] || pick-- > 0) continue;
                    if (SpawnCar(lane, z) == null) return;
                    break;
                }
            }
        }

        private bool IsSafeToSpawn(int lane, float z, float horizon)
        {
            int laneCount = _layout.LaneCount;
            float speed = GetLaneSpeed(lane);
            for (int i = 0; i < laneCount; ++i) _laneBlocked[i] = false;
            int blocked = 0;

            for (int i = 0; i < _cars.Length; ++i)
            {
                TrafficCar car = _cars[i];
                if (!car.IsActive || _laneBlocked[car.Lane]) continue;

                float startGap = z - car.Z;
                float endGap = startGap + (speed - car.Speed) * horizon;
                bool crosses = Math.Sign(startGap) != Math.Sign(endGap);
                float minGap = crosses ? 0f : Math.Min(Math.Abs(startGap), Math.Abs(endGap));
                if (minGap >= _config.BlockWindow) continue;

                if (car.Lane == lane) return false;
                _laneBlocked[car.Lane] = true;
                ++blocked;
            }

            return blocked + 1 < laneCount;
        }

        private void Despawn(TrafficCar car)
        {
            car.Deactivate();
            for (int i = 0; i < _listeners.Count; ++i) _listeners[i].OnCarDespawned(car);
        }

        private TrafficCar FindFreeSlot()
        {
            for (int i = 0; i < _cars.Length; ++i) if (!_cars[i].IsActive) return _cars[i];
            return null;
        }

        private double NextRowGap()
        {
            return _config.MinRowGap + (_config.MaxRowGap - _config.MinRowGap) * _random.NextDouble();
        }
    }
}
