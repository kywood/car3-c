using System;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 가속 발판 표시. 몇 개의 발판 오브젝트를 돌려 쓰며(풀) BoostPadField가 정한 위치에 놓고,
    /// 화살표 무늬가 앞쪽으로 흐르게 한다(런타임 복제 머티리얼만 움직이므로 에셋은 바뀌지 않는다).
    /// </summary>
    public sealed class BoostPadView : MonoBehaviour
    {
        [SerializeField] private Renderer[] _pads = Array.Empty<Renderer>();
        [SerializeField] private float _surfaceHeight = 0.035f;
        [SerializeField] private float _flowSpeed = 1.6f;
        [SerializeField] private float _visibleBehind = 20f;

        private BoostPadField _field;
        private SpeedController _speed;
        private Material _runtimeMaterial;
        private float _flow;

        public void Initialize(BoostPadField field, SpeedController speed)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            if (_pads.Length == 0) return;

            _runtimeMaterial = new Material(_pads[0].sharedMaterial);
            for (int i = 0; i < _pads.Length; ++i) _pads[i].sharedMaterial = _runtimeMaterial;
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }

        private void LateUpdate()
        {
            if (_field == null || _pads.Length == 0) return;

            double distance = _speed.Distance;
            long first = _field.GetFirstIndexFrom(distance - _visibleBehind);
            for (int i = 0; i < _pads.Length; ++i)
            {
                long index = first + i;
                float z = (float)(_field.GetPadDistance(index) - distance);
                Transform pad = _pads[i].transform;
                pad.localPosition = new Vector3(_field.GetPadX(index), _surfaceHeight, z + _field.Length * 0.5f);
                pad.localScale = new Vector3(_field.Width, _field.Length, 1f);
            }

            _flow = Mathf.Repeat(_flow - _flowSpeed * Time.deltaTime, 1f);
            _runtimeMaterial.SetTextureOffset("_BaseMap", new Vector2(0f, _flow));
        }
    }
}
