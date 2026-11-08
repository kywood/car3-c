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
        private static readonly int SunGlintScaleId = Shader.PropertyToID("_Car3CSunGlintScale");
        // Car3C_WeatherSurface.hlsl / Car3C_WetStreak.shader 전역 값.
        private static readonly int RainIntensityId = Shader.PropertyToID("_Car3CRainIntensity");
        private static readonly int PuddleAmountId = Shader.PropertyToID("_Car3CPuddleAmount");
        private static readonly int SnowCoverId = Shader.PropertyToID("_Car3CSnowCover");
        private static readonly int WetnessId = Shader.PropertyToID("_Car3CWetness");
        private static readonly int RoadFlowId = Shader.PropertyToID("_Car3CRoadFlow");
        /// <summary>셰이더 무늬가 z 방향으로 반복되는 길이(m). Car3C_WeatherSurface.hlsl의 CAR3C_FLOW_PERIOD와 같아야 한다.</summary>
        private const double RoadFlowPeriod = 256d;

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

        /// <summary>
        /// 날씨 단계별 조정. 기본 Look 위에 곱해진다. 배율 값이 0 이하이면 1(변화 없음)로 본다.
        /// </summary>
        [Serializable]
        private struct LevelLook
        {
            [SerializeField] private Weather _weather;
            [SerializeField] private int _level;
            [Tooltip("낮 하늘 교체. 비우면 기본 Look의 하늘")]
            [SerializeField] private Material _daySky;
            [Tooltip("낮 태양 고도(도). 0 이하면 기본 각도. 하늘 이미지의 해 위치와 맞춘다")]
            [SerializeField] private float _daySunElevation;
            [SerializeField] private float _sunScale;
            [SerializeField] private float _ambientScale;
            [Tooltip("안개 시작·끝 거리 배율. 작을수록 짙다")]
            [SerializeField] private float _fogDistanceScale;
            [Tooltip("안개 색 섞기. 알파 = 섞는 정도")]
            [SerializeField] private Color _fogTint;
            [SerializeField] private float _particleRateScale;
            [SerializeField] private float _particleSizeScale;
            [Tooltip("낙하 속도 배율")]
            [SerializeField] private float _particleFallScale;
            [Tooltip("옆바람(m/s, +x). 폭설 등에서 입자를 옆으로 날린다")]
            [SerializeField] private float _windX;
            [Header("Surface (Car3C/WeatherLit)")]
            [Tooltip("빗방울 파문 밀도 0~1")]
            [SerializeField] private float _rainIntensity;
            [Tooltip("노면 물웅덩이 양 0~1")]
            [SerializeField] private float _puddleAmount;
            [Tooltip("물체 위에 쌓인 눈 0~1")]
            [SerializeField] private float _snowCover;

            public float RainIntensity => _rainIntensity;
            public float PuddleAmount => _puddleAmount;
            public float SnowCover => _snowCover;
            public Weather Weather => _weather;
            public int Level => _level;
            public Material DaySky => _daySky;
            public float DaySunElevation => _daySunElevation;
            public float SunScale => OrOne(_sunScale);
            public float AmbientScale => OrOne(_ambientScale);
            public float FogDistanceScale => OrOne(_fogDistanceScale);
            public Color FogTint => _fogTint;
            public float ParticleRateScale => OrOne(_particleRateScale);
            public float ParticleSizeScale => OrOne(_particleSizeScale);
            public float ParticleFallScale => OrOne(_particleFallScale);
            public float WindX => _windX;

            private static float OrOne(float value) => value > 0f ? value : 1f;
        }

        /// <summary>입자 효과의 원래 값(단계 배율의 기준).</summary>
        private struct EffectBase
        {
            public float Rate;
            public int MaxParticles;
            public float Size;
            public float FallY;
            public Vector3 ShapePosition;
            public float Lifetime;
        }

        [SerializeField] private Light _sun;
        [SerializeField] private WeatherSurfaceView _surfaces;
        [SerializeField] private WeatherEffect[] _effects = Array.Empty<WeatherEffect>();
        [SerializeField] private NightLight[] _extraNightLights = Array.Empty<NightLight>();
        [Tooltip("비 올 때만 켜는 후처리 Volume 오브젝트(조금 어둡고 채도·대비 낮게, 차갑게)")]
        [SerializeField] private GameObject _rainGrade;

        [Header("Looks")]
        [SerializeField] private Look _dayClear;
        [SerializeField] private Look _dayRain;
        [SerializeField] private Look _daySnow;
        [SerializeField] private Look _nightClear;
        [SerializeField] private Look _nightRain;
        [SerializeField] private Look _nightSnow;

        [Header("Weather Levels")]
        [SerializeField] private LevelLook[] _levels = Array.Empty<LevelLook>();

        private EnvironmentController _environment;
        private EffectBase[] _effectBases = Array.Empty<EffectBase>();
        private Vector3 _baseSunEuler;
        private float _windX;
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
            CaptureBases();
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
            Shader.SetGlobalFloat(RoadFlowId, WaterFlow.GetOffset(_speed.Distance, RoadFlowPeriod));

            float speed = _speed.CurrentSpeed;
            for (int i = 0; i < _effects.Length; ++i)
            {
                ParticleSystem system = _effects[i].System;
                if (system == null || !system.isPlaying) continue;
                ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
                velocity.z = -speed * _effects[i].ScrollFactor;
                velocity.x = _windX;
            }
        }

        private void CaptureBases()
        {
            if (_sun != null) _baseSunEuler = _sun.transform.eulerAngles;
            _effectBases = new EffectBase[_effects.Length];
            for (int i = 0; i < _effects.Length; ++i)
            {
                ParticleSystem system = _effects[i].System;
                if (system == null) continue;
                _effectBases[i] = new EffectBase
                {
                    Rate = system.emission.rateOverTimeMultiplier,
                    MaxParticles = system.main.maxParticles,
                    Size = system.main.startSizeMultiplier,
                    FallY = system.velocityOverLifetime.yMultiplier,
                    ShapePosition = system.shape.position,
                    Lifetime = system.main.startLifetimeMultiplier,
                };
            }
        }

        private void Apply()
        {
            Look look = SelectLook();
            LevelLook level = SelectLevel(out bool hasLevel);
            bool isNight = _environment.IsNight;
            bool useLevelSky = hasLevel && !isNight && level.DaySky != null;

            Material sky = useLevelSky ? level.DaySky : look.Sky;
            if (sky != null) RenderSettings.skybox = sky;
            // 비·눈(흐린 하늘)에서는 바다의 해 반짝임을 끈다.
            Shader.SetGlobalFloat(SunGlintScaleId, _environment.Weather == Weather.Clear ? 1f : 0f);

            float ambientScale = hasLevel ? level.AmbientScale : 1f;
            float sunScale = hasLevel ? level.SunScale : 1f;
            float fogScale = hasLevel ? level.FogDistanceScale : 1f;
            // 안개 색 섞기는 낮에만(밤에 하얀 안개가 빛나 보이지 않게).
            Color fogTint = hasLevel && !isNight ? level.FogTint : Color.clear;

            RenderSettings.ambientIntensity = look.AmbientIntensity * ambientScale;
            RenderSettings.reflectionIntensity = look.ReflectionIntensity;
            RenderSettings.fogColor = Color.Lerp(look.FogColor, new Color(fogTint.r, fogTint.g, fogTint.b, 1f), fogTint.a);
            RenderSettings.fogStartDistance = look.FogStart * fogScale;
            RenderSettings.fogEndDistance = look.FogEnd * fogScale;
            if (_sun != null)
            {
                _sun.color = look.SunColor;
                _sun.intensity = look.SunIntensity * sunScale;
                Vector3 euler = _baseSunEuler;
                if (hasLevel && !isNight && level.DaySunElevation > 0f) euler.x = level.DaySunElevation;
                _sun.transform.rotation = Quaternion.Euler(euler);
            }

            ApplyParticleLevel(hasLevel ? level : default, hasLevel);

            bool isRain = _environment.Weather == Weather.Rain;
            if (_rainGrade != null && _rainGrade.activeSelf != isRain) _rainGrade.SetActive(isRain);
            Shader.SetGlobalFloat(RainIntensityId, isRain && hasLevel ? level.RainIntensity : 0f);
            Shader.SetGlobalFloat(PuddleAmountId, isRain && hasLevel ? level.PuddleAmount : 0f);
            Shader.SetGlobalFloat(WetnessId, isRain ? 1f : 0f);
            Shader.SetGlobalFloat(SnowCoverId, _environment.Weather == Weather.Snow && hasLevel ? level.SnowCover : 0f);

            DynamicGI.UpdateEnvironment();
            if (_surfaces != null) _surfaces.Apply(_environment.Weather, _environment.IsRainbowRoad);
            UpdateEffects();

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

        private LevelLook SelectLevel(out bool found)
        {
            for (int i = 0; i < _levels.Length; ++i)
            {
                if (_levels[i].Weather == _environment.Weather && _levels[i].Level == _environment.Level)
                {
                    found = true;
                    return _levels[i];
                }
            }
            found = false;
            return default;
        }

        private void ApplyParticleLevel(LevelLook level, bool hasLevel)
        {
            float rateScale = hasLevel ? level.ParticleRateScale : 1f;
            float sizeScale = hasLevel ? level.ParticleSizeScale : 1f;
            float fallScale = hasLevel ? level.ParticleFallScale : 1f;
            _windX = hasLevel ? level.WindX : 0f;

            for (int i = 0; i < _effects.Length && i < _effectBases.Length; ++i)
            {
                ParticleSystem system = _effects[i].System;
                if (system == null || _effects[i].Weather != _environment.Weather) continue;
                EffectBase source = _effectBases[i];
                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTimeMultiplier = source.Rate * rateScale;
                ParticleSystem.MainModule main = system.main;
                main.maxParticles = Mathf.CeilToInt(source.MaxParticles * Mathf.Max(1f, rateScale));
                main.startSizeMultiplier = source.Size * sizeScale;
                ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
                velocity.yMultiplier = source.FallY * fallScale;
                // 바람에 날려 화면 밖으로 빠지지 않도록 생성 영역을 바람 반대쪽으로 수명 절반만큼 옮긴다.
                ParticleSystem.ShapeModule shape = system.shape;
                shape.position = source.ShapePosition + new Vector3(-_windX * source.Lifetime * 0.5f, 0f, 0f);
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
