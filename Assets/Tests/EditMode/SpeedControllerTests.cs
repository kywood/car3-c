using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class SpeedControllerTests
    {
        internal static DriveConfig CreateConfig()
        {
            return new DriveConfig(baseSpeed: 20f, minSpeed: 10f, maxSpeed: 40f, recoverAcceleration: 5f, boostDecay: 2f,
                nearMissBoost: 4f, comboBonusRatio: 0.5f, comboWindow: 2f, collisionSpeedMultiplier: 0.5f,
                invulnerableDuration: 1f, laneChangeDuration: 0.2f, maxForwardOffset: 8f, maxBackwardOffset: 2f,
                forwardOffsetSharpness: 100f);
        }

        [Test]
        public void StartsAtBaseSpeedAndAccumulatesDistance()
        {
            SpeedController speed = new SpeedController(CreateConfig());
            speed.Tick(0.5f);
            Assert.AreEqual(20f, speed.CurrentSpeed, 1e-4f);
            Assert.AreEqual(10d, speed.Distance, 1e-4d);
        }

        [Test]
        public void NearMiss_BoostsSpeed_AndComboAddsMore()
        {
            SpeedController speed = new SpeedController(CreateConfig());
            speed.ApplyNearMiss();
            Assert.AreEqual(24f, speed.CurrentSpeed, 1e-4f);
            Assert.AreEqual(1, speed.Combo);

            speed.ApplyNearMiss();
            Assert.AreEqual(30f, speed.CurrentSpeed, 1e-4f);
            Assert.AreEqual(2, speed.Combo);
        }

        [Test]
        public void Combo_ResetsAfterWindow()
        {
            SpeedController speed = new SpeedController(CreateConfig());
            speed.ApplyNearMiss();
            speed.Tick(2.5f);
            Assert.AreEqual(0, speed.Combo);
            speed.ApplyNearMiss();
            Assert.AreEqual(1, speed.Combo);
        }

        [Test]
        public void Speed_IsCappedAtMax()
        {
            SpeedController speed = new SpeedController(CreateConfig());
            for (int i = 0; i < 10; ++i) speed.ApplyNearMiss();
            Assert.AreEqual(40f, speed.CurrentSpeed, 1e-4f);
        }

        [Test]
        public void Boost_DecaysBackToBase()
        {
            SpeedController speed = new SpeedController(CreateConfig());
            speed.ApplyNearMiss();
            speed.Tick(1f);
            Assert.AreEqual(22f, speed.CurrentSpeed, 1e-4f);
            speed.Tick(5f);
            Assert.AreEqual(20f, speed.CurrentSpeed, 1e-4f);
        }

        [Test]
        public void Collision_HalvesSpeed_ResetsCombo_AndGrantsInvulnerability()
        {
            SpeedController speed = new SpeedController(CreateConfig());
            speed.ApplyNearMiss();
            Assert.IsTrue(speed.ApplyCollision());
            Assert.AreEqual(12f, speed.CurrentSpeed, 1e-4f);
            Assert.AreEqual(0, speed.Combo);
            Assert.IsTrue(speed.IsInvulnerable);
            Assert.IsFalse(speed.ApplyCollision());
            Assert.AreEqual(12f, speed.CurrentSpeed, 1e-4f);
        }

        [Test]
        public void Collision_NeverDropsBelowMin_AndRecovers()
        {
            SpeedController speed = new SpeedController(CreateConfig());
            speed.ApplyCollision();
            speed.Tick(1.1f);
            speed.ApplyCollision();
            Assert.GreaterOrEqual(speed.CurrentSpeed, 10f);
            speed.Tick(10f);
            Assert.AreEqual(20f, speed.CurrentSpeed, 1e-4f);
        }

        [Test]
        public void ForwardOffset_MovesAheadWhenBoosted_AndBackWhenSlowed()
        {
            SpeedController speed = new SpeedController(CreateConfig());
            Assert.AreEqual(0f, speed.GetTargetForwardOffset(), 1e-4f);

            for (int i = 0; i < 10; ++i) speed.ApplyNearMiss();
            Assert.AreEqual(8f, speed.GetTargetForwardOffset(), 1e-4f);
            speed.Tick(0.001f);
            Assert.Greater(speed.ForwardOffset, 0f);

            SpeedController slowed = new SpeedController(CreateConfig());
            slowed.ApplyCollision();
            Assert.AreEqual(-2f, slowed.GetTargetForwardOffset(), 1e-4f);
        }
    }
}
