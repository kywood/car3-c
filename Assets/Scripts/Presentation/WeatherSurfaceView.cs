using System;
using System.Collections.Generic;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 날씨에 따라 바닥 재질을 바꾼다(맑음: 마른 노면, 비: 젖은 노면, 눈: 눈 덮인 노면).
    /// 무지개 도로 모드에서는 무지개 재질이 있는 면(차도)을 무지개로 바꾸고 색을 흘려보낸다.
    /// 머티리얼 에셋은 수정하지 않는다. 무지개 색 흐름은 런타임 복제본에서만 움직인다.
    /// </summary>
    public sealed class WeatherSurfaceView : MonoBehaviour
    {
        [Serializable]
        private struct SurfaceVariant
        {
            [SerializeField] private Material _dry;
            [SerializeField] private Material _wet;
            [SerializeField] private Material _snow;
            [SerializeField] private Material _rainbow;

            public Material Dry => _dry;
            public Material Rainbow => _rainbow;

            public Material For(Weather weather)
            {
                switch (weather)
                {
                    case Weather.Rain: return _wet != null ? _wet : _dry;
                    case Weather.Snow: return _snow != null ? _snow : _dry;
                    default: return _dry;
                }
            }
        }

        [SerializeField] private SurfaceVariant[] _variants = Array.Empty<SurfaceVariant>();
        [SerializeField] private Transform[] _roots = Array.Empty<Transform>();
        [SerializeField] private float _rainbowFlowSpeed = 0.35f;

        private readonly List<Renderer> _renderers = new List<Renderer>(256);
        private readonly List<int> _variantIndices = new List<int>(256);
        private readonly List<Renderer> _buffer = new List<Renderer>(512);
        private Material[] _rainbowInstances;
        private bool _rainbowActive;
        private float _rainbowOffset;

        /// <summary>풀링된 도로·배경이 모두 만들어진 뒤 한 번 호출해 교체 대상 렌더러를 모은다.</summary>
        public void Collect()
        {
            _renderers.Clear();
            _variantIndices.Clear();
            for (int r = 0; r < _roots.Length; ++r)
            {
                if (_roots[r] == null) continue;
                _roots[r].GetComponentsInChildren(true, _buffer);
                for (int i = 0; i < _buffer.Count; ++i)
                {
                    int variant = FindVariant(_buffer[i].sharedMaterial);
                    if (variant < 0) continue;
                    _renderers.Add(_buffer[i]);
                    _variantIndices.Add(variant);
                }
            }

            if (_rainbowInstances != null) return;
            _rainbowInstances = new Material[_variants.Length];
            for (int v = 0; v < _variants.Length; ++v)
                if (_variants[v].Rainbow != null) _rainbowInstances[v] = new Material(_variants[v].Rainbow);
        }

        public void Apply(Weather weather, bool rainbowRoad)
        {
            _rainbowActive = rainbowRoad;
            for (int i = 0; i < _renderers.Count; ++i)
            {
                int v = _variantIndices[i];
                Material target = rainbowRoad && _rainbowInstances[v] != null ? _rainbowInstances[v] : _variants[v].For(weather);
                if (_renderers[i].sharedMaterial != target) _renderers[i].sharedMaterial = target;
            }
        }

        private void Update()
        {
            if (!_rainbowActive || _rainbowInstances == null) return;

            _rainbowOffset = Mathf.Repeat(_rainbowOffset + _rainbowFlowSpeed * Time.deltaTime, 1f);
            for (int v = 0; v < _rainbowInstances.Length; ++v)
            {
                Material m = _rainbowInstances[v];
                if (m == null) continue;
                Vector2 offset = new Vector2(0f, _rainbowOffset);
                m.SetTextureOffset("_BaseMap", offset);
                m.SetTextureOffset("_EmissionMap", offset);
            }
        }

        private void OnDestroy()
        {
            if (_rainbowInstances == null) return;
            for (int v = 0; v < _rainbowInstances.Length; ++v)
                if (_rainbowInstances[v] != null) Destroy(_rainbowInstances[v]);
        }

        private int FindVariant(Material material)
        {
            if (material == null) return -1;
            for (int v = 0; v < _variants.Length; ++v)
                if (_variants[v].Dry == material) return v;
            return -1;
        }
    }
}
