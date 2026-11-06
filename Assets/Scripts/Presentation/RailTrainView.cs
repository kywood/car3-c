using System;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 고가 경전철 열차 표시. RailTrainMotion이 계산한 위치로 편성 전체를 옮기고, 쉬는 동안은 숨긴다.
    /// </summary>
    public sealed class RailTrainView : MonoBehaviour
    {
        [SerializeField] private Transform _trainRoot;
        [SerializeField] private float _trainSpeed = 25f;
        [SerializeField] private float _spawnZ = 320f;
        [SerializeField] private float _despawnZ = -120f;
        [SerializeField] private float _interval = 6f;

        private SpeedController _speed;
        private RailTrainMotion _motion;

        public void Initialize(SpeedController speed)
        {
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            if (_trainRoot == null) throw new InvalidOperationException($"{nameof(RailTrainView)}: 열차 루트가 없습니다.");
            _motion = new RailTrainMotion(_trainSpeed, _spawnZ, _despawnZ, _interval);
            _trainRoot.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_motion == null) return;

            _motion.Tick(Time.deltaTime, _speed.CurrentSpeed);
            if (_trainRoot.gameObject.activeSelf != _motion.IsActive) _trainRoot.gameObject.SetActive(_motion.IsActive);
            if (!_motion.IsActive) return;

            Vector3 position = _trainRoot.localPosition;
            position.z = _motion.Z;
            _trainRoot.localPosition = position;
        }
    }
}
