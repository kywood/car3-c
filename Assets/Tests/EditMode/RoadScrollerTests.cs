using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class RoadScrollerTests
    {
        [Test]
        public void FirstSegment_IsClampedToZeroAtStart()
        {
            RoadScroller scroller = new RoadScroller(20f, 10, 2);
            Assert.AreEqual(0L, scroller.GetFirstSegmentIndex(0d));
            Assert.AreEqual(0L, scroller.GetFirstSegmentIndex(45d));
        }

        [Test]
        public void FirstSegment_KeepsSegmentsBehindPlayer()
        {
            RoadScroller scroller = new RoadScroller(20f, 10, 2);
            Assert.AreEqual(3L, scroller.GetFirstSegmentIndex(100d));
        }

        [Test]
        public void Slot_WrapsAroundPool()
        {
            RoadScroller scroller = new RoadScroller(20f, 10, 2);
            Assert.AreEqual(3, scroller.GetSlot(13L));
        }

        [Test]
        public void SegmentLocalZ_MovesTowardPlayerAsDistanceGrows()
        {
            RoadScroller scroller = new RoadScroller(20f, 10, 2);
            Assert.AreEqual(260f, scroller.GetSegmentLocalZ(13L, 0d), 1e-4f);
            Assert.AreEqual(10f, scroller.GetSegmentLocalZ(13L, 250d), 1e-4f);
        }

        [Test]
        public void SegmentLocalZ_StaysPreciseAtLargeDistance()
        {
            RoadScroller scroller = new RoadScroller(20f, 10, 2);
            double distance = 5_000_000.25d;
            long first = scroller.GetFirstSegmentIndex(distance);
            Assert.AreEqual(-40.25f, scroller.GetSegmentLocalZ(first, distance), 1e-3f);
        }
    }
}
