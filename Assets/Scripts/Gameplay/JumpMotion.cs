using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 플레이어 차량 점프. 땅에 있을 때만 시작하며, Duration 동안 최고 Height의 포물선을 그린다.
    /// </summary>
    public sealed class JumpMotion
    {
        private readonly float _height;
        private readonly float _duration;
        private float _elapsed;

        public bool IsAirborne { get; private set; }
        public float Height { get; private set; }
        /// <summary>점프 진행도 0~1(땅에 있으면 0).</summary>
        public float Progress => IsAirborne ? _elapsed / _duration : 0f;

        public JumpMotion(float height, float duration)
        {
            if (height <= 0f) throw new ArgumentOutOfRangeException(nameof(height));
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));
            _height = height;
            _duration = duration;
        }

        public bool TryJump()
        {
            if (IsAirborne) return false;
            IsAirborne = true;
            _elapsed = 0f;
            Height = 0f;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsAirborne || deltaTime <= 0f) return;

            _elapsed += deltaTime;
            if (_elapsed >= _duration)
            {
                IsAirborne = false;
                _elapsed = 0f;
                Height = 0f;
                return;
            }

            float p = _elapsed / _duration;
            Height = 4f * _height * p * (1f - p);
        }
    }
}
