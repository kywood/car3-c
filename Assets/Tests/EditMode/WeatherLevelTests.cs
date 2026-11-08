using System.Collections.Generic;
using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class WeatherLevelTests
    {
        [Test]
        public void RainButton_GoesDrizzle_Heavy_ThenOff()
        {
            var env = new EnvironmentController();
            env.CycleRain();
            Assert.AreEqual((Weather.Rain, 0), (env.Weather, env.Level));
            env.CycleRain();
            Assert.AreEqual((Weather.Rain, 1), (env.Weather, env.Level));
            env.CycleRain();
            Assert.AreEqual(Weather.Clear, env.Weather);
        }

        [Test]
        public void SnowButton_HasThreeLevels_ThenOff()
        {
            var env = new EnvironmentController();
            var seen = new List<int>();
            for (int i = 0; i < 3; ++i)
            {
                env.CycleSnow();
                Assert.AreEqual(Weather.Snow, env.Weather);
                seen.Add(env.Level);
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, seen);
            env.CycleSnow();
            Assert.AreEqual(Weather.Clear, env.Weather);
        }

        [Test]
        public void SwitchingWeather_StartsAtFirstLevel()
        {
            var env = new EnvironmentController();
            env.CycleRain();
            env.CycleRain();
            env.CycleSnow();
            Assert.AreEqual((Weather.Snow, 0), (env.Weather, env.Level));
            env.CycleRain();
            Assert.AreEqual((Weather.Rain, 0), (env.Weather, env.Level));
        }

        [Test]
        public void SkyButton_ClearsWeather_ThenAlternatesSky_AndSkyIsRemembered()
        {
            var env = new EnvironmentController();
            Assert.AreEqual(0, env.ClearLevel);
            env.CycleSky();
            Assert.AreEqual((Weather.Clear, 1), (env.Weather, env.Level));

            env.CycleRain();
            env.CycleSky();
            Assert.AreEqual((Weather.Clear, 1), (env.Weather, env.Level), "비를 그치면 고른 하늘로 돌아간다");
            env.CycleSky();
            Assert.AreEqual(0, env.Level);
        }

        [Test]
        public void Set_RaisesChangedOnlyOnChange_AndRejectsBadLevel()
        {
            var env = new EnvironmentController();
            int changes = 0;
            env.Changed += () => ++changes;
            env.Set(Weather.Snow, 2);
            env.Set(Weather.Snow, 2);
            Assert.AreEqual(1, changes);
            Assert.AreEqual(2, env.Level);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => env.Set(Weather.Rain, 2));
        }
    }
}
