using UnityEngine;

namespace Car3C.Gameplay
{
    [CreateAssetMenu(fileName = "DriveSettings", menuName = "Car3C/Drive Settings")]
    public sealed class DriveSettings : ScriptableObject
    {
        [SerializeField] private DriveConfig _config = new DriveConfig();

        public DriveConfig Config => _config;
    }
}
