using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// FPS가 목표 아래로 일정 시간 떨어지면 품질 단계를 한 단계 낮추고(숫자 증가), 여유가 일정 시간 이어지면 한 단계 올린다.
    /// 단계마다 실제로 무엇을 바꿀지는 표현 쪽(AdaptiveQualityView)이 정한다. 오르내림이 반복되지 않도록
    /// 내릴 때와 올릴 때의 기준·대기 시간을 다르게 둔다(올리기가 더 느리고 깐깐함).
    /// </summary>
    public sealed class AdaptiveQualityController
    {
        private readonly int _maxLevel;
        private readonly float _minFps;
        private readonly float _dropAfter;
        private readonly float _raiseHeadroom;
        private float _belowTimer;
        private float _aboveTimer;
        private float _cooldown;
        private float _raiseWait;
        private bool _lastWasRaise;

        /// <param name="maxLevel">가장 가벼운 단계 번호(0 = 최고 품질).</param>
        /// <param name="minFps">지키려는 최소 FPS.</param>
        /// <param name="dropAfter">이 시간(초) 동안 계속 minFps 아래면 한 단계 낮춤.</param>
        /// <param name="raiseAfter">이 시간(초) 동안 계속 여유가 있으면 한 단계 올림.</param>
        /// <param name="raiseHeadroom">여유 기준: 평균 FPS가 minFps × 이 값 이상.</param>
        public AdaptiveQualityController(int maxLevel, float minFps = 60f, float dropAfter = 2f, float raiseAfter = 8f, float raiseHeadroom = 1.15f, int startLevel = 0)
        {
            if (maxLevel < 0) throw new ArgumentOutOfRangeException(nameof(maxLevel));
            if (minFps <= 0f) throw new ArgumentOutOfRangeException(nameof(minFps));
            _maxLevel = maxLevel;
            _minFps = minFps;
            _dropAfter = dropAfter;
            _raiseHeadroom = raiseHeadroom;
            Level = Math.Max(0, Math.Min(maxLevel, startLevel));
            _raiseWait = raiseAfter;
        }

        /// <summary>현재 단계(0 = 최고 품질).</summary>
        public int Level { get; private set; }

        /// <summary>단계가 바뀌면 true.</summary>
        /// <param name="averageFps">최근 평균 FPS.</param>
        /// <param name="targetFps">화면 주사율 등 현재 목표 FPS. 60보다 높아도 기준은 minFps.</param>
        public bool Update(float averageFps, float targetFps, float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return false;
            if (_cooldown > 0f)
            {
                _cooldown -= deltaSeconds;
                return false;
            }

            // 목표가 60보다 낮게 묶여 있으면(예: 30 고정) 그 목표를 기준으로 본다.
            float floor = Math.Min(_minFps, targetFps > 0f ? targetFps : _minFps) * 0.95f;
            if (averageFps < floor)
            {
                _belowTimer += deltaSeconds;
                _aboveTimer = 0f;
            }
            else if (averageFps >= _minFps * _raiseHeadroom || (targetFps > 0f && averageFps >= targetFps * 0.98f && targetFps >= _minFps))
            {
                _aboveTimer += deltaSeconds;
                _belowTimer = 0f;
            }
            else
            {
                _belowTimer = 0f;
                _aboveTimer = 0f;
            }

            if (_belowTimer >= _dropAfter && Level < _maxLevel)
            {
                // 올린 직후 다시 떨어졌으면 그 단계는 버거운 것: 다음 올리기는 두 배 더 기다린다(최대 64초).
                if (_lastWasRaise) _raiseWait = Math.Min(_raiseWait * 2f, 64f);
                ++Level;
                _lastWasRaise = false;
                Reset();
                return true;
            }
            if (_aboveTimer >= _raiseWait && Level > 0)
            {
                --Level;
                _lastWasRaise = true;
                Reset();
                return true;
            }
            return false;
        }

        private void Reset()
        {
            _belowTimer = 0f;
            _aboveTimer = 0f;
            // 바뀐 직후의 프레임 변화가 평균에 반영될 때까지 잠시 기다린다.
            _cooldown = 1.5f;
        }
    }
}
