using System;

namespace Car3C.Presentation
{
    /// <summary>
    /// HUD 팝업 연출 값 계산: 처음에 크게 튀어나왔다가 제자리 크기로 줄고, 끝부분에서 서서히 사라진다.
    /// </summary>
    public sealed class PopupAnimation
    {
        private readonly float _duration;
        private readonly float _punchScale;
        private readonly float _punchTime;
        private readonly float _fadeTime;
        private float _elapsed;

        public bool IsVisible => _elapsed < _duration;
        public float Scale { get; private set; } = 1f;
        public float Alpha { get; private set; }

        public PopupAnimation(float duration, float punchScale, float punchTime, float fadeTime)
        {
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));
            if (punchTime <= 0f || punchTime > duration) throw new ArgumentOutOfRangeException(nameof(punchTime));
            if (fadeTime <= 0f || fadeTime > duration) throw new ArgumentOutOfRangeException(nameof(fadeTime));
            _duration = duration;
            _punchScale = punchScale;
            _punchTime = punchTime;
            _fadeTime = fadeTime;
            _elapsed = duration;
        }

        public void Play()
        {
            _elapsed = 0f;
            Evaluate();
        }

        public void Tick(float deltaTime)
        {
            if (!IsVisible) return;
            _elapsed = Math.Min(_duration, _elapsed + Math.Max(0f, deltaTime));
            Evaluate();
        }

        private void Evaluate()
        {
            float punch = Math.Min(1f, _elapsed / _punchTime);
            Scale = _punchScale + (1f - _punchScale) * punch;
            float remaining = _duration - _elapsed;
            Alpha = remaining >= _fadeTime ? 1f : Math.Max(0f, remaining / _fadeTime);
        }
    }
}
