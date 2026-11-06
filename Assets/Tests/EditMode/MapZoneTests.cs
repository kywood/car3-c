using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class MapZoneTests
    {
        private static MapZoneSchedule Create()
        {
            return new MapZoneSchedule(new (MapZone, int)[]
            {
                (MapZone.Harbor, 6), (MapZone.Plain, 3), (MapZone.Launch, 5), (MapZone.City, 6), (MapZone.Tunnel, 4),
            }, 40f);
        }

        [Test]
        public void Zones_FollowRequestedOrder_AndRepeat()
        {
            MapZoneSchedule s = Create();
            Assert.AreEqual(24, s.CycleChunks);
            Assert.AreEqual(MapZone.Harbor, s.GetZone(0));
            Assert.AreEqual(MapZone.Plain, s.GetZone(6));
            Assert.AreEqual(MapZone.Launch, s.GetZone(9));
            Assert.AreEqual(MapZone.City, s.GetZone(14));
            Assert.AreEqual(MapZone.Tunnel, s.GetZone(20));
            Assert.AreEqual(MapZone.Harbor, s.GetZone(24));
            Assert.AreEqual(MapZone.Launch, s.GetZone(24 + 11));
        }

        [Test]
        public void LaunchCenter_And_TunnelSchedule_MatchZones()
        {
            MapZoneSchedule s = Create();
            Assert.AreEqual((9 + 2.5) * 40d, s.GetZoneCenterDistance(MapZone.Launch), 1e-6);
            Assert.AreEqual(960d, s.CycleDistance, 1e-6);

            TunnelSchedule tunnels = s.CreateTunnelSchedule();
            Assert.IsTrue(tunnels.IsEntrance(20));
            Assert.IsTrue(tunnels.IsExit(23));
            Assert.IsFalse(tunnels.IsTunnel(24));
            Assert.IsTrue(tunnels.IsEntrance(44));
        }

        [Test]
        public void Mask_ContainsOnlyListedZones()
        {
            Assert.IsTrue(MapZoneSchedule.Contains(MapZoneMask.City | MapZoneMask.Harbor, MapZone.Harbor));
            Assert.IsFalse(MapZoneSchedule.Contains(MapZoneMask.City | MapZoneMask.Harbor, MapZone.Launch));
            Assert.IsFalse(MapZoneSchedule.Contains(MapZoneMask.AllOutdoor, MapZone.Tunnel));
        }
    }
}
