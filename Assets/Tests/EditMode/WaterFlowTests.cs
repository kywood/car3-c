using System;
using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class WaterFlowTests
    {
        [Test]
        public void Offset_FollowsDistanceWithinOneTile()
        {
            Assert.AreEqual(0f, WaterFlow.GetOffset(0d, 64d), 1e-5f);
            Assert.AreEqual(12.5f, WaterFlow.GetOffset(12.5d, 64d), 1e-5f);
        }

        [Test]
        public void Offset_WrapsByTileLength()
        {
            Assert.AreEqual(6f, WaterFlow.GetOffset(134d, 64d), 1e-5f);
        }

        [Test]
        public void Offset_StaysPreciseAtLargeDistance()
        {
            Assert.AreEqual(0.25f, WaterFlow.GetOffset(64d * 1_000_000d + 0.25d, 64d), 1e-4f);
        }

        [Test]
        public void Offset_IsNeverNegative()
        {
            Assert.AreEqual(54f, WaterFlow.GetOffset(-10d, 64d), 1e-5f);
        }

        [Test]
        public void Offset_RejectsInvalidTileLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WaterFlow.GetOffset(1d, 0d));
        }
    }
}
