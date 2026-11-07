using System;
using System.Collections.Generic;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// EnvironmentController 상태(맑음/비/눈, 낮/밤)를 하늘·태양광·주변광·안개·날씨 입자·바닥 재질·야간 조명에 반영한다.
    /// 씬 설정(RenderSettings)과 씬 오브젝트만 바꾸고 머티리얼 에셋은 수정하지 않는다.
    /// </summary>
    public sealed class EnvironmentView : MonoBehaviour
    {
        [Serializable]
        private struct Look
        {
            [SerializeField] private Material _sky;
            [SerializeField] private Color _sunColor;
            [SerializeField] private float _sunIntensity;
            [SerializeField] private float _ambientIntensity;
            [SerializeField] private float _reflectionIntensity;
            [SerializeField] private Color _fogColor;
            [SerializeField] private float _fogStart;
            [SerializeField] private float _fogEnd;

            public Material Sky => _sky;
            public Color SunColor => _sunColor;
            public float SunIntensity => _sunIntensity;
            public float AmbientIntensity => _ambientIntensity;
            public float ReflectionIntensity => _reflectionIntensity;
            public Color FogColor => _fogColor;
            public float FogStart => _fogStart;
            public float FogEnd => _fogEnd;
        }

        /// <summary>날씨에 따라 켜지는 입자 효과. ScrollFactor는 도로가 다가오는 속도를 입자에 얼마나 실을지.</summary>
        [Serializable]
        private struct WeatherEffect
        {
            [SerializeField] private ParticleSystem _system;
            [SerializeField] private Weather _weather;
            [SerializeField] private float _scrollFactor;

            public ParticleSystem System => _system;
            public Weather Weather => _weather;
            public float ScrollFactor => _scrollFactor;
        }

        [SerializeField] private Light _sun;
        [SerializeField] private WeatherSurfaceView _surfaces;
        [SerializeField] private WeatherEffect[] _effects = Array.Empty<WeatherEffect>();
        [SerializeField] private NightLight[] _extraNightLights = Array.Empty<NightLight>();

        [Header("Looks")]
        [SerializeField] private Look _dayClear;
        [SerializeField] private Look _dayRain;
        [SerializeField] private Look _daySnow;
        [SerializeField] private Look _nightClear;
        [SerializeField] private Look _nightRain;
        [SerializeField] private Look _nightSnow;

        private EnvironmentController _environment;
        private SpeedController _speed;
        private TunnelSchedule _tunnels;
        private IReadOnlyList<NightLight> _nightLights;
        private bool _insideTunnel;

        public void Initialize(EnvironmentController environment, SpeedController speed, TunnelSchedule tunnels,
            IReadOnlyList<NightLight> nightLights, Transform tunnelFloorRoot = null)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            _tunnels = tunnels;
            _nightLights = nightLights ?? Array.Empty<NightLight>();
            if (_surfaces != null)
            {
                _surfaces.SetTunnelFloor(tunnels, tunnelFloorRoot);
                _surfaces.Collect();
                _surfaces.UpdateTunnelFloor(_speed.Distance);
            }
            _environment.Changed += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            if (_environment != null) _environment.Changed -= Apply;
        }

        private void LateUpdate()
        {
            if (_environment == null) return;

            bool inside = _tunnels != null && _tunnels.IsInsideAt(_speed.Distance);
            if (inside != _insideTunnel)
            {
                _insideTunnel = inside;
                UpdateEffects();
            }

            if (_surfaces != null) _surfaces.UpdateTunnelFloor(_speed.Distance);

            float speed = _speed.CurrentSpeed;
            for (int i = 0; i < _effects.Length; ++i)
            {
                ParticleSystem system = _effects[i].System;
                if (system == null || !system.isPlaying) continue;
                ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
                velocity.z = -speed * _effects[i].ScrollFactor;
            }
        }

        private void Apply()
        {
            Look look = SelectLook();
            if (look.Sky != null) RenderSettings.skybox = look.Sky;
            RenderSettings.ambientIntensity = look.AmbientIntensity;
            RenderSettings.reflectionIntensity = look.ReflectionIntensity;
            RenderSettings.fogColor = look.FogColor;
            RenderSettings.fogStartDistance = look.FogStart;
            RenderSettings.fogEndDistance = look.FogEnd;
            if (_sun != null)
            {
                _sun.color = look.SunColor;
                _sun.intensity = look.SunIntensity;
            }

            DynamicGI.UpdateEnvironment();
            if (_surfaces != null) _surfaces.Apply(_environment.Weather, _environment.IsRainbowRoad);
            UpdateEffects();

            bool isNight = _environment.IsNight;
            for (int i = 0; i < _nightLights.Count; ++i) _nightLights[i].SetOn(isNight);
            for (int i = 0; i < _extraNightLights.Length; ++i) _extraNightLights[i].SetOn(isNight);
        }

        private Look SelectLook()
        {
            bool night = _environment.IsNight;
            switch (_environment.Weather)
            {
                case Weather.Rain: return night ? _nightRain : _dayRain;
                case Weather.Snow: return night ? _nightSnow : _daySnow;
                default: return night ? _nightClear : _dayClear;
            }
        }

        private void UpdateEffects()
        {
            for (int i = 0; i < _effects.Length; ++i)
            {
                bool isOn = _effects[i].Weather == _environment.Weather && _environment.Weather != Weather.Clear && !_insideTunnel;
                SetEmitting(_effects[i].System, isOn);
            }
        }

        private static void SetEmitting(ParticleSystem system, bool isOn)
        {
            if (system == null) return;
            if (isOn && !system.isEmitting) system.Play(true);
            else if (!isOn && system.isEmitting) system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
