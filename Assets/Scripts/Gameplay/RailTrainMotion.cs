using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 고가 경전철 한 편성의 위치. 플레이어 쪽으로 마주 달려와 지나가면 잠시 쉬었다가 다시 먼 앞에서 나타난다.
    /// Z는 카메라 기준점(z = 0) 기준 열차 앞머리 위치.
    /// </summary>
    public sealed class RailTrainMotion
    {
        private readonly float _trainSpeed;
        private readonly float _spawnZ;
        private readonly float _despawnZ;
        private readonly float _interval;
        private float _waitTimer;

        public bool IsActive { get; private set; }
        public float Z { get; private set; }

        public RailTrainMotion(float trainSpeed, float spawnZ, float despawnZ, float interval)
        {
            if (trainSpeed < 0f) throw new ArgumentOutOfRangeException(nameof(trainSpeed));
            if (spawnZ <= despawnZ) throw new ArgumentException("spawnZ는 despawnZ보다 앞이어야 합니다.");
            if (interval < 0f) throw new ArgumentOutOfRangeException(nameof(interval));
            _trainSpeed = trainSpeed;
            _spawnZ = spawnZ;
            _despawnZ = despawnZ;
            _interval = interval;
            _waitTimer = 0f;
        }

        public void Tick(float deltaTime, float playerSpeed)
        {
            if (deltaTime <= 0f) return;

            if (!IsActive)
            {
                _waitTimer -= deltaTime;
                if (_waitTimer > 0f) return;
                IsActive = true;
                Z = _spawnZ;
                return;
            }

            Z -= (playerSpeed + _trainSpeed) * deltaTime;
            if (Z >= _despawnZ) return;

            IsActive = false;
            _waitTimer = _interval;
        }
    }
}
