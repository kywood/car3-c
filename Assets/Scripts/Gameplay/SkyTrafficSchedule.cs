using System;

namespace Car3C.Gameplay
{
    /// <summary>하늘을 지나가는 비행기 한 번의 비행.</summary>
    public readonly struct SkyFlight
    {
        public SkyFlight(long index, float progress, bool leftToRight, float altitude, float distanceAhead, float climb)
        {
            Index = index;
            Progress = progress;
            LeftToRight = leftToRight;
            Altitude = altitude;
            DistanceAhead = distanceAhead;
            Climb = climb;
        }

        /// <summary>몇 번째 비행인지(새 비행이면 값이 바뀐다).</summary>
        public long Index { get; }
        /// <summary>0 = 화면 한쪽 끝, 1 = 반대쪽 끝.</summary>
        public float Progress { get; }
        public bool LeftToRight { get; }
        /// <summary>고도(m).</summary>
        public float Altitude { get; }
        /// <summary>차 앞쪽 거리(m).</summary>
        public float DistanceAhead { get; }
        /// <summary>지나가는 동안 높이 변화(m, 음수면 내려감).</summary>
        public float Climb { get; }
    }

    /// <summary>
    /// 하늘을 가로지르는 비행기 일정. 시간만으로 상태가 정해져(씨앗 값이 같으면 모든 클라이언트가 같다) 네트워크 동기화가 쉽다.
    /// 비행은 Interval마다 하나씩 시작하고, 각 비행의 방향·고도·거리는 씨앗 값과 번호로 정한다.
    /// </summary>
    public sealed class SkyTrafficSchedule
    {
        private readonly int _seed;

        public SkyTrafficSchedule(int seed, float intervalSeconds = 34f, float flightSeconds = 24f)
        {
            if (intervalSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            if (flightSeconds <= 0f || flightSeconds > intervalSeconds) throw new ArgumentOutOfRangeException(nameof(flightSeconds));
            _seed = seed;
            IntervalSeconds = intervalSeconds;
            FlightSeconds = flightSeconds;
        }

        public float IntervalSeconds { get; }
        public float FlightSeconds { get; }

        /// <summary>time(초)에 날고 있는 비행이 있으면 true. 첫 비행은 시작 후 FirstDelay 뒤.</summary>
        public bool TryGetFlight(double time, out SkyFlight flight)
        {
            const double firstDelay = 6d;
            double t = time - firstDelay;
            if (t < 0d)
            {
                flight = default;
                return false;
            }

            long index = (long)Math.Floor(t / IntervalSeconds);
            double local = t - index * IntervalSeconds;
            if (local >= FlightSeconds)
            {
                flight = default;
                return false;
            }

            flight = new SkyFlight(
                index,
                (float)(local / FlightSeconds),
                Hash(index, 1) < 0.5f,
                // 카메라가 살짝 내려다봐 지평선 위 약 10°까지만 보이므로 멀리·낮게(지평선 바로 위) 지나간다.
                55f + Hash(index, 2) * 35f,
                330f + Hash(index, 3) * 90f,
                (Hash(index, 4) - 0.5f) * 30f);
            return true;
        }

        private float Hash(long index, int salt)
        {
            unchecked
            {
                ulong h = (ulong)_seed * 0x9E3779B97F4A7C15UL ^ (ulong)index * 0xBF58476D1CE4E5B9UL ^ (ulong)salt * 0x94D049BB133111EBUL;
                h ^= h >> 31;
                h *= 0xD6E8FEB86659FD93UL;
                h ^= h >> 32;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
