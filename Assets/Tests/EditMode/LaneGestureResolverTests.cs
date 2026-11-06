using Car3C.Infrastructure;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class LaneGestureResolverTests
    {
        [Test]
        public void Swipe_UsesSwipeDirection()
        {
            Assert.AreEqual(1, LaneGestureResolver.Resolve(100f, 300f, 1000f, 50f));
            Assert.AreEqual(-1, LaneGestureResolver.Resolve(900f, 700f, 1000f, 50f));
        }

        [Test]
        public void Tap_UsesScreenHalf()
        {
            Assert.AreEqual(-1, LaneGestureResolver.Resolve(200f, 210f, 1000f, 50f));
            Assert.AreEqual(1, LaneGestureResolver.Resolve(800f, 790f, 1000f, 50f));
        }
    }
}
