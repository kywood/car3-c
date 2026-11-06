namespace Car3C.Gameplay.Launch
{
    /// <summary>
    /// 특정 시각의 발사 상태. 시작 시각과 현재 시각만으로 계산되므로 네트워크에서 같은 값을 재현할 수 있다.
    /// </summary>
    public readonly struct LaunchSnapshot
    {
        public LaunchPhase Phase { get; }
        /// <summary>카운트다운 남은 정수 초(T-10 → 10). 카운트다운이 아니면 0.</summary>
        public int CountdownSeconds { get; }
        /// <summary>발사대 기준 상승 높이(m).</summary>
        public float Altitude { get; }
        /// <summary>엔진 출력 0~1. 화염·연기 세기에 쓴다.</summary>
        public float Thrust { get; }
        /// <summary>카메라 흔들림 0~1(발사장과의 거리 감쇠는 뷰에서 적용).</summary>
        public float Shake { get; }

        public LaunchSnapshot(LaunchPhase phase, int countdownSeconds, float altitude, float thrust, float shake)
        {
            Phase = phase;
            CountdownSeconds = countdownSeconds;
            Altitude = altitude;
            Thrust = thrust;
            Shake = shake;
        }
    }
}
