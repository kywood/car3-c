using System;
using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    /// <summary>
    /// 실제 게임 수치(차 길이 4.8m, 폭 2m, 차선 변경 0.18초)로 "앞차 뒤에서 다가가다 옆으로 피하기"를 재현한다.
    /// </summary>
    public sealed class NearMissDodgeTests
    {
        private sealed class Recorder : ITrafficListener
        {
            public int NearMisses, Collisions;
            public void OnCarSpawned(TrafficCar car) { }
            public void OnCarDespawned(TrafficCar car) { }
            public void OnNearMiss(TrafficCar car) => ++NearMisses;
            public void OnCollision(TrafficCar car) => ++Collisions;
        }

        private static (int nearMisses, int collisions) DodgeWhenGapIs(float startDodgeGap, float longitudinalMargin)
        {
            var layout = new LaneLayout(5, 3.5f);
            var config = new TrafficConfig(capacity: 8, spawnAhead: 140f, despawnBehind: -20f, despawnAhead: 170f,
                minRowGap: 1e9f, maxRowGap: 1e9f, secondCarChance: 0f, blockWindow: 14f, slowestLaneSpeed: 12f,
                fastestLaneSpeed: 12f, carLength: 4.8f, carWidth: 2f, hitboxScale: 0.9f, nearMissLateralDistance: 3.6f,
                nearMissLongitudinalMargin: longitudinalMargin);
            var rec = new Recorder();
            var traffic = new TrafficSystem(layout, config, new Random(1));
            traffic.AddListener(rec);
            TrafficCar car = traffic.SpawnCar(2, 30f);
            var motion = new VehicleMotion(layout, 2, 0.18f);

            const float step = 1f / 60f;
            bool dodged = false;
            for (int i = 0; i < 400; ++i)
            {
                if (!dodged && car.Z <= startDodgeGap)
                {
                    motion.RequestLaneChange(1);
                    dodged = true;
                }
                motion.Tick(step);
                traffic.Tick(step, 22f, 0d, motion.LateralOffset, 0f, false);
            }
            return (rec.NearMisses, rec.Collisions);
        }

        [TestCase(6f)]
        [TestCase(8f)]
        [TestCase(10f)]
        public void DodgingFromBehind_WithWideWindow_IsNearMiss(float gap)
        {
            var (nearMisses, collisions) = DodgeWhenGapIs(gap, 6f);
            Assert.AreEqual(0, collisions);
            Assert.AreEqual(1, nearMisses);
        }

        [Test]
        public void DodgingFromBehind_WithOldOneMeterWindow_IsMissed()
        {
            var (nearMisses, collisions) = DodgeWhenGapIs(8f, 1f);
            Assert.AreEqual(0, collisions);
            Assert.AreEqual(0, nearMisses);
        }

        [Test]
        public void DodgingTooLate_Collides()
        {
            var (nearMisses, collisions) = DodgeWhenGapIs(4.5f, 6f);
            Assert.AreEqual(1, collisions);
            Assert.AreEqual(0, nearMisses);
        }
    }
}
