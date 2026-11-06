using System;

namespace Car3C.Gameplay
{
    public enum MapZone
    {
        City,
        Harbor,
        Plain,
        Launch,
        Tunnel,
    }

    /// <summary>배경 배치 지점이 나타날 구간 묶음.</summary>
    [Flags]
    public enum MapZoneMask
    {
        None = 0,
        City = 1 << MapZone.City,
        Harbor = 1 << MapZone.Harbor,
        Plain = 1 << MapZone.Plain,
        Launch = 1 << MapZone.Launch,
        Tunnel = 1 << MapZone.Tunnel,
        AllOutdoor = City | Harbor | Plain | Launch,
    }
}
