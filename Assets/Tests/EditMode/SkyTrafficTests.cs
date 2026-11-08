using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class SkyTrafficTests
    {
        [Test]
        public void SameSeed_GivesSameFlight_OnEveryClient()
        {
            var a = new SkyTrafficSchedule(42);
            var b = new SkyTrafficSchedule(42);
            for (double t = 0; t < 300; t += 3.7)
            {
                bool fa = a.TryGetFlight(t, out SkyFlight x);
                bool fb = b.TryGetFlight(t, out SkyFlight y);
                Assert.AreEqual(fa, fb);
                if (!fa) continue;
                Assert.AreEqual(x.Index, y.Index);
                Assert.AreEqual(x.Progress, y.Progress, 1e-6f);
                Assert.AreEqual(x.Altitude, y.Altitude, 1e-6f);
                Assert.AreEqual(x.LeftToRight, y.LeftToRight);
            }
        }

        [Test]
        public void Flights_ProgressAcrossThenPause()
        {
            var s = new SkyTrafficSchedule(7, intervalSeconds: 30f, flightSeconds: 20f);
            Assert.IsFalse(s.TryGetFlight(2, out _), "처음 몇 초는 비어 있다");
            Assert.IsTrue(s.TryGetFlight(6 + 10, out SkyFlight mid));
            Assert.AreEqual(0.5f, mid.Progress, 1e-4f);
            Assert.IsFalse(s.TryGetFlight(6 + 25, out _), "비행 사이 쉬는 시간");
            Assert.IsTrue(s.TryGetFlight(6 + 31, out SkyFlight next));
            Assert.AreEqual(mid.Index + 1, next.Index);
            Assert.That(mid.Altitude, Is.InRange(55f, 90f));
            Assert.That(mid.DistanceAhead, Is.InRange(330f, 420f));
        }
    }
}
