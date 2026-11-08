using System;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 주행 거리에 맞춰 도로 굴곡 값을 셰이더 전역 변수로 넘긴다(Car3C_Bend.hlsl). 굴곡을 쓰는 재질은 모두 같은 값을 받아
    /// 도로·건물·차·바다가 이음새 없이 함께 휜다.
    /// </summary>
    public sealed class RoadBendView : MonoBehaviour
    {
        private static readonly int PhaseId = Shader.PropertyToID("_Car3CBendPhase");
        private static readonly int WaveNumberId = Shader.PropertyToID("_Car3CBendK");
        private static readonly int AmplitudeId = Shader.PropertyToID("_Car3CBendAmp");
        private static readonly int PhaseXId = Shader.PropertyToID("_Car3CBendPhaseX");
        private static readonly int WaveNumberXId = Shader.PropertyToID("_Car3CBendKX");
        private static readonly int AmplitudeXId = Shader.PropertyToID("_Car3CBendAmpX");

        private RoadBendProfile _vertical;
        private RoadBendProfile _lateral;
        private SpeedController _speed;

        public void Initialize(RoadBendProfile vertical, RoadBendProfile lateral, SpeedController speed)
        {
            _vertical = vertical ?? throw new ArgumentNullException(nameof(vertical));
            _lateral = lateral ?? throw new ArgumentNullException(nameof(lateral));
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            Shader.SetGlobalVector(WaveNumberId, WaveNumbers(_vertical));
            Shader.SetGlobalVector(AmplitudeId, Amplitudes(_vertical));
            Shader.SetGlobalVector(WaveNumberXId, WaveNumbers(_lateral));
            Shader.SetGlobalVector(AmplitudeXId, Amplitudes(_lateral));
            Apply();
        }

        private void LateUpdate()
        {
            if (_speed != null) Apply();
        }

        private void OnDestroy()
        {
            // 에디터에서 플레이를 끝내면 씬 뷰가 휜 채로 남지 않게 끈다.
            Shader.SetGlobalVector(AmplitudeId, Vector4.zero);
            Shader.SetGlobalVector(AmplitudeXId, Vector4.zero);
        }

        private void Apply()
        {
            double d = _speed.Distance;
            Shader.SetGlobalVector(PhaseId, Phases(_vertical, d));
            Shader.SetGlobalVector(PhaseXId, Phases(_lateral, d));
        }

        private static Vector4 Phases(RoadBendProfile p, double d) => new Vector4(p.Phase(0, d), p.Phase(1, d), p.Phase(2, d), p.Phase(3, d));

        private static Vector4 WaveNumbers(RoadBendProfile p) => new Vector4(p.WaveNumber(0), p.WaveNumber(1), p.WaveNumber(2), p.WaveNumber(3));

        private static Vector4 Amplitudes(RoadBendProfile p) => new Vector4(p.Amplitude(0), p.Amplitude(1), p.Amplitude(2), p.Amplitude(3));
    }
}
