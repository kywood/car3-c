using Car3C.Gameplay;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Car3C.Presentation
{
    /// <summary>
    /// FPS 등 성능 정보 표시. FPS 버튼(또는 F 키)으로 켜고 끄며, 켠 상태는 이 기기에 기억한다.
    /// 글자는 0.25초마다 재사용 StringBuilder로 갱신한다(매 프레임 문자열을 만들지 않음).
    /// 그리기 호출·삼각형 수는 그 값을 주는 빌드(에디터·개발 빌드)에서만 보인다.
    /// </summary>
    public sealed class PerformanceHudView : MonoBehaviour
    {
        private const string PrefsKey = "car3c.perfHud";

        [SerializeField] private UnityEngine.UI.Button _toggleButton;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _text;
        [Tooltip("선택: 자동 품질 단계 표시")]
        [SerializeField] private AdaptiveQualityView _quality;
        [SerializeField] private float _refreshSeconds = 0.25f;
        [SerializeField] private Color _offColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color _onColor = new Color(1f, 0.6f, 0.15f, 0.9f);

        private readonly FrameStats _stats = new FrameStats();
        private ProfilerRecorder _batches;
        private ProfilerRecorder _triangles;
        private float _refreshTimer;
        private bool _visible;
        private readonly System.Text.StringBuilder _builder = new System.Text.StringBuilder(160);

        private void OnEnable()
        {
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            if (_toggleButton != null) _toggleButton.onClick.AddListener(Toggle);
            SetVisible(SafeGetPrefs());
        }

        private void OnDisable()
        {
            if (_toggleButton != null) _toggleButton.onClick.RemoveListener(Toggle);
            _batches.Dispose();
            _triangles.Dispose();
        }

        public void Toggle()
        {
            SetVisible(!_visible);
            try { PlayerPrefs.SetInt(PrefsKey, _visible ? 1 : 0); } catch { /* 저장 실패는 무시(표시만 바뀜) */ }
        }

        private void Update()
        {
            _stats.AddSample(Time.unscaledDeltaTime);
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame) Toggle();

            if (!_visible || _text == null) return;
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = _refreshSeconds;

            float memoryMb = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            int target = Application.targetFrameRate;
            _builder.Clear();
            _builder.Append("FPS ").Append(Mathf.RoundToInt(_stats.AverageFps)).Append("  ").Append(_stats.AverageMs.ToString("0.0")).Append("ms\n");
            _builder.Append("Low ").Append(Mathf.RoundToInt(_stats.LowFps)).Append("  Worst ").Append(_stats.WorstMs.ToString("0.0")).Append("ms\n");
            _builder.Append("Target ").Append(target > 0 ? target.ToString() : "-").Append("  ").Append(Screen.width).Append('x').Append(Screen.height).Append('\n');
            if (_quality != null && _quality.IsActive)
                _builder.Append("Quality L").Append(_quality.Level).Append("  Scale ").Append(_quality.RenderScale.ToString("0.00")).Append('\n');
            _builder.Append("Mem ").Append(Mathf.RoundToInt(memoryMb)).Append("MB");
            if (_batches.Valid && _triangles.Valid)
                _builder.Append("  Draws ").Append(_batches.LastValue).Append("  Tris ").Append(_triangles.LastValue / 1000).Append('k');
            _text.SetText(_builder);
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            _refreshTimer = 0f;
            if (_panel != null && _panel.activeSelf != visible) _panel.SetActive(visible);
            if (_toggleButton != null && _toggleButton.targetGraphic != null) _toggleButton.targetGraphic.color = visible ? _onColor : _offColor;
        }

        private static bool SafeGetPrefs()
        {
            try { return PlayerPrefs.GetInt(PrefsKey, 0) == 1; } catch { return false; }
        }
    }
}
