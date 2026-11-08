using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 최근 window초 동안의 프레임 시간으로 평균 FPS·평균 프레임 시간·가장 느린 프레임을 계산한다(성능 표시용).
    /// 고정 크기 원형 버퍼라 매 프레임 메모리를 할당하지 않는다.
    /// </summary>
    public sealed class FrameStats
    {
        private readonly float[] _samples;
        private readonly float _window;
        private int _head;
        private int _count;

        public FrameStats(float windowSeconds = 1f, int capacity = 240)
        {
            if (windowSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(windowSeconds));
            if (capacity < 2) throw new ArgumentOutOfRangeException(nameof(capacity));
            _window = windowSeconds;
            _samples = new float[capacity];
        }

        /// <summary>평균 FPS(창 안 프레임 수 / 창 안 시간).</summary>
        public float AverageFps { get; private set; }
        /// <summary>평균 프레임 시간(ms).</summary>
        public float AverageMs { get; private set; }
        /// <summary>창 안에서 가장 느린 프레임 시간(ms).</summary>
        public float WorstMs { get; private set; }
        /// <summary>가장 느린 프레임을 FPS로 환산한 값(끊김 정도).</summary>
        public float LowFps => WorstMs > 0f ? 1000f / WorstMs : 0f;

        public void AddSample(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            _samples[_head] = deltaSeconds;
            _head = (_head + 1) % _samples.Length;
            if (_count < _samples.Length) ++_count;
            Recalculate();
        }

        private void Recalculate()
        {
            float total = 0f, worst = 0f;
            int used = 0;
            // 가장 최근 프레임부터 window초가 찰 때까지만 센다.
            for (int i = 0; i < _count; ++i)
            {
                int index = (_head - 1 - i + _samples.Length) % _samples.Length;
                float dt = _samples[index];
                total += dt;
                if (dt > worst) worst = dt;
                ++used;
                if (total >= _window) break;
            }
            AverageFps = total > 0f ? used / total : 0f;
            AverageMs = used > 0 ? total / used * 1000f : 0f;
            WorstMs = worst * 1000f;
        }
    }
}
