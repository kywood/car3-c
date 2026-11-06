using System;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 밤에만 켜지는 조명 표시(가로등 불빛, 전조등, 차량 후미등).
    /// 오브젝트를 켜고 끄거나, 모델의 특정 재질 슬롯(예: 후미등 렌즈)을 발광 재질로 바꿔 끼운다.
    /// 공유 머티리얼 에셋 자체는 수정하지 않는다.
    /// </summary>
    public sealed class NightLight : MonoBehaviour
    {
        [Serializable]
        private struct SlotSwap
        {
            [SerializeField] private Renderer _renderer;
            [SerializeField] private int _slot;
            [SerializeField] private Material _nightMaterial;

            public Renderer Renderer => _renderer;
            public int Slot => _slot;
            public Material NightMaterial => _nightMaterial;
        }

        [SerializeField] private GameObject _target;
        [SerializeField] private SlotSwap[] _slotSwaps = Array.Empty<SlotSwap>();

        private Material[] _dayMaterials;
        private bool _isOn;

        public void SetOn(bool isOn)
        {
            if (_target != null && _target.activeSelf != isOn) _target.SetActive(isOn);
            if (_slotSwaps.Length == 0 || (_dayMaterials != null && _isOn == isOn)) return;

            CacheDayMaterials();
            _isOn = isOn;
            for (int i = 0; i < _slotSwaps.Length; ++i)
            {
                Renderer renderer = _slotSwaps[i].Renderer;
                if (renderer == null) continue;
                Material[] materials = renderer.sharedMaterials;
                int slot = _slotSwaps[i].Slot;
                if (slot < 0 || slot >= materials.Length) continue;
                materials[slot] = isOn ? _slotSwaps[i].NightMaterial : _dayMaterials[i];
                renderer.sharedMaterials = materials;
            }
        }

        private void CacheDayMaterials()
        {
            if (_dayMaterials != null) return;
            _dayMaterials = new Material[_slotSwaps.Length];
            for (int i = 0; i < _slotSwaps.Length; ++i)
            {
                Renderer renderer = _slotSwaps[i].Renderer;
                int slot = _slotSwaps[i].Slot;
                if (renderer != null && slot >= 0 && slot < renderer.sharedMaterials.Length)
                    _dayMaterials[i] = renderer.sharedMaterials[slot];
            }
        }
    }
}
