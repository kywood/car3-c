using System;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 비/눈/야간/무지개 도로 전환 버튼. 누르면 EnvironmentController를 바꾸고, 켜진 버튼은 강조 색으로 표시한다.
    /// </summary>
    public sealed class EnvironmentButtonsView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button _rainButton;
        [SerializeField] private UnityEngine.UI.Button _snowButton;
        [SerializeField] private UnityEngine.UI.Button _nightButton;
        [SerializeField] private UnityEngine.UI.Button _rainbowButton;
        [SerializeField] private Color _offColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color _onColor = new Color(1f, 0.6f, 0.15f, 0.9f);

        private EnvironmentController _environment;

        public void Initialize(EnvironmentController environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _rainButton.onClick.AddListener(_environment.ToggleRain);
            _snowButton.onClick.AddListener(_environment.ToggleSnow);
            _nightButton.onClick.AddListener(_environment.ToggleNight);
            if (_rainbowButton != null) _rainbowButton.onClick.AddListener(_environment.ToggleRainbowRoad);
            _environment.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_environment == null) return;
            _rainButton.onClick.RemoveListener(_environment.ToggleRain);
            _snowButton.onClick.RemoveListener(_environment.ToggleSnow);
            _nightButton.onClick.RemoveListener(_environment.ToggleNight);
            if (_rainbowButton != null) _rainbowButton.onClick.RemoveListener(_environment.ToggleRainbowRoad);
            _environment.Changed -= Refresh;
        }

        private void Refresh()
        {
            SetColor(_rainButton, _environment.Weather == Weather.Rain);
            SetColor(_snowButton, _environment.Weather == Weather.Snow);
            SetColor(_nightButton, _environment.IsNight);
            if (_rainbowButton != null) SetColor(_rainbowButton, _environment.IsRainbowRoad);
        }

        private void SetColor(UnityEngine.UI.Button button, bool isOn)
        {
            if (button.targetGraphic != null) button.targetGraphic.color = isOn ? _onColor : _offColor;
        }
    }
}
