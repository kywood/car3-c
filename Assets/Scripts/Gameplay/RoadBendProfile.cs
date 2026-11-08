using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 도로 굴곡(상하 또는 좌우) 한 축. 도로에 고정된 함수 H(s) = Σ A_i sin(k_i s + φ0_i)를 쓰고,
    /// 차 위치(주행 거리 d)에서 값과 기울기가 0이 되도록 뺀 굴곡을 준다:
    ///   Offset(d, z) = H(d + z) - H(d) - z · H'(d)
    /// 그래서 차 근처는 항상 평평하고, 앞쪽 언덕·커브는 도로와 함께 다가온다.
    /// 게임 규칙은 곧은 도로 그대로이며 이 값은 그리기(셰이더)에만 쓴다. 주행 거리만으로 정해져 네트워크 동기화가 필요 없다.
    /// 셰이더와 같은 식(Car3C_Bend.hlsl)이며, 셰이더에는 위상 φ_i(d)만 넘긴다.
    /// </summary>
    public sealed class RoadBendProfile
    {
        public const int WaveCount = 4;

        private readonly double[] _amplitudes = new double[WaveCount];
        private readonly double[] _waveNumbers = new double[WaveCount];
        private readonly double[] _phaseOffsets = new double[WaveCount];

        /// <param name="amplitudes">각 물결의 진폭(m). 최대 4개.</param>
        /// <param name="wavelengths">각 물결의 파장(m). 0 이하면 그 물결은 쓰지 않는다.</param>
        /// <param name="strength">전체 배율. 0이면 굴곡 없음(곧은 도로).</param>
        public RoadBendProfile(float[] amplitudes, float[] wavelengths, float strength, int seed)
        {
            if (amplitudes == null) throw new ArgumentNullException(nameof(amplitudes));
            if (wavelengths == null) throw new ArgumentNullException(nameof(wavelengths));
            for (int i = 0; i < WaveCount; ++i)
            {
                float amp = i < amplitudes.Length ? amplitudes[i] : 0f;
                float length = i < wavelengths.Length ? wavelengths[i] : 0f;
                if (length <= 0f) continue;
                _amplitudes[i] = amp * strength;
                _waveNumbers[i] = 2d * Math.PI / length;
                _phaseOffsets[i] = Hash(seed, i) * 2d * Math.PI;
            }
        }

        public bool IsFlat
        {
            get
            {
                for (int i = 0; i < WaveCount; ++i) if (_amplitudes[i] != 0d) return false;
                return true;
            }
        }

        /// <summary>셰이더에 넘길 진폭(m).</summary>
        public float Amplitude(int i) => (float)_amplitudes[i];

        /// <summary>셰이더에 넘길 파수(rad/m).</summary>
        public float WaveNumber(int i) => (float)_waveNumbers[i];

        /// <summary>주행 거리 d에서의 위상(rad, 0~2π). 큰 거리에서도 float 정밀도를 지키려고 여기서 감싼다.</summary>
        public float Phase(int i, double distance)
        {
            double phase = _waveNumbers[i] * distance + _phaseOffsets[i];
            phase -= Math.Floor(phase / (2d * Math.PI)) * 2d * Math.PI;
            return (float)phase;
        }

        /// <summary>차 앞 z(m)에서의 굴곡(m). 셰이더와 같은 식.</summary>
        public double Offset(double distance, double z)
        {
            double sum = 0d;
            for (int i = 0; i < WaveCount; ++i)
            {
                if (_amplitudes[i] == 0d) continue;
                double phase = Phase(i, distance);
                double k = _waveNumbers[i];
                sum += _amplitudes[i] * (Math.Sin(phase + k * z) - Math.Sin(phase) - z * k * Math.Cos(phase));
            }
            return sum;
        }

        private static double Hash(int seed, int i)
        {
            unchecked
            {
                uint h = (uint)seed * 2654435761u ^ (uint)(i + 1) * 2246822519u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (h & 0xFFFFFF) / (double)0x1000000;
            }
        }
    }
}
