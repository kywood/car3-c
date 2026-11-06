using System;
using UnityEngine;

namespace Car3C.Gameplay.Launch
{
    /// <summary>
    /// 발사 연출 시간표(초)와 상승 가속도. 인스펙터/에셋에서 조정하고, 테스트에서는 생성자로 만든다.
    /// </summary>
    [Serializable]
    public sealed class LaunchTimeline
    {
        [SerializeField] private float _countdownSeconds = 10f;
        [SerializeField] private float _ignitionSeconds = 3f;
        [SerializeField] private float _ascentSeconds = 30f;
        [SerializeField] private float _ascentAcceleration = 3f;
        [SerializeField] private float _shakeFadeSeconds = 8f;

        public float CountdownSeconds => _countdownSeconds;
        public float IgnitionSeconds => _ignitionSeconds;
        public float AscentSeconds => _ascentSeconds;
        public float AscentAcceleration => _ascentAcceleration;
        public float ShakeFadeSeconds => _shakeFadeSeconds;
        public float TotalSeconds => _countdownSeconds + _ignitionSeconds + _ascentSeconds;

        public LaunchTimeline()
        {
        }

        public LaunchTimeline(float countdownSeconds, float ignitionSeconds, float ascentSeconds, float ascentAcceleration, float shakeFadeSeconds)
        {
            if (countdownSeconds < 0f || ignitionSeconds <= 0f || ascentSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(countdownSeconds));
            _countdownSeconds = countdownSeconds;
            _ignitionSeconds = ignitionSeconds;
            _ascentSeconds = ascentSeconds;
            _ascentAcceleration = ascentAcceleration;
            _shakeFadeSeconds = shakeFadeSeconds;
        }
    }
}
