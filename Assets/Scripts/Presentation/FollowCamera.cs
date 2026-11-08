using System;
using Car3C.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;

namespace Car3C.Presentation
{
    /// <summary>
    /// 고정된 앞뒤 위치(z = 0 기준점)에서 차량을 바라보는 카메라.
    /// 좌우만 부드럽게 따라가고, 가속해도 앞으로 따라가지 않아 차량이 화면 앞쪽으로 나가 보인다.
    /// 속도가 오르면 시야각을 넓혀 속도감을 준다.
    /// 운전석 시점(ViewMode.Cockpit)에서는 차 안 운전석 기준점에 붙고, 시점 전환은 부드럽게 이동한다.
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

        [Header("Cockpit")]
        [Tooltip("운전석 눈 위치. 차량 기울기·점프를 따르도록 차량 Visual 아래에 둔다 (+z = 앞)")]
        [SerializeField] private Transform _cockpitAnchor;
        [Tooltip("운전석 시점에서만 보이는 실내(계기판·운전대·기둥)")]
        [SerializeField] private GameObject _cockpitInterior;
        [Tooltip("운전석 시점에서 숨길 내 차 외형(그림자만 남긴다)")]
        [SerializeField] private Renderer[] _hideInCockpit = Array.Empty<Renderer>();
        [SerializeField] private float _cockpitFieldOfView = 66f;
        [SerializeField] private float _cockpitExtraFieldOfView = 8f;
        [SerializeField] private float _cockpitNearClip = 0.05f;
        [Tooltip("차의 좌우 기울기를 시점에 얼마나 실을지")]
        [SerializeField, Range(0f, 1f)] private float _cockpitRollFollow = 0.4f;
        [SerializeField] private float _transitionSeconds = 0.45f;

        private Transform _target;
        private SpeedController _speed;
        private float _lateral;
        private float _lateralVelocity;
        private float _externalShake;
        private ViewModeController _viewMode;
        private float _cockpitBlend;
        private float _chaseNearClip = 0.3f;

        /// <summary>
        /// 연출이 매 프레임 요청하는 흔들림 세기(0~1). 요청이 끊기면 다음 프레임부터 흔들리지 않는다.
        /// </summary>
        public void SetExternalShake(float strength)
        {
            _externalShake = Mathf.Max(_externalShake, Mathf.Clamp01(strength));
        }

        public void Initialize(Transform target, SpeedController speed, ViewModeController viewMode = null)
        {
            _target = target != null ? target : throw new ArgumentNullException(nameof(target));
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            _viewMode = viewMode;
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera != null) _chaseNearClip = _camera.nearClipPlane;
            _cockpitBlend = IsCockpitTarget ? 1f : 0f;
            ApplyCockpitState();
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

            float targetBlend = IsCockpitTarget ? 1f : 0f;
            if (!Mathf.Approximately(_cockpitBlend, targetBlend))
            {
                _cockpitBlend = Mathf.MoveTowards(_cockpitBlend, targetBlend, Time.deltaTime / Mathf.Max(0.01f, _transitionSeconds));
                ApplyCockpitState();
            }
            ApplyPose();

            if (_camera == null) return;
            float eased = Ease(_cockpitBlend);
            float speedFov = Mathf.Lerp(_maxExtraFieldOfView, _cockpitExtraFieldOfView, eased) * Mathf.Clamp01(_speed.NormalizedSpeed);
            float targetFov = Mathf.Lerp(_baseFieldOfView, _cockpitFieldOfView, eased) + speedFov;
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, 1f - Mathf.Exp(-_fieldOfViewSharpness * Time.deltaTime));
        }

        private bool IsCockpitTarget => _viewMode != null && _viewMode.Mode == ViewMode.Cockpit && _cockpitAnchor != null;

        private static float Ease(float t) => t * t * (3f - 2f * t);

        /// <summary>실내 표시와 근접 클립: 차 안에 들어가면 가까운 실내가 잘리지 않게 한다.</summary>
        private void ApplyCockpitState()
        {
            bool inside = _cockpitBlend > 0.5f;
            if (_cockpitInterior != null && _cockpitInterior.activeSelf != (_cockpitBlend > 0f)) _cockpitInterior.SetActive(_cockpitBlend > 0f);
            if (_camera != null) _camera.nearClipPlane = inside ? _cockpitNearClip : _chaseNearClip;

            // 운전석에서는 내 차 외형을 그리지 않고 바닥 그림자만 남긴다.
            ShadowCastingMode mode = inside ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            for (int i = 0; i < _hideInCockpit.Length; ++i)
                if (_hideInCockpit[i] != null && _hideInCockpit[i].shadowCastingMode != mode) _hideInCockpit[i].shadowCastingMode = mode;
        }

        private void ApplyPose()
        {
            Vector3 position = new Vector3(_lateral + _offset.x, _offset.y, _offset.z);
            Vector3 lookPoint = new Vector3(_lateral, _lookHeight, _lookAheadDistance);
            Quaternion rotation = Quaternion.LookRotation(lookPoint - position, Vector3.up);

            if (_cockpitBlend > 0f && _cockpitAnchor != null)
            {
                // 차의 기울기(요·피치)는 따르고, 좌우 롤은 일부만 실어 어지럽지 않게 한다.
                Vector3 euler = _cockpitAnchor.rotation.eulerAngles;
                float roll = Mathf.DeltaAngle(0f, euler.z) * _cockpitRollFollow;
                Quaternion cockpitRotation = Quaternion.Euler(euler.x, euler.y, roll);
                float eased = Ease(_cockpitBlend);
                position = Vector3.Lerp(position, _cockpitAnchor.position, eased);
                rotation = Quaternion.Slerp(rotation, cockpitRotation, eased);
            }

            if (_externalShake > 0f)
            {
                float t = Time.time * _shakeFrequency;
                float amplitude = _shakeAmplitude * (_cockpitBlend > 0.5f ? 0.3f : 1f);
                Vector3 jitter = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * (2f * amplitude * _externalShake);
                position += rotation * jitter;
                _externalShake = 0f;
            }
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
