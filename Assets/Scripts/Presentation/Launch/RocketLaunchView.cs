using System;
using Car3C.Gameplay.Launch;
using UnityEngine;

namespace Car3C.Presentation.Launch
{
    /// <summary>
    /// 발사 연출 표시: 우주선 상승, 엔진 화염·발사대 연기, 엔진 불빛, 경고등 점멸, 카메라 흔들림.
    /// RocketLaunchSequence의 현재 상태(Current)만 읽어 그린다.
    /// </summary>
    public sealed class RocketLaunchView : MonoBehaviour, ILaunchSequenceListener
    {
        [SerializeField] private Transform _rocket;
        [SerializeField] private ParticleSystem _flame;
        [SerializeField] private ParticleSystem _smoke;
        [SerializeField] private Light _engineLight;
        [SerializeField] private GameObject[] _warningLights = Array.Empty<GameObject>();
        [SerializeField] private FollowCamera _camera;
        [Tooltip("발사 때 카메라를 흔들지 여부. 꺼 두면 도로 화면이 흔들리지 않는다.")]
        [SerializeField] private bool _shakeCamera;
        [SerializeField] private float _flameRate = 140f;
        [SerializeField] private float _smokeRate = 45f;
        [SerializeField] private float _smokeMaxAltitude = 160f;
        [SerializeField] private float _engineLightIntensity = 9f;
        [SerializeField] private float _blinkInterval = 0.45f;
        [SerializeField] private float _shakeFullDistance = 180f;
        [SerializeField] private float _shakeZeroDistance = 650f;

        private RocketLaunchSequence _sequence;
        private Transform _siteRoot;
        private Vector3 _rocketRestPosition;

        public void Bind(RocketLaunchSequence sequence, Transform siteRoot)
        {
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _siteRoot = siteRoot;
            _rocketRestPosition = _rocket.localPosition;
            _sequence.AddListener(this);
            ResetVisuals();
        }

        private void LateUpdate()
        {
            if (_sequence == null || !_siteRoot.gameObject.activeInHierarchy) return;

            LaunchSnapshot s = _sequence.Current;
            _rocket.localPosition = _rocketRestPosition + Vector3.up * s.Altitude;

            SetRate(_flame, s.Thrust * _flameRate);
            SetRate(_smoke, s.Thrust > 0f && s.Altitude < _smokeMaxAltitude ? s.Thrust * _smokeRate : 0f);
            if (_engineLight != null)
            {
                float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 12f, 0.4f);
                _engineLight.intensity = s.Thrust * _engineLightIntensity * flicker;
                _engineLight.enabled = s.Thrust > 0f;
            }

            bool blinking = s.Phase == LaunchPhase.Countdown || s.Phase == LaunchPhase.Ignition;
            bool lit = blinking && Mathf.FloorToInt(Time.time / _blinkInterval) % 2 == 0;
            for (int i = 0; i < _warningLights.Length; ++i)
                if (_warningLights[i].activeSelf != lit) _warningLights[i].SetActive(lit);

            if (_shakeCamera && _camera != null && s.Shake > 0f)
            {
                float distance = Vector3.Distance(_camera.transform.position, _rocket.position);
                float falloff = Mathf.InverseLerp(_shakeZeroDistance, _shakeFullDistance, distance);
                _camera.SetExternalShake(s.Shake * falloff);
            }
        }

        public void OnLaunchPhaseChanged(LaunchPhase phase)
        {
            if (phase == LaunchPhase.Idle) ResetVisuals();
            if (phase == LaunchPhase.Ignition)
            {
                if (_flame != null) _flame.Play(true);
                if (_smoke != null) _smoke.Play(true);
            }
            if (phase == LaunchPhase.Complete)
            {
                if (_flame != null) _flame.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (_smoke != null) _smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public void OnCountdownTick(int secondsLeft)
        {
        }

        private void ResetVisuals()
        {
            _rocket.localPosition = _rocketRestPosition;
            if (_flame != null) _flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_smoke != null) _smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_engineLight != null) _engineLight.enabled = false;
            for (int i = 0; i < _warningLights.Length; ++i) _warningLights[i].SetActive(false);
        }

        private static void SetRate(ParticleSystem system, float rate)
        {
            if (system == null) return;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;
        }
    }
}
