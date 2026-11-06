using Car3C.Gameplay.Launch;
using TMPro;
using UnityEngine;

namespace Car3C.Presentation.Launch
{
    /// <summary>
    /// HUD 상단의 발사 카운트다운 표시(T-10 … IGNITION, LIFTOFF). 발사 이벤트가 없을 때는 숨긴다.
    /// </summary>
    public sealed class LaunchCountdownView : MonoBehaviour, ILaunchSequenceListener
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _liftoffMessageSeconds = 3f;

        private float _hideAt = -1f;

        private void Awake()
        {
            _label.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_hideAt > 0f && Time.time >= _hideAt)
            {
                _hideAt = -1f;
                _label.gameObject.SetActive(false);
            }
        }

        public void OnCountdownTick(int secondsLeft)
        {
            _label.gameObject.SetActive(true);
            _label.SetText("LAUNCH  T-{0}", secondsLeft);
        }

        public void OnLaunchPhaseChanged(LaunchPhase phase)
        {
            switch (phase)
            {
                case LaunchPhase.Ignition:
                    Show("IGNITION");
                    break;
                case LaunchPhase.Ascent:
                    Show("LIFTOFF!");
                    _hideAt = Time.time + _liftoffMessageSeconds;
                    break;
                case LaunchPhase.Idle:
                case LaunchPhase.Complete:
                    _label.gameObject.SetActive(false);
                    break;
            }
        }

        private void Show(string text)
        {
            _label.gameObject.SetActive(true);
            _label.SetText(text);
        }
    }
}
