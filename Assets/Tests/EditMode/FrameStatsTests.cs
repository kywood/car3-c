using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class FrameStatsTests
    {
        [Test]
        public void Steady60_Reads60Fps_And16Ms()
        {
            var s = new FrameStats();
            for (int i = 0; i < 120; ++i) s.AddSample(1f / 60f);
            Assert.AreEqual(60f, s.AverageFps, 0.5f);
            Assert.AreEqual(16.67f, s.AverageMs, 0.1f);
            Assert.AreEqual(60f, s.LowFps, 0.5f);
        }

        [Test]
        public void OneHitch_ShowsAsWorstFrame()
        {
            var s = new FrameStats();
            for (int i = 0; i < 50; ++i) s.AddSample(1f / 60f);
            s.AddSample(0.1f);
            Assert.AreEqual(100f, s.WorstMs, 0.5f);
            Assert.AreEqual(10f, s.LowFps, 0.1f);
            Assert.Less(s.AverageFps, 60f);
        }

        [Test]
        public void OldFrames_LeaveTheWindow()
        {
            var s = new FrameStats(windowSeconds: 1f);
            s.AddSample(0.5f);
            for (int i = 0; i < 90; ++i) s.AddSample(1f / 30f);
            Assert.AreEqual(30f, s.AverageFps, 0.5f);
            Assert.AreEqual(33.3f, s.WorstMs, 0.5f);
        }
    }
}
