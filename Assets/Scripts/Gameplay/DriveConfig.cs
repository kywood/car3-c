using System;
using UnityEngine;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 플레이어 속도·차선 변경·화면상 앞뒤 위치 조정값. DriveSettings 에셋에 들어가며, 테스트에서는 생성자로 만든다.
    /// </summary>
    [Serializable]
    public sealed class DriveConfig
    {
        [SerializeField] private float _baseSpeed = 22f;
        [SerializeField] private float _minSpeed = 10f;
        [SerializeField] private float _maxSpeed = 45f;
        [SerializeField] private float _recoverAcceleration = 4f;
        [SerializeField] private float _boostDecay = 1.5f;
        [SerializeField] private float _nearMissBoost = 3f;
        [SerializeField] private float _comboBonusRatio = 0.5f;
        [SerializeField] private float _comboWindow = 2.5f;
        [SerializeField, Range(0.1f, 1f)] private float _collisionSpeedMultiplier = 0.5f;
        [SerializeField] private float _invulnerableDuration = 1.2f;
        [SerializeField] private float _laneChangeDuration = 0.18f;
        [SerializeField] private float _maxForwardOffset = 7f;
        [SerializeField] private float _maxBackwardOffset = 2f;
        [SerializeField] private float _forwardOffsetSharpness = 3f;
        [SerializeField] private float _jumpHeight = 2.6f;
        [SerializeField] private float _jumpDuration = 0.9f;

        public float JumpHeight => _jumpHeight;
        public float JumpDuration => _jumpDuration;
        public float BaseSpeed => _baseSpeed;
        public float MinSpeed => _minSpeed;
        public float MaxSpeed => _maxSpeed;
        /// <summary>기본 속도 아래일 때 기본 속도로 회복하는 가속도(m/s²).</summary>
        public float RecoverAcceleration => _recoverAcceleration;
        /// <summary>기본 속도 위일 때 기본 속도로 내려가는 감속도(m/s²).</summary>
        public float BoostDecay => _boostDecay;
        public float NearMissBoost => _nearMissBoost;
        /// <summary>연속 근접 추월 1회마다 추가되는 가속 비율.</summary>
        public float ComboBonusRatio => _comboBonusRatio;
        public float ComboWindow => _comboWindow;
        public float CollisionSpeedMultiplier => _collisionSpeedMultiplier;
        public float InvulnerableDuration => _invulnerableDuration;
        public float LaneChangeDuration => _laneChangeDuration;
        /// <summary>최고 속도일 때 차량이 카메라 기준 위치보다 앞으로 나가는 거리.</summary>
        public float MaxForwardOffset => _maxForwardOffset;
        /// <summary>최저 속도일 때 차량이 뒤로 밀리는 거리.</summary>
        public float MaxBackwardOffset => _maxBackwardOffset;
        public float ForwardOffsetSharpness => _forwardOffsetSharpness;

        public DriveConfig()
        {
        }

        public DriveConfig(float baseSpeed, float minSpeed, float maxSpeed, float recoverAcceleration, float boostDecay,
            float nearMissBoost, float comboBonusRatio, float comboWindow, float collisionSpeedMultiplier,
            float invulnerableDuration, float laneChangeDuration, float maxForwardOffset, float maxBackwardOffset,
            float forwardOffsetSharpness, float jumpHeight = 2.6f, float jumpDuration = 0.9f)
        {
            _jumpHeight = jumpHeight;
            _jumpDuration = jumpDuration;
            _baseSpeed = baseSpeed;
            _minSpeed = minSpeed;
            _maxSpeed = maxSpeed;
            _recoverAcceleration = recoverAcceleration;
            _boostDecay = boostDecay;
            _nearMissBoost = nearMissBoost;
            _comboBonusRatio = comboBonusRatio;
            _comboWindow = comboWindow;
            _collisionSpeedMultiplier = collisionSpeedMultiplier;
            _invulnerableDuration = invulnerableDuration;
            _laneChangeDuration = laneChangeDuration;
            _maxForwardOffset = maxForwardOffset;
            _maxBackwardOffset = maxBackwardOffset;
            _forwardOffsetSharpness = forwardOffsetSharpness;
        }
    }
}
