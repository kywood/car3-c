using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class SceneryTests
    {
        [Test]
        public void Pick_IsDeterministic()
        {
            for (long chunk = 0; chunk < 50; ++chunk)
                Assert.AreEqual(SceneryPicker.Pick(1, chunk, 3, 5, 0.3f), SceneryPicker.Pick(1, chunk, 3, 5, 0.3f));
        }

        [Test]
        public void Pick_StaysInRange_AndUsesEveryOption()
        {
            bool[] seen = new bool[4];
            for (long chunk = 0; chunk < 500; ++chunk)
            {
                int pick = SceneryPicker.Pick(7, chunk, 0, 4, 0f);
                Assert.That(pick, Is.InRange(0, 3));
                seen[pick] = true;
            }
            Assert.IsTrue(seen[0] && seen[1] && seen[2] && seen[3]);
        }

        [Test]
        public void Pick_EmptyChanceRoughlyRespected()
        {
            int empty = 0;
            const int samples = 2000;
            for (long chunk = 0; chunk < samples; ++chunk)
                if (SceneryPicker.Pick(3, chunk, 1, 3, 0.5f) == SceneryPicker.Empty) ++empty;
            Assert.That(empty / (float)samples, Is.InRange(0.4f, 0.6f));
        }

        [Test]
        public void Pick_NoOptions_IsEmpty()
        {
            Assert.AreEqual(SceneryPicker.Empty, SceneryPicker.Pick(1, 0, 0, 0, 0f));
        }

        [Test]
        public void RailTrain_AppearsAhead_PassesBy_ThenWaits()
        {
            RailTrainMotion train = new RailTrainMotion(trainSpeed: 20f, spawnZ: 300f, despawnZ: -100f, interval: 5f);
            Assert.IsFalse(train.IsActive);

            train.Tick(0.1f, 20f);
            Assert.IsTrue(train.IsActive);
            Assert.AreEqual(300f, train.Z, 1e-4f);

            train.Tick(1f, 20f);
            Assert.AreEqual(260f, train.Z, 1e-4f);

            for (int i = 0; i < 10; ++i) train.Tick(1f, 20f);
            Assert.IsFalse(train.IsActive);

            train.Tick(4f, 20f);
            Assert.IsFalse(train.IsActive);
            train.Tick(1.1f, 20f);
            Assert.IsTrue(train.IsActive);
        }
    }
}
