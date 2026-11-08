using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 플레이어 속도 규칙. 근접 추월은 가속(연속 시 콤보 보너스), 충돌은 감속 후 잠시 무적.
    /// 평상시에는 기본 속도로 서서히 돌아간다. 속도가 높을수록 차량이 화면 앞쪽으로 나간다(ForwardOffset).
    /// </summary>
    public sealed class SpeedController
    {
        private readonly DriveConfig _config;
        private float _speed;
        private double _distance;
        private int _combo;
        private float _timeSinceNearMiss = float.MaxValue;
        private float _invulnerableTimer;
        private float _forwardOffset;

        public float CurrentSpeed => _speed;
        public double Distance => _distance;
        public int Combo => _combo;
        public bool IsInvulnerable => _invulnerableTimer > 0f;
        public float NormalizedSpeed => (_speed - _config.MinSpeed) / (_config.MaxSpeed - _config.MinSpeed);
        /// <summary>카메라 기준점(z = 0)에서 플레이어 차량이 놓일 z. 가속 시 +, 감속 시 -.</summary>
        public float ForwardOffset => _forwardOffset;

        public SpeedController(DriveConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            if (config.MinSpeed <= 0f || config.MinSpeed >= config.BaseSpeed || config.BaseSpeed >= config.MaxSpeed)
                throw new ArgumentException("속도 설정은 0 < Min < Base < Max 이어야 합니다.", nameof(config));
            _speed = config.BaseSpeed;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            _distance += _speed * deltaTime;

            if (_speed > _config.BaseSpeed)
                _speed = Math.Max(_config.BaseSpeed, _speed - _config.BoostDecay * deltaTime);
            else if (_speed < _config.BaseSpeed)
                _speed = Math.Min(_config.BaseSpeed, _speed + _config.RecoverAcceleration * deltaTime);

            if (_invulnerableTimer > 0f) _invulnerableTimer = Math.Max(0f, _invulnerableTimer - deltaTime);
            if (_timeSinceNearMiss < float.MaxValue) _timeSinceNearMiss += deltaTime;
            if (_timeSinceNearMiss > _config.ComboWindow) _combo = 0;

            float blend = 1f - (float)Math.Exp(-_config.ForwardOffsetSharpness * deltaTime);
            _forwardOffset += (GetTargetForwardOffset() - _forwardOffset) * blend;
        }

        public float GetTargetForwardOffset()
        {
            if (_speed >= _config.BaseSpeed)
                return (_speed - _config.BaseSpeed) / (_config.MaxSpeed - _config.BaseSpeed) * _config.MaxForwardOffset;
            return -(_config.BaseSpeed - _speed) / (_config.BaseSpeed - _config.MinSpeed) * _config.MaxBackwardOffset;
        }

        public void ApplyNearMiss()
        {
            _combo = _timeSinceNearMiss <= _config.ComboWindow ? _combo + 1 : 1;
            _timeSinceNearMiss = 0f;
            float boost = _config.NearMissBoost * (1f + (_combo - 1) * _config.ComboBonusRatio);
            _speed = Math.Min(_config.MaxSpeed, _speed + boost);
        }

        /// <summary>가속 발판 등 즉시 가속. 최고 속도를 넘지 않는다. 콤보에는 영향이 없다.</summary>
        public void ApplyBoost(float speedGain)
        {
            if (speedGain <= 0f) return;
            _speed = Math.Min(_config.MaxSpeed, _speed + speedGain);
        }

        /// <summary>무적 상태면 무시하고 false를 반환한다.</summary>
        public bool ApplyCollision()
        {
            if (IsInvulnerable) return false;
            _speed = Math.Max(_config.MinSpeed, _speed * _config.CollisionSpeedMultiplier);
            _combo = 0;
            _timeSinceNearMiss = float.MaxValue;
            _invulnerableTimer = _config.InvulnerableDuration;
            return true;
        }
    }
}
