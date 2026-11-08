using UnityEngine;

namespace Car3C.Gameplay
{
    /// <summary>도로 굴곡 조정값. Strength를 0으로 두면 곧은 도로(굴곡 끔).</summary>
    [CreateAssetMenu(menuName = "Car3C/Road Bend Settings", fileName = "RoadBendSettings")]
    public sealed class RoadBendSettings : ScriptableObject
    {
        [Header("상하 (언덕)")]
        [Tooltip("0이면 상하 굴곡 끔")]
        [SerializeField, Range(0f, 2f)] private float _verticalStrength = 1f;
        [SerializeField] private float[] _verticalAmplitudes = { 3f, 5f, 8f, 0f };
        [SerializeField] private float[] _verticalWavelengths = { 260f, 470f, 830f, 0f };

        [Header("좌우 (커브)")]
        [Tooltip("0이면 좌우 굴곡 끔")]
        [SerializeField, Range(0f, 2f)] private float _lateralStrength;
        [SerializeField] private float[] _lateralAmplitudes = { 10f, 18f, 0f, 0f };
        [SerializeField] private float[] _lateralWavelengths = { 520f, 910f, 0f, 0f };

        [SerializeField] private int _seed = 7;

        public RoadBendProfile CreateVertical() => new RoadBendProfile(_verticalAmplitudes, _verticalWavelengths, _verticalStrength, _seed);

        public RoadBendProfile CreateLateral() => new RoadBendProfile(_lateralAmplitudes, _lateralWavelengths, _lateralStrength, _seed + 101);
    }
}
