using UnityEngine;

namespace Car3C.Gameplay
{
    [CreateAssetMenu(fileName = "TrafficSettings", menuName = "Car3C/Traffic Settings")]
    public sealed class TrafficSettings : ScriptableObject
    {
        [SerializeField] private TrafficConfig _config = new TrafficConfig();

        public TrafficConfig Config => _config;
    }
}
