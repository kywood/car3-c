using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class LandmarkScheduleTests
    {
        [Test]
        public void Landmarks_ComeInOrder_AndRepeatEachCycle()
        {
            var s = new LandmarkSchedule(count: 4, spacing: 250d, first: 300d);
            Assert.AreEqual(300f, s.GetLocalZ(0, 0d, 200f), 1e-3f);
            Assert.AreEqual(550f, s.GetLocalZ(1, 0d, 200f), 1e-3f);
            // 한 바퀴(1000m) 뒤에도 같은 자리.
            Assert.AreEqual(s.GetLocalZ(2, 120d, 200f), s.GetLocalZ(2, 1120d, 200f), 1e-3f);
        }

        [Test]
        public void PassedLandmark_StaysBehind_UntilItWrapsToFar()
        {
            var s = new LandmarkSchedule(count: 4, spacing: 250d, first: 300d);
            Assert.AreEqual(-150f, s.GetLocalZ(0, 450d, 200f), 1e-3f);
            // 200m보다 더 지나가면 다음 바퀴 위치(멀리 앞)로 넘어간다.
            Assert.AreEqual(750f, s.GetLocalZ(0, 550d, 200f), 1e-3f);
        }

        [Test]
        public void LargeDistance_StaysPrecise()
        {
            var s = new LandmarkSchedule(count: 13, spacing: 260d, first: 300d);
            float z = s.GetLocalZ(5, 9_000_000d, 200f);
            Assert.That(z, Is.InRange(-200f, 13 * 260f - 200f));
        }
    }
}
