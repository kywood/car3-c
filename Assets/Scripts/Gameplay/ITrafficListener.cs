namespace Car3C.Gameplay
{
    /// <summary>
    /// TrafficSystem 이벤트 구독자(Observer). 뷰 풀링과 속도 규칙이 각각 구독한다.
    /// </summary>
    public interface ITrafficListener
    {
        void OnCarSpawned(TrafficCar car);
        void OnCarDespawned(TrafficCar car);
        void OnNearMiss(TrafficCar car);
        void OnCollision(TrafficCar car);
    }
}
