using System;
using Car3C.Gameplay;
using TMPro;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 하늘/비/눈/야간/무지개 도로 전환 버튼. 비·눈 버튼은 누를 때마다 다음 단계로, 하늘 버튼은 맑은 하늘 종류를 바꾼다.
    /// 켜진 버튼은 강조 색, 글자는 현재 단계 이름으로 표시한다.
    /// </summary>
    public sealed class EnvironmentButtonsView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button _skyButton;
        [SerializeField] private UnityEngine.UI.Button _rainButton;
        [SerializeField] private UnityEngine.UI.Button _snowButton;
        [SerializeField] private UnityEngine.UI.Button _nightButton;
        [SerializeField] private UnityEngine.UI.Button _rainbowButton;
        [SerializeField] private Color _offColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color _onColor = new Color(1f, 0.6f, 0.15f, 0.9f);

        [Header("Labels")]
        [Tooltip("맑은 하늘 단계별 이름")]
        [SerializeField] private string[] _skyLabels = { "SUNNY", "CLOUDS" };
        [Tooltip("[0] = 꺼짐, [1..] = 비 단계별 이름")]
        [SerializeField] private string[] _rainLabels = { "RAIN", "DRIZZLE", "STORM" };
        [Tooltip("[0] = 꺼짐, [1..] = 눈 단계별 이름")]
        [SerializeField] private string[] _snowLabels = { "SNOW", "LIGHT", "BIG SNOW", "BLIZZARD" };

        private EnvironmentController _environment;

        public void Initialize(EnvironmentController environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            if (_skyButton != null) _skyButton.onClick.AddListener(_environment.CycleSky);
            _rainButton.onClick.AddListener(_environment.CycleRain);
            _snowButton.onClick.AddListener(_environment.CycleSnow);
            _nightButton.onClick.AddListener(_environment.ToggleNight);
            if (_rainbowButton != null) _rainbowButton.onClick.AddListener(_environment.ToggleRainbowRoad);
            _environment.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_environment == null) return;
            if (_skyButton != null) _skyButton.onClick.RemoveListener(_environment.CycleSky);
            _rainButton.onClick.RemoveListener(_environment.CycleRain);
            _snowButton.onClick.RemoveListener(_environment.CycleSnow);
            _nightButton.onClick.RemoveListener(_environment.ToggleNight);
            if (_rainbowButton != null) _rainbowButton.onClick.RemoveListener(_environment.ToggleRainbowRoad);
            _environment.Changed -= Refresh;
        }

        private void Refresh()
        {
            Weather weather = _environment.Weather;
            int level = _environment.Level;

            if (_skyButton != null)
            {
                SetColor(_skyButton, weather == Weather.Clear);
                SetLabel(_skyButton, _skyLabels, _environment.ClearLevel);
            }
            SetColor(_rainButton, weather == Weather.Rain);
            SetLabel(_rainButton, _rainLabels, weather == Weather.Rain ? level + 1 : 0);
            SetColor(_snowButton, weather == Weather.Snow);
            SetLabel(_snowButton, _snowLabels, weather == Weather.Snow ? level + 1 : 0);
            SetColor(_nightButton, _environment.IsNight);
            if (_rainbowButton != null) SetColor(_rainbowButton, _environment.IsRainbowRoad);
        }

        private void SetColor(UnityEngine.UI.Button button, bool isOn)
        {
            if (button.targetGraphic != null) button.targetGraphic.color = isOn ? _onColor : _offColor;
        }

        private static void SetLabel(UnityEngine.UI.Button button, string[] labels, int index)
        {
            if (labels == null || index < 0 || index >= labels.Length) return;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = labels[index];
        }
    }
}
