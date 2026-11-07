using System;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 고정된 바다 평면(Car3C/Water)의 물결 무늬를 주행 거리만큼 흘려보낸다.
    /// 머티리얼 에셋은 바꾸지 않고 MaterialPropertyBlock으로 오프셋만 넘긴다.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class SeaView : MonoBehaviour
    {
        private static readonly int FlowOffsetId = Shader.PropertyToID("_FlowOffset");
        private static readonly int TileLengthId = Shader.PropertyToID("_TileLength");

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private SpeedController _speed;
        private double _tileLength;

        public void Initialize(SpeedController speed)
        {
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            _renderer = GetComponent<Renderer>();
            Material material = _renderer.sharedMaterial;
            if (material == null || !material.HasProperty(TileLengthId))
                throw new InvalidOperationException($"{nameof(SeaView)}: Car3C/Water 머티리얼이 필요합니다.");
            _tileLength = material.GetFloat(TileLengthId);
            _block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (_speed == null) return;

            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(FlowOffsetId, WaterFlow.GetOffset(_speed.Distance, _tileLength));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
