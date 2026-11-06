using System;
using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class LaneControllerTests
    {
        [Test]
        public void TryMove_MovesOneLaneAtATime()
        {
            LaneController lanes = new LaneController(5, 2);
            Assert.IsTrue(lanes.TryMove(1));
            Assert.AreEqual(3, lanes.CurrentLane);
            Assert.IsTrue(lanes.TryMove(-1));
            Assert.AreEqual(2, lanes.CurrentLane);
        }

        [Test]
        public void TryMove_StopsAtLeftmostLane()
        {
            LaneController lanes = new LaneController(5, 2);
            Assert.IsTrue(lanes.TryMove(-1));
            Assert.IsTrue(lanes.TryMove(-1));
            Assert.IsFalse(lanes.TryMove(-1));
            Assert.AreEqual(0, lanes.CurrentLane);
        }

        [Test]
        public void TryMove_StopsAtRightmostLane()
        {
            LaneController lanes = new LaneController(5, 2);
            Assert.IsTrue(lanes.TryMove(1));
            Assert.IsTrue(lanes.TryMove(1));
            Assert.IsFalse(lanes.TryMove(1));
            Assert.AreEqual(4, lanes.CurrentLane);
        }

        [Test]
        public void Constructor_InvalidInitialLane_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaneController(5, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaneController(5, -1));
        }
    }
}
