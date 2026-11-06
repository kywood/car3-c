using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 배경 구간 번호 기준으로 어디가 터널인지 정한다.
    /// FirstChunk부터 Period 구간마다 Length 구간 길이의 터널이 반복된다.
    /// </summary>
    public sealed class TunnelSchedule
    {
        private readonly long _firstChunk;
        private readonly int _period;
        private readonly int _length;
        private readonly float _chunkLength;

        public float ChunkLength => _chunkLength;

        public TunnelSchedule(long firstChunk, int period, int length, float chunkLength)
        {
            if (firstChunk < 0) throw new ArgumentOutOfRangeException(nameof(firstChunk));
            if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));
            if (period <= length) throw new ArgumentOutOfRangeException(nameof(period), "터널 사이에 바깥 구간이 있어야 합니다.");
            if (chunkLength <= 0f) throw new ArgumentOutOfRangeException(nameof(chunkLength));
            _firstChunk = firstChunk;
            _period = period;
            _length = length;
            _chunkLength = chunkLength;
        }

        public bool IsTunnel(long chunkIndex)
        {
            if (chunkIndex < _firstChunk) return false;
            return (chunkIndex - _firstChunk) % _period < _length;
        }

        public bool IsEntrance(long chunkIndex)
        {
            return IsTunnel(chunkIndex) && !IsTunnel(chunkIndex - 1);
        }

        public bool IsExit(long chunkIndex)
        {
            return IsTunnel(chunkIndex) && !IsTunnel(chunkIndex + 1);
        }

        /// <summary>주행 거리 distance 지점(z = 0에 있는 플레이어 위치)이 터널 안인지.</summary>
        public bool IsInsideAt(double distance)
        {
            return IsTunnel((long)Math.Floor(distance / _chunkLength));
        }
    }
}
