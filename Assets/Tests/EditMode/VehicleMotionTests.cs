using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class VehicleMotionTests
    {
        private static VehicleMotion CreateMotion()
        {
            return new VehicleMotion(new LaneLayout(5, 3.5f), 2, 0.2f);
        }

        [Test]
        public void StartsCenteredOnLaneTwo()
        {
            VehicleMotion motion = CreateMotion();
            Assert.AreEqual(2, motion.CurrentLane);
            Assert.AreEqual(0f, motion.LateralOffset, 1e-5f);
            Assert.IsFalse(motion.IsChangingLane);
        }

        [Test]
        public void LaneChange_ReachesTargetAfterDuration()
        {
            VehicleMotion motion = CreateMotion();
            Assert.IsTrue(motion.RequestLaneChange(1));

            motion.Tick(0.1f);
            Assert.IsTrue(motion.IsChangingLane);
            Assert.AreEqual(1.75f, motion.LateralOffset, 1e-4f);

            motion.Tick(0.1f);
            Assert.AreEqual(3.5f, motion.LateralOffset, 1e-4f);
            Assert.IsFalse(motion.IsChangingLane);
        }

        [Test]
        public void LaneChange_NeverOvershootsTarget()
        {
            VehicleMotion motion = CreateMotion();
            motion.RequestLaneChange(-1);
            motion.Tick(1f);
            Assert.AreEqual(-3.5f, motion.LateralOffset, 1e-4f);
            Assert.AreEqual(1, motion.CurrentLane);
        }

        [Test]
        public void LaneChange_BeyondEdge_IsRejected()
        {
            VehicleMotion motion = CreateMotion();
            motion.RequestLaneChange(1);
            motion.RequestLaneChange(1);
            Assert.IsFalse(motion.RequestLaneChange(1));
            motion.Tick(1f);
            Assert.AreEqual(4, motion.CurrentLane);
            Assert.AreEqual(7f, motion.LateralOffset, 1e-4f);
        }
    }
}
