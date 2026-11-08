using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class AdaptiveQualityTests
    {
        private static void Run(AdaptiveQualityController c, float fps, float target, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.1f) c.Update(fps, target, 0.1f);
        }

        [Test]
        public void BelowSixty_ForTwoSeconds_LowersOneLevel()
        {
            var c = new AdaptiveQualityController(maxLevel: 4);
            Run(c, 45f, 60f, 1.5f);
            Assert.AreEqual(0, c.Level, "잠깐 떨어진 것은 무시");
            Run(c, 45f, 60f, 1f);
            Assert.AreEqual(1, c.Level);
        }

        [Test]
        public void KeepsLowering_UntilMax_WhileStillSlow()
        {
            var c = new AdaptiveQualityController(maxLevel: 2);
            Run(c, 30f, 60f, 30f);
            Assert.AreEqual(2, c.Level);
        }

        [Test]
        public void Raises_OnlyAfterLongHeadroom()
        {
            var c = new AdaptiveQualityController(maxLevel: 4, startLevel: 2);
            Run(c, 60f, 60f, 5f);
            Assert.AreEqual(2, c.Level, "목표에 겨우 맞으면 아직 올리지 않음");
            Run(c, 60f, 60f, 6f);
            Assert.AreEqual(1, c.Level, "목표를 계속 꽉 채우면 한 단계 올림");
        }

        [Test]
        public void HighRefreshPhone_120Target_At90Fps_DoesNotDrop()
        {
            var c = new AdaptiveQualityController(maxLevel: 4);
            Run(c, 90f, 120f, 20f);
            Assert.AreEqual(0, c.Level, "최소 기준은 60이므로 90이면 낮추지 않음");
        }
    }
}
