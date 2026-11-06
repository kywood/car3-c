using System;
using UnityEngine;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 플레이어 차량 표시 어댑터. DriveSimulation의 좌우·앞뒤 위치를 Transform에 반영하고,
    /// 차선 변경 기울기와 충돌 후 무적 깜빡임을 보여준다.
    /// </summary>
    public sealed class VehicleBehaviour : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private float _maxTiltAngle = 8f;
        [SerializeField] private float _tiltPerLateralSpeed = 0.4f;
        [SerializeField] private float _blinkInterval = 0.08f;
        [SerializeField] private float _jumpPitchAngle = 7f;

        private DriveSimulation _simulation;
        private float _baseHeight;

        public DriveSimulation Simulation => _simulation;

        /// <summary>이번 프레임에 굴러간 거리(m). 바퀴 회전 표시가 구독한다.</summary>
        public event Action<float> WheelsRolled;

        public void Initialize(DriveSimulation simulation)
        {
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _baseHeight = transform.position.y;
            ApplyPose();
        }

        private void LateUpdate()
        {
            if (_simulation == null) return;
            ApplyPose();
            WheelsRolled?.Invoke(_simulation.Speed.CurrentSpeed * Time.deltaTime);
        }

        private void ApplyPose()
        {
            VehicleMotion motion = _simulation.Vehicle;
            SpeedController speed = _simulation.Speed;
            JumpMotion jump = _simulation.Jump;
            transform.position = new Vector3(motion.LateralOffset, _baseHeight + jump.Height, speed.ForwardOffset);

            if (_visualRoot == null) return;
            float tilt = Mathf.Clamp(motion.LateralVelocity * _tiltPerLateralSpeed, -_maxTiltAngle, _maxTiltAngle);
            float pitch = jump.IsAirborne ? Mathf.Lerp(-_jumpPitchAngle, _jumpPitchAngle, jump.Progress) : 0f;
            _visualRoot.localRotation = Quaternion.Euler(pitch, tilt, -tilt * 0.5f);

            bool visible = !speed.IsInvulnerable || Mathf.FloorToInt(Time.time / _blinkInterval) % 2 == 0;
            if (_visualRoot.gameObject.activeSelf != visible) _visualRoot.gameObject.SetActive(visible);
        }
    }
}
