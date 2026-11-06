using System;
using Car3C.Gameplay;
using TMPro;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 속도·거리·속도 게이지와 근접 추월/충돌 팝업을 표시한다.
    /// 텍스트는 값이 바뀔 때만 갱신하고 SetText 숫자 포맷을 써서 매 프레임 문자열을 만들지 않는다.
    /// </summary>
    public sealed class HudView : MonoBehaviour, ITrafficListener
    {
        private const float MetersPerSecondToKmh = 3.6f;

        [SerializeField] private TMP_Text _speedText;
        [SerializeField] private TMP_Text _distanceText;
        [SerializeField] private RectTransform _speedBarFill;
        [SerializeField] private RectTransform _popupRoot;
        [SerializeField] private CanvasGroup _popupGroup;
        [SerializeField] private TMP_Text _popupText;
        [SerializeField] private Color _nearMissColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _crashColor = new Color(1f, 0.3f, 0.25f);
        [SerializeField] private float _popupDuration = 0.9f;
        [SerializeField] private float _popupPunchScale = 1.6f;

        private DriveSimulation _simulation;
        private PopupAnimation _popup;
        private int _shownSpeed = -1;
        private long _shownDistance = -1;

        public void Initialize(DriveSimulation simulation)
        {
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _popup = new PopupAnimation(_popupDuration, _popupPunchScale, _popupDuration * 0.2f, _popupDuration * 0.35f);
            _simulation.Traffic.AddListener(this);
            ApplyPopup();
            Refresh();
        }

        private void OnDestroy()
        {
            _simulation?.Traffic.RemoveListener(this);
        }

        private void LateUpdate()
        {
            if (_simulation == null) return;
            Refresh();
            _popup.Tick(Time.deltaTime);
            ApplyPopup();
        }

        private void Refresh()
        {
            SpeedController speed = _simulation.Speed;

            int kmh = Mathf.RoundToInt(speed.CurrentSpeed * MetersPerSecondToKmh);
            if (kmh != _shownSpeed)
            {
                _shownSpeed = kmh;
                _speedText.SetText("{0}", kmh);
            }

            long meters = (long)speed.Distance;
            if (meters != _shownDistance)
            {
                _shownDistance = meters;
                _distanceText.SetText("{0} m", meters);
            }

            Vector2 anchorMax = _speedBarFill.anchorMax;
            anchorMax.x = Mathf.Clamp01(speed.NormalizedSpeed);
            _speedBarFill.anchorMax = anchorMax;
        }

        private void ShowPopup(Color color)
        {
            _popupText.color = color;
            _popup.Play();
            ApplyPopup();
        }

        private void ApplyPopup()
        {
            _popupGroup.alpha = _popup.Alpha;
            _popupRoot.localScale = Vector3.one * _popup.Scale;
        }

        void ITrafficListener.OnCarSpawned(TrafficCar car)
        {
        }

        void ITrafficListener.OnCarDespawned(TrafficCar car)
        {
        }

        void ITrafficListener.OnNearMiss(TrafficCar car)
        {
            int combo = _simulation.Speed.Combo;
            if (combo > 1) _popupText.SetText("NEAR MISS x{0}", combo);
            else _popupText.SetText("NEAR MISS");
            ShowPopup(_nearMissColor);
        }

        void ITrafficListener.OnCollision(TrafficCar car)
        {
            _popupText.SetText("CRASH");
            ShowPopup(_crashColor);
        }
    }
}
