namespace Car3C.Gameplay
{
    /// <summary>
    /// 풀 슬롯 하나에 해당하는 교통 차량 상태. Z는 카메라 기준점(z = 0) 기준 상대 위치.
    /// </summary>
    public sealed class TrafficCar
    {
        public int Slot { get; }
        public bool IsActive { get; private set; }
        public int Lane { get; private set; }
        public float X { get; private set; }
        public float Z { get; internal set; }
        public float Speed { get; private set; }
        /// <summary>플레이어와 부딪혔거나 무적 상태에서 겹쳐 지나간 차량. 다시 판정하지 않는다.</summary>
        public bool IsHit { get; internal set; }
        public bool IsNearMissArmed { get; internal set; }
        /// <summary>플레이어와 실제로 충돌한 차량. CrashRemoveSeconds 뒤 도로에서 사라진다.</summary>
        public bool IsCrashed { get; internal set; }
        public float CrashElapsed { get; internal set; }
        public bool IsPassed { get; internal set; }

        public TrafficCar(int slot)
        {
            Slot = slot;
        }

        internal void Activate(int lane, float x, float z, float speed)
        {
            IsActive = true;
            Lane = lane;
            X = x;
            Z = z;
            Speed = speed;
            IsHit = false;
            IsNearMissArmed = false;
            IsPassed = false;
            IsCrashed = false;
            CrashElapsed = 0f;
        }

        internal void Deactivate()
        {
            IsActive = false;
        }
    }
}
