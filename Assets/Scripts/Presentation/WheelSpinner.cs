using System.Collections.Generic;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 차량 모델 안의 바퀴 메시(이름에 wheel/tire/tyre 포함)를 이동 거리만큼 굴린다.
    /// 바퀴 피벗이 차량 원점에 있는 모델도 있어, 메시 중심을 축으로 회전시킨다.
    /// </summary>
    public sealed class WheelSpinner : MonoBehaviour
    {
        private struct Wheel
        {
            public Transform Transform;
            public Vector3 MeshCenter;
            public Vector3 Pivot;
            public Vector3 Axle;
            public Quaternion RestRotation;
            public float Radius;
        }

        private readonly List<Wheel> _wheels = new List<Wheel>(4);
        private float _angle;

        private void Awake()
        {
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                string name = renderer.name.ToLowerInvariant();
                if (!name.Contains("wheel") && !name.Contains("tire") && !name.Contains("tyre")) continue;

                Transform wheel = renderer.transform;
                Vector3 meshCenter = renderer.localBounds.center;
                _wheels.Add(new Wheel
                {
                    Transform = wheel,
                    MeshCenter = meshCenter,
                    Pivot = wheel.parent.InverseTransformPoint(wheel.TransformPoint(meshCenter)),
                    Axle = wheel.parent.InverseTransformDirection(transform.right).normalized,
                    RestRotation = wheel.localRotation,
                    Radius = Mathf.Max(0.1f, renderer.bounds.extents.y),
                });
            }
        }

        /// <summary>meters만큼 굴러간 것처럼 모든 바퀴를 회전한다.</summary>
        public void Spin(float meters)
        {
            if (_wheels.Count == 0 || meters == 0f) return;

            _angle = Mathf.Repeat(_angle + meters / _wheels[0].Radius * Mathf.Rad2Deg, 360f);
            for (int i = 0; i < _wheels.Count; ++i)
            {
                Wheel wheel = _wheels[i];
                Quaternion rotation = Quaternion.AngleAxis(_angle, wheel.Axle) * wheel.RestRotation;
                wheel.Transform.localRotation = rotation;
                wheel.Transform.localPosition = wheel.Pivot - rotation * Vector3.Scale(wheel.MeshCenter, wheel.Transform.localScale);
            }
        }
    }
}
