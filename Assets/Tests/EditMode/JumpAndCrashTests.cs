using System;
using Car3C.Gameplay;
using Car3C.Infrastructure;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class JumpAndCrashTests
    {
        private sealed class Recorder : ITrafficListener
        {
            public int Despawned, NearMisses, Collisions;
            public void OnCarSpawned(TrafficCar car) { }
            public void OnCarDespawned(TrafficCar car) => ++Despawned;
            public void OnNearMiss(TrafficCar car) => ++NearMisses;
            public void OnCollision(TrafficCar car) => ++Collisions;
        }

        private static TrafficSystem CreateTraffic(Recorder recorder)
        {
            var config = new TrafficConfig(capacity: 8, spawnAhead: 140f, despawnBehind: -20f, despawnAhead: 170f,
                minRowGap: 1e9f, maxRowGap: 1e9f, secondCarChance: 0f, blockWindow: 14f, slowestLaneSpeed: 10f,
                fastestLaneSpeed: 10f, carLength: 4f, carWidth: 1.8f, hitboxScale: 0.9f, nearMissLateralDistance: 3.6f,
                nearMissLongitudinalMargin: 1f);
            var traffic = new TrafficSystem(new LaneLayout(5, 3.5f), config, new Random(1));
            traffic.AddListener(recorder);
            return traffic;
        }

        [Test]
        public void Jump_FollowsArc_AndLands()
        {
            var jump = new JumpMotion(2f, 1f);
            Assert.IsTrue(jump.TryJump());
            Assert.IsFalse(jump.TryJump());
            jump.Tick(0.5f);
            Assert.AreEqual(2f, jump.Height, 1e-4f);
            jump.Tick(0.6f);
            Assert.IsFalse(jump.IsAirborne);
            Assert.AreEqual(0f, jump.Height, 1e-5f);
            Assert.IsTrue(jump.TryJump());
        }

        [Test]
        public void JumpingOverCar_NoCollision_CountsAsNearMiss()
        {
            var rec = new Recorder();
            TrafficSystem traffic = CreateTraffic(rec);
            traffic.SpawnCar(2, 3f);
            for (int i = 0; i < 100; ++i) traffic.Tick(0.02f, 20f, 0d, 0f, 0f, false, 2f);
            Assert.AreEqual(0, rec.Collisions);
            Assert.AreEqual(1, rec.NearMisses);
        }

        [Test]
        public void CrashedCar_DisappearsAfterDelay()
        {
            var rec = new Recorder();
            TrafficSystem traffic = CreateTraffic(rec);
            TrafficCar car = traffic.SpawnCar(2, 2f);
            traffic.Tick(0.02f, 10f, 0d, 0f, 0f, false);
            Assert.AreEqual(1, rec.Collisions);
            Assert.IsTrue(car.IsCrashed);

            for (int i = 0; i < 30; ++i) traffic.Tick(0.02f, 10f, 0d, 0f, 0f, false);
            Assert.IsTrue(car.IsActive);
            for (int i = 0; i < 40; ++i) traffic.Tick(0.02f, 10f, 0d, 0f, 0f, false);
            Assert.IsFalse(car.IsActive);
            Assert.AreEqual(1, rec.Despawned);
        }

        [Test]
        public void SwipeUp_IsJump_SidewaysIsNot()
        {
            Assert.IsTrue(LaneGestureResolver.IsJumpSwipe(500f, 200f, 520f, 400f, 50f));
            Assert.IsFalse(LaneGestureResolver.IsJumpSwipe(500f, 200f, 800f, 300f, 50f));
            Assert.IsFalse(LaneGestureResolver.IsJumpSwipe(500f, 400f, 500f, 200f, 50f));
        }
    }
}
