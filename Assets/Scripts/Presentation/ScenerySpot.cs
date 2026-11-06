using System;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 배경 구간 안의 배치 지점 하나. 구간마다 Options 중 하나를 고르거나(EmptyChance 확률로) 비운다.
    /// </summary>
    [Serializable]
    public sealed class ScenerySpot
    {
        [SerializeField] private string _name = "Spot";
        [SerializeField] private Vector3 _localPosition;
        [SerializeField] private float _yaw;
        [SerializeField, Range(0f, 1f)] private float _emptyChance;
        [SerializeField] private GameObject[] _options = Array.Empty<GameObject>();
        [Tooltip("이 지점이 나타나는 구간(SceneryView가 구간을 쓸 때만 적용)")]
        [SerializeField] private Car3C.Gameplay.MapZoneMask _zones = Car3C.Gameplay.MapZoneMask.AllOutdoor;

        public string Name => _name;
        public Car3C.Gameplay.MapZoneMask Zones => _zones;
        public Vector3 LocalPosition => _localPosition;
        public float Yaw => _yaw;
        public float EmptyChance => _emptyChance;
        public GameObject[] Options => _options;

        public ScenerySpot()
        {
        }

        public ScenerySpot(string name, Vector3 localPosition, float yaw, float emptyChance, GameObject[] options)
        {
            _name = name;
            _localPosition = localPosition;
            _yaw = yaw;
            _emptyChance = emptyChance;
            _options = options ?? Array.Empty<GameObject>();
        }
    }
}
