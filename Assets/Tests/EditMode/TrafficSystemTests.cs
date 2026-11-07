using System;
using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class TrafficSystemTests
    {
        private sealed class RecordingListener : ITrafficListener
        {
            public int Spawned;
            public int Despawned;
            public int NearMisses;
            public int Collisions;

            public void OnCarSpawned(TrafficCar car) => ++Spawned;
            public void OnCarDespawned(TrafficCar car) => ++Despawned;
            public void OnNearMiss(TrafficCar car) => ++NearMisses;
            public void OnCollision(TrafficCar car) => ++Collisions;
        }

        private static readonly LaneLayout Layout = new LaneLayout(5, 3.5f);

        private static TrafficConfig CreateConfig(float rowGap)
        {
            return new TrafficConfig(capacity: 24, spawnAhead: 140f, despawnBehind: -20f, despawnAhead: 170f,
                minRowGap: rowGap, maxRowGap: rowGap, secondCarChance: 0.5f, blockWindow: 14f,
                slowestLaneSpeed: 8f, fastestLaneSpeed: 16f, carLength: 4f, carWidth: 1.8f, hitboxScale: 0.9f,
                nearMissLateralDistance: 3.6f, nearMissLongitudinalMargin: 1f);
        }

        private static TrafficSystem CreateManual(RecordingListener listener)
        {
            TrafficSystem traffic = new TrafficSystem(Layout, CreateConfig(1e9f), new Random(1));
            traffic.AddListener(listener);
            return traffic;
        }

        private static void Drive(TrafficSystem traffic, float playerSpeed, float playerX, float playerZ, bool invulnerable, float seconds)
        {
            const float step = 0.02f;
            for (float t = 0f; t < seconds; t += step)
                traffic.Tick(step, playerSpeed, 0d, playerX, playerZ, invulnerable);
        }

        [Test]
        public void LaneSpeeds_IncreaseFromLeftToRight()
        {
            TrafficSystem traffic = CreateManual(new RecordingListener());
            Assert.AreEqual(8f, traffic.GetLaneSpeed(0), 1e-4f);
            Assert.AreEqual(12f, traffic.GetLaneSpeed(2), 1e-4f);
            Assert.AreEqual(16f, traffic.GetLaneSpeed(4), 1e-4f);
        }

        [Test]
        public void SameLane_Collides_Once()
        {
            RecordingListener listener = new RecordingListener();
            TrafficSystem traffic = CreateManual(listener);
            traffic.SpawnCar(2, 20f);

            Drive(traffic, 22f, 0f, 0f, false, 5f);

            Assert.AreEqual(1, listener.Collisions);
            Assert.AreEqual(0, listener.NearMisses);
        }

        [Test]
        public void AdjacentLane_PassingClose_IsNotNearMiss()
        {
            RecordingListener listener = new RecordingListener();
            TrafficSystem traffic = CreateManual(listener);
            traffic.SpawnCar(3, 20f);

            Drive(traffic, 22f, 0f, 0f, false, 5f);

            Assert.AreEqual(0, listener.NearMisses);
            Assert.AreEqual(0, listener.Collisions);
        }

        [TestCase(0f)]
        [TestCase(6f)]
        public void ApproachingDirectlyBehind_ThenDodging_IsNearMissOnce(float playerZ)
        {
            RecordingListener listener = new RecordingListener();
            TrafficSystem traffic = CreateManual(listener);
            TrafficCar car = traffic.SpawnCar(2, playerZ + 5f);

            traffic.Tick(0.02f, 22f, 0d, 0f, playerZ, false);

            Assert.IsTrue(car.IsNearMissArmed);
            Assert.AreEqual(0, listener.NearMisses);
            Drive(traffic, 22f, Layout.GetLaneCenter(1), playerZ, false, 5f);

            Assert.AreEqual(1, listener.NearMisses);
            Assert.AreEqual(0, listener.Collisions);
        }

        [TestCase(0f)]
        [TestCase(-4f)]
        public void MovingCloseOnlyAlongsideOrAhead_IsNotNearMiss(float carZ)
        {
            RecordingListener listener = new RecordingListener();
            TrafficSystem traffic = CreateManual(listener);
            traffic.SpawnCar(3, carZ);

            Drive(traffic, 22f, 0f, 0f, false, 5f);

            Assert.AreEqual(0, listener.NearMisses);
            Assert.AreEqual(0, listener.Collisions);
        }

        [TestCase(12f)]
        [TestCase(10f)]
        public void DirectlyBehind_WithoutClosing_DoesNotArmNearMiss(float playerSpeed)
        {
            RecordingListener listener = new RecordingListener();
            TrafficSystem traffic = CreateManual(listener);
            TrafficCar car = traffic.SpawnCar(2, 4.5f);

            traffic.Tick(0.02f, playerSpeed, 0d, 0f, 0f, false);

            Assert.IsFalse(car.IsNearMissArmed);
        }

        [Test]
        public void TwoLanesAway_IsNotNearMiss()
        {
            RecordingListener listener = new RecordingListener();
            TrafficSystem traffic = CreateManual(listener);
            traffic.SpawnCar(4, 20f);

            Drive(traffic, 22f, 0f, 0f, false, 5f);

            Assert.AreEqual(0, listener.NearMisses);
            Assert.AreEqual(0, listener.Collisions);
        }

        [Test]
        public void Invulnerable_PassesThroughWithoutCollisionOrNearMiss()
        {
            RecordingListener listener = new RecordingListener();
            TrafficSystem traffic = CreateManual(listener);
            traffic.SpawnCar(2, 20f);

            Drive(traffic, 22f, 0f, 0f, true, 5f);

            Assert.AreEqual(0, listener.Collisions);
            Assert.AreEqual(0, listener.NearMisses);
        }

        [Test]
        public void PlayerForwardOffset_ShiftsCollisionPoint()
        {
            RecordingListener listener = new RecordingListener();
            TrafficSystem traffic = CreateManual(listener);
            TrafficCar car = traffic.SpawnCar(2, 9f);

            traffic.Tick(0.001f, 12f, 0d, 0f, 6f, false);

            Assert.AreEqual(1, listener.Collisions);
            Assert.IsTrue(car.IsHit);
        }

        [Test]
        public void PassedCar_IsDespawned_AndSlotReused()
        {
            RecordingListener listener = new RecordingListener();
            TrafficSystem traffic = CreateManual(listener);
            TrafficCar car = traffic.SpawnCar(4, 5f);

            Drive(traffic, 22f, 0f, 0f, false, 6f);

            Assert.AreEqual(1, listener.Despawned);
            Assert.IsFalse(car.IsActive);
            Assert.AreSame(car, traffic.SpawnCar(0, 50f));
        }

        [Test]
        public void SpawnedRows_AlwaysLeaveAFreeLane()
        {
            TrafficSystem traffic = new TrafficSystem(Layout, CreateConfig(3f), new Random(42));
            double distance = 0d;
            const float step = 0.02f;
            const float playerSpeed = 30f;
            bool[] occupied = new bool[Layout.LaneCount];

            for (int frame = 0; frame < 5000; ++frame)
            {
                distance += playerSpeed * step;
                traffic.Tick(step, playerSpeed, distance, 100f, 0f, false);

                for (int i = 0; i < traffic.Cars.Count; ++i)
                {
                    TrafficCar anchor = traffic.Cars[i];
                    if (!anchor.IsActive) continue;

                    Array.Clear(occupied, 0, occupied.Length);
                    int count = 0;
                    for (int j = 0; j < traffic.Cars.Count; ++j)
                    {
                        TrafficCar other = traffic.Cars[j];
                        if (!other.IsActive || occupied[other.Lane] || Math.Abs(other.Z - anchor.Z) >= 4f) continue;
                        occupied[other.Lane] = true;
                        ++count;
                    }

                    Assert.Less(count, Layout.LaneCount, $"frame {frame}: 모든 차선이 막혔습니다.");
                }
            }

            Assert.Greater(traffic.CountActive(), 0);
        }
    }
}
