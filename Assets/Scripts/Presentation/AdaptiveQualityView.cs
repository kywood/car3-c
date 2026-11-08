using System;
using Car3C.Gameplay;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Car3C.Presentation
{
    /// <summary>
    /// 최소 FPS(기본 60)를 지키도록 렌더 해상도·그림자 거리를 자동으로 조절한다(AdaptiveQualityController가 단계를 정함).
    /// URP 설정 에셋 값을 실행 중에만 바꾸고, 끝나면(OnDestroy) 원래 값으로 돌려놓는다.
    /// </summary>
    public sealed class AdaptiveQualityView : MonoBehaviour
    {
        [Serializable]
        private struct QualityLevel
        {
            [Range(0.3f, 1f)] public float RenderScale;
            [Tooltip("0이면 그림자 끔")] public float ShadowDistance;
        }

        [SerializeField] private bool _enabled = true;
        [SerializeField] private float _minFps = 60f;
        [SerializeField] private int _startLevel = 1;
        [Tooltip("0 = 최고 품질, 아래로 갈수록 가벼움")]
        [SerializeField] private QualityLevel[] _levels =
        {
            new QualityLevel { RenderScale = 0.9f, ShadowDistance = 60f },
            new QualityLevel { RenderScale = 0.8f, ShadowDistance = 60f },
            new QualityLevel { RenderScale = 0.7f, ShadowDistance = 45f },
            new QualityLevel { RenderScale = 0.6f, ShadowDistance = 30f },
            new QualityLevel { RenderScale = 0.5f, ShadowDistance = 0f },
        };

        private readonly FrameStats _stats = new FrameStats();
        private AdaptiveQualityController _controller;
        private UniversalRenderPipelineAsset _asset;
        private float _originalScale;
        private float _originalShadowDistance;

        public bool IsActive => _controller != null;
        public int Level => _controller != null ? _controller.Level : -1;
        public float RenderScale => _asset != null ? _asset.renderScale : 1f;

        private void Start()
        {
            _asset = UniversalRenderPipeline.asset;
            if (!_enabled || _asset == null || _levels.Length == 0) return;
            _originalScale = _asset.renderScale;
            _originalShadowDistance = _asset.shadowDistance;
            _controller = new AdaptiveQualityController(_levels.Length - 1, _minFps, startLevel: _startLevel);
            Apply();
        }

        private void Update()
        {
            if (_controller == null) return;
            float dt = Time.unscaledDeltaTime;
            _stats.AddSample(dt);
            if (_controller.Update(_stats.AverageFps, Application.targetFrameRate, dt)) Apply();
        }

        private void OnDestroy()
        {
            if (_controller == null || _asset == null) return;
            _asset.renderScale = _originalScale;
            _asset.shadowDistance = _originalShadowDistance;
        }

        private void Apply()
        {
            QualityLevel level = _levels[_controller.Level];
            _asset.renderScale = level.RenderScale;
            _asset.shadowDistance = level.ShadowDistance;
        }
    }
}
