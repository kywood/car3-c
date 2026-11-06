using System;
using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class LaneLayoutTests
    {
        [Test]
        public void FiveLanes_CenterLaneIsTwo()
        {
            LaneLayout layout = new LaneLayout(5, 3.5f);
            Assert.AreEqual(2, layout.CenterLane);
            Assert.AreEqual(0f, layout.GetLaneCenter(2), 1e-5f);
        }

        [Test]
        public void FiveLanes_LaneCentersAreSymmetricAndSpacedByWidth()
        {
            LaneLayout layout = new LaneLayout(5, 3.5f);
            Assert.AreEqual(-7f, layout.GetLaneCenter(0), 1e-5f);
            Assert.AreEqual(-3.5f, layout.GetLaneCenter(1), 1e-5f);
            Assert.AreEqual(3.5f, layout.GetLaneCenter(3), 1e-5f);
            Assert.AreEqual(7f, layout.GetLaneCenter(4), 1e-5f);
        }

        [Test]
        public void FiveLanes_BoundariesCoverRoadWidth()
        {
            LaneLayout layout = new LaneLayout(5, 3.5f);
            Assert.AreEqual(17.5f, layout.RoadWidth, 1e-5f);
            Assert.AreEqual(-8.75f, layout.GetBoundary(0), 1e-5f);
            Assert.AreEqual(8.75f, layout.GetBoundary(5), 1e-5f);
        }

        [Test]
        public void GetLaneCenter_OutOfRange_Throws()
        {
            LaneLayout layout = new LaneLayout(5, 3.5f);
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetLaneCenter(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetLaneCenter(5));
        }
    }
}
