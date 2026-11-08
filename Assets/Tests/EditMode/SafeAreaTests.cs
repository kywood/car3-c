using Car3C.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Car3C.Tests.EditMode
{
    public sealed class SafeAreaTests
    {
        [Test]
        public void NormalNotch_IsKept()
        {
            Rect r = SafeAreaFitter.Normalize(new Rect(80f, 0f, 2240f, 1080f), 2400f, 1080f);
            Assert.AreEqual(80f / 2400f, r.xMin, 1e-4f);
            Assert.AreEqual(2320f / 2400f, r.xMax, 1e-4f);
            Assert.AreEqual(0f, r.yMin, 1e-4f);
            Assert.AreEqual(1f, r.yMax, 1e-4f);
        }

        [Test]
        public void ZeroSize_FallsBackToFullScreen()
        {
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), SafeAreaFitter.Normalize(new Rect(0f, 0f, 0f, 0f), 1600f, 900f));
        }

        [Test]
        public void PortraitCoordinatesOnLandscapeScreen_FallsBackToFullScreen()
        {
            // 회전 전(세로) 좌표가 넘어오면 화면 밖으로 벗어난다.
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), SafeAreaFitter.Normalize(new Rect(0f, 0f, 900f, 1600f), 1600f, 900f));
        }
    }
}
