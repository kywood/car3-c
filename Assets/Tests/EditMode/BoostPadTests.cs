using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class BoostPadTests
    {
        private static readonly LaneLayout Layout = new LaneLayout(5, 3.5f);

        private static BoostPadField Create()
        {
            return new BoostPadField(Layout, firstDistance: 100d, spacing: 200d, length: 6f, jitter: 30f, seed: 7);
        }

        [Test]
        public void Placement_IsDeterministic_AndInsideLanes()
        {
            BoostPadField a = Create(), b = Create();
            for (long i = 0; i < 50; ++i)
            {
                Assert.AreEqual(a.GetPadDistance(i), b.GetPadDistance(i), 1e-9);
                Assert.AreEqual(a.GetPadLane(i), b.GetPadLane(i));
                Assert.That(a.GetPadLane(i), Is.InRange(0, 4));
                Assert.That(a.GetPadDistance(i), Is.InRange(100d + i * 200d - 30d, 100d + i * 200d + 30d));
            }
        }

        [Test]
        public void DrivingOverPad_TriggersOnce()
        {
            BoostPadField field = Create();
            double start = field.GetPadDistance(0);
            float x = field.GetPadX(0);
            int triggers = 0;
            for (double d = start - 5d; d < start + 10d; d += 0.5d)
                if (field.TryTrigger(d, x, 0f, false, out long index)) { ++triggers; Assert.AreEqual(0L, index); }
            Assert.AreEqual(1, triggers);
        }

        [Test]
        public void OtherLane_OrAirborne_DoesNotTrigger()
        {
            BoostPadField field = Create();
            double mid = field.GetPadDistance(0) + 3d;
            float x = field.GetPadX(0);
            float otherX = x + Layout.LaneWidth * (field.GetPadLane(0) < 4 ? 1f : -1f);
            Assert.IsFalse(field.TryTrigger(mid, otherX, 0f, false, out _));
            Assert.IsFalse(field.TryTrigger(mid, x, 0f, true, out _));
        }

        [Test]
        public void ForwardOffset_IsUsedForPlayerPosition()
        {
            BoostPadField field = Create();
            double start = field.GetPadDistance(0);
            Assert.IsTrue(field.TryTrigger(start - 4d, field.GetPadX(0), 6f, false, out _));
        }

        [Test]
        public void SpeedBoost_IsCappedAtMax()
        {
            var speed = new SpeedController(SpeedControllerTests.CreateConfig());
            speed.ApplyBoost(12f);
            Assert.AreEqual(32f, speed.CurrentSpeed, 1e-4f);
            speed.ApplyBoost(50f);
            Assert.AreEqual(40f, speed.CurrentSpeed, 1e-4f);
        }
    }
}
