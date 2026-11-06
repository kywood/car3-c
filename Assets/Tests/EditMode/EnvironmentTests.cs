using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class EnvironmentTests
    {
        [Test]
        public void StartsClearDay()
        {
            EnvironmentController env = new EnvironmentController();
            Assert.AreEqual(Weather.Clear, env.Weather);
            Assert.IsFalse(env.IsNight);
        }

        [Test]
        public void RainAndSnow_AreExclusive_AndToggleBackToClear()
        {
            EnvironmentController env = new EnvironmentController();
            env.ToggleRain();
            Assert.AreEqual(Weather.Rain, env.Weather);
            env.ToggleSnow();
            Assert.AreEqual(Weather.Snow, env.Weather);
            env.ToggleSnow();
            Assert.AreEqual(Weather.Clear, env.Weather);
        }

        [Test]
        public void Night_IsIndependentOfWeather()
        {
            EnvironmentController env = new EnvironmentController();
            env.ToggleRain();
            env.ToggleNight();
            Assert.IsTrue(env.IsNight);
            Assert.AreEqual(Weather.Rain, env.Weather);
            env.ToggleNight();
            Assert.IsFalse(env.IsNight);
        }

        [Test]
        public void Changed_FiresOnlyOnRealChange()
        {
            EnvironmentController env = new EnvironmentController();
            int count = 0;
            env.Changed += () => ++count;
            env.ToggleRain();
            env.SetWeather(Weather.Rain);
            env.ToggleNight();
            Assert.AreEqual(2, count);
        }

        [Test]
        public void RainbowRoad_TogglesIndependently()
        {
            EnvironmentController env = new EnvironmentController();
            int count = 0;
            env.Changed += () => ++count;
            env.ToggleSnow();
            env.ToggleRainbowRoad();
            Assert.IsTrue(env.IsRainbowRoad);
            Assert.AreEqual(Weather.Snow, env.Weather);
            env.ToggleRainbowRoad();
            Assert.IsFalse(env.IsRainbowRoad);
            Assert.AreEqual(3, count);
        }

        [Test]
        public void TunnelSchedule_RepeatsWithEntranceAndExit()
        {
            TunnelSchedule tunnels = new TunnelSchedule(firstChunk: 10, period: 20, length: 4, chunkLength: 40f);
            Assert.IsFalse(tunnels.IsTunnel(9));
            Assert.IsTrue(tunnels.IsEntrance(10));
            Assert.IsTrue(tunnels.IsTunnel(12));
            Assert.IsTrue(tunnels.IsExit(13));
            Assert.IsFalse(tunnels.IsTunnel(14));
            Assert.IsTrue(tunnels.IsEntrance(30));
            Assert.IsFalse(tunnels.IsEntrance(31));
        }

        [Test]
        public void TunnelSchedule_InsideAtDistance()
        {
            TunnelSchedule tunnels = new TunnelSchedule(10, 20, 4, 40f);
            Assert.IsFalse(tunnels.IsInsideAt(399.9));
            Assert.IsTrue(tunnels.IsInsideAt(400.0));
            Assert.IsTrue(tunnels.IsInsideAt(559.9));
            Assert.IsFalse(tunnels.IsInsideAt(560.0));
        }
    }
}
