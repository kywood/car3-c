using UnityEngine;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 차선 수, 차선 폭, 시작 차선을 코드 밖에서 관리하는 설정 에셋.
    /// 도로·차량·장애물·충돌은 모두 CreateLayout()으로 만든 같은 LaneLayout을 기준으로 삼는다.
    /// </summary>
    [CreateAssetMenu(fileName = "TrackSettings", menuName = "Car3C/Track Settings")]
    public sealed class TrackSettings : ScriptableObject
    {
        [SerializeField, Min(1)] private int _laneCount = 5;
        [SerializeField, Min(0.5f)] private float _laneWidth = 3.5f;
        [SerializeField, Min(0)] private int _initialLane = 2;

        public int LaneCount => _laneCount;
        public float LaneWidth => _laneWidth;
        public int InitialLane => _initialLane;

        public LaneLayout CreateLayout()
        {
            return new LaneLayout(_laneCount, _laneWidth);
        }

        private void OnValidate()
        {
            _initialLane = Mathf.Clamp(_initialLane, 0, Mathf.Max(0, _laneCount - 1));
        }
    }
}
