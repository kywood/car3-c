using System;
using System.Collections.Generic;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 도로를 따라 반복되는 구간 순서(예: 항구 → 평지 → 발사장 → 도시 → 터널)를 배경 구간(chunk) 단위로 정한다.
    /// 터널·발사장 위치도 이 표에서 계산해 배경과 이벤트가 항상 맞물리게 한다.
    /// </summary>
    public sealed class MapZoneSchedule
    {
        private readonly MapZone[] _zoneByChunk;
        private readonly float _chunkLength;

        public int CycleChunks => _zoneByChunk.Length;
        public float ChunkLength => _chunkLength;
        public double CycleDistance => _zoneByChunk.Length * (double)_chunkLength;

        public MapZoneSchedule(IReadOnlyList<(MapZone zone, int chunks)> segments, float chunkLength)
        {
            if (segments == null || segments.Count == 0) throw new ArgumentException("구간이 비어 있습니다.", nameof(segments));
            if (chunkLength <= 0f) throw new ArgumentOutOfRangeException(nameof(chunkLength));
            var list = new List<MapZone>();
            foreach (var (zone, chunks) in segments)
            {
                if (chunks < 1) throw new ArgumentOutOfRangeException(nameof(segments), "구간 길이는 1 이상이어야 합니다.");
                for (int i = 0; i < chunks; ++i) list.Add(zone);
            }
            _zoneByChunk = list.ToArray();
            _chunkLength = chunkLength;
        }

        public MapZone GetZone(long chunkIndex)
        {
            long i = chunkIndex % _zoneByChunk.Length;
            if (i < 0) i += _zoneByChunk.Length;
            return _zoneByChunk[i];
        }

        public static bool Contains(MapZoneMask mask, MapZone zone)
        {
            return ((int)mask & (1 << (int)zone)) != 0;
        }

        /// <summary>한 주기 안에서 zone이 처음 시작하는 구간 번호와 길이. 없으면 (-1, 0).</summary>
        public (int firstChunk, int chunks) FindZone(MapZone zone)
        {
            for (int i = 0; i < _zoneByChunk.Length; ++i)
            {
                if (_zoneByChunk[i] != zone) continue;
                int length = 0;
                while (i + length < _zoneByChunk.Length && _zoneByChunk[i + length] == zone) ++length;
                return (i, length);
            }
            return (-1, 0);
        }

        /// <summary>zone 구간 가운데 지점의 첫 주기 주행 거리. 없으면 -1.</summary>
        public double GetZoneCenterDistance(MapZone zone)
        {
            var (first, chunks) = FindZone(zone);
            return first < 0 ? -1d : (first + chunks * 0.5d) * _chunkLength;
        }

        /// <summary>터널 구간이 있으면 같은 위치의 TunnelSchedule을 만든다.</summary>
        public TunnelSchedule CreateTunnelSchedule()
        {
            var (first, chunks) = FindZone(MapZone.Tunnel);
            return first < 0 ? null : new TunnelSchedule(first, _zoneByChunk.Length, chunks, _chunkLength);
        }
    }
}
