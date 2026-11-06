using System;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 고정된 앞뒤 위치(z = 0 기준점)에서 차량을 바라보는 카메라.
    /// 좌우만 부드럽게 따라가고, 가속해도 앞으로 따라가지 않아 차량이 화면 앞쪽으로 나가 보인다.
    /// 속도가 오르면 시야각을 넓혀 속도감을 준다.
    /// </summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 4.5f, -8f);
        [SerializeField] private float _lookAheadDistance = 10f;
        [SerializeField] private float _lookHeight = 1f;
        [SerializeField, Range(0f, 1f)] private float _lateralFollowRatio = 0.7f;
        [SerializeField] private float _lateralSmoothTime = 0.12f;
        [SerializeField] private float _baseFieldOfView = 60f;
        [SerializeField] private float _maxExtraFieldOfView = 12f;
        [SerializeField] private float _fieldOfViewSharpness = 4f;

        [SerializeField] private float _shakeAmplitude = 0.35f;
        [SerializeField] private float _shakeFrequency = 18f;

        private Transform _target;
        private SpeedController _speed;
        private float _lateral;
        private float _lateralVelocity;
        private float _externalShake;

        /// <summary>
        /// 연출이 매 프레임 요청하는 흔들림 세기(0~1). 요청이 끊기면 다음 프레임부터 흔들리지 않는다.
        /// </summary>
        public void SetExternalShake(float strength)
        {
            _externalShake = Mathf.Max(_externalShake, Mathf.Clamp01(strength));
        }

        public void Initialize(Transform target, SpeedController speed)
        {
            _target = target != null ? target : throw new ArgumentNullException(nameof(target));
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            if (_camera == null) _camera = GetComponent<Camera>();
            _lateral = _target.position.x * _lateralFollowRatio;
            _lateralVelocity = 0f;
            if (_camera != null) _camera.fieldOfView = _baseFieldOfView;
            ApplyPose();
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            float desiredLateral = _target.position.x * _lateralFollowRatio;
            _lateral = Mathf.SmoothDamp(_lateral, desiredLateral, ref _lateralVelocity, _lateralSmoothTime);
            ApplyPose();

            if (_camera == null) return;
            float targetFov = _baseFieldOfView + _maxExtraFieldOfView * Mathf.Clamp01(_speed.NormalizedSpeed);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, 1f - Mathf.Exp(-_fieldOfViewSharpness * Time.deltaTime));
        }

        private void ApplyPose()
        {
            Vector3 position = new Vector3(_lateral + _offset.x, _offset.y, _offset.z);
            Vector3 lookPoint = new Vector3(_lateral, _lookHeight, _lookAheadDistance);
            if (_externalShake > 0f)
            {
                float t = Time.time * _shakeFrequency;
                Vector3 jitter = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * (2f * _shakeAmplitude * _externalShake);
                position += jitter;
                lookPoint += jitter * 0.5f;
                _externalShake = 0f;
            }
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookPoint - position, Vector3.up));
        }
    }
}
