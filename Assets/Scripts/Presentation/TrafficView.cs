using System;
using System.Collections.Generic;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// TrafficSystem의 슬롯 수만큼 차량 뷰를 미리 만들어 두고(오브젝트 풀), 생성/제거 이벤트에 맞춰 켜고 끈다.
    /// 슬롯마다 차종을 돌아가며 배정해 여러 차종이 섞여 보이게 한다.
    /// </summary>
    public sealed class TrafficView : MonoBehaviour, ITrafficListener
    {
        [SerializeField] private TrafficCarView[] _carPrefabs = Array.Empty<TrafficCarView>();
        [SerializeField] private Material[] _bodyMaterials = Array.Empty<Material>();

        private readonly List<NightLight> _nightLights = new List<NightLight>();
        private TrafficSystem _traffic;
        private TrafficCarView[] _views;

        /// <summary>풀링된 차량들의 야간 조명(미등 불빛).</summary>
        public IReadOnlyList<NightLight> NightLights => _nightLights;

        public void Initialize(TrafficSystem traffic)
        {
            _traffic = traffic ?? throw new ArgumentNullException(nameof(traffic));
            if (_carPrefabs.Length == 0) throw new InvalidOperationException($"{nameof(TrafficView)}: 차량 프리팹이 없습니다.");

            IReadOnlyList<TrafficCar> cars = traffic.Cars;
            _views = new TrafficCarView[cars.Count];
            for (int slot = 0; slot < _views.Length; ++slot)
            {
                TrafficCarView view = Instantiate(_carPrefabs[slot % _carPrefabs.Length], transform);
                view.name = $"TrafficCar_{slot}";
                view.gameObject.SetActive(false);
                _views[slot] = view;
                _nightLights.AddRange(view.GetComponentsInChildren<NightLight>(true));
            }

            traffic.AddListener(this);
        }

        private void OnDestroy()
        {
            _traffic?.RemoveListener(this);
        }

        private void LateUpdate()
        {
            if (_traffic == null) return;

            float deltaTime = Time.deltaTime;
            IReadOnlyList<TrafficCar> cars = _traffic.Cars;
            for (int slot = 0; slot < _views.Length; ++slot)
            {
                TrafficCar car = cars[slot];
                if (!car.IsActive) continue;
                if (car.IsCrashed)
                    _views[slot].SetCrashPose(car.X, car.Z, Mathf.Clamp01(car.CrashElapsed / _traffic.CrashRemoveSeconds), car.CrashElapsed);
                else
                    _views[slot].SetPose(car.X, car.Z, car.IsHit);
                _views[slot].SpinWheels(car.Speed * deltaTime);
            }
        }

        void ITrafficListener.OnCarSpawned(TrafficCar car)
        {
            TrafficCarView view = _views[car.Slot];
            if (view.HasRecolorableBody && _bodyMaterials.Length > 0)
                view.SetBodyMaterial(_bodyMaterials[UnityEngine.Random.Range(0, _bodyMaterials.Length)]);
            view.SetPose(car.X, car.Z, false);
            view.gameObject.SetActive(true);
        }

        void ITrafficListener.OnCarDespawned(TrafficCar car)
        {
            _views[car.Slot].gameObject.SetActive(false);
        }

        void ITrafficListener.OnNearMiss(TrafficCar car)
        {
        }

        void ITrafficListener.OnCollision(TrafficCar car)
        {
        }
    }
}
