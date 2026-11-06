using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 교통 차량 한 대의 표시. 위치, 충돌 시 틀어짐, 바퀴 회전, (선택) 차체 색 교체를 담당한다.
    /// </summary>
    public sealed class TrafficCarView : MonoBehaviour
    {
        [SerializeField] private Renderer[] _bodyRenderers = System.Array.Empty<Renderer>();
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private WheelSpinner _wheels;
        [SerializeField] private float _hitYawAngle = 18f;

        public bool HasRecolorableBody => _bodyRenderers.Length > 0;

        public void SetBodyMaterial(Material material)
        {
            for (int i = 0; i < _bodyRenderers.Length; ++i) _bodyRenderers[i].sharedMaterial = material;
        }

        [SerializeField] private float _crashSpinDegrees = 540f;
        [SerializeField] private float _crashBlinkInterval = 0.08f;

        public void SetPose(float x, float z, bool isHit)
        {
            transform.localPosition = new Vector3(x, 0f, z);
            if (_visualRoot == null) return;
            _visualRoot.localRotation = isHit ? Quaternion.Euler(0f, _hitYawAngle, 0f) : Quaternion.identity;
            if (!_visualRoot.gameObject.activeSelf) _visualRoot.gameObject.SetActive(true);
        }

        /// <summary>충돌 연출: progress(0~1) 동안 빙글 돌며 깜빡인다. 끝나면 로직이 차량을 제거한다.</summary>
        public void SetCrashPose(float x, float z, float progress, float elapsed)
        {
            transform.localPosition = new Vector3(x, 0f, z);
            if (_visualRoot == null) return;
            float eased = 1f - (1f - progress) * (1f - progress);
            _visualRoot.localRotation = Quaternion.Euler(0f, _hitYawAngle + _crashSpinDegrees * eased, 0f);
            bool visible = Mathf.FloorToInt(elapsed / _crashBlinkInterval) % 2 == 0;
            if (_visualRoot.gameObject.activeSelf != visible) _visualRoot.gameObject.SetActive(visible);
        }

        public void SpinWheels(float meters)
        {
            if (_wheels != null) _wheels.Spin(meters);
        }
    }
}
