namespace Car3C.Gameplay
{
    /// <summary>
    /// 배경 구간 번호와 배치 지점 번호로 어떤 소품을 놓을지(또는 비울지) 정한다.
    /// 같은 입력이면 항상 같은 결과를 내므로, 구간을 재사용해도 지나온 배경이 바뀌지 않는다.
    /// </summary>
    public static class SceneryPicker
    {
        public const int Empty = -1;

        public static int Pick(int seed, long chunkIndex, int spotIndex, int optionCount, float emptyChance)
        {
            if (optionCount <= 0) return Empty;

            ulong hash = Mix((ulong)(uint)seed ^ Mix((ulong)chunkIndex * 0x9E3779B97F4A7C15UL ^ (ulong)(uint)spotIndex));
            double roll = (hash >> 11) * (1.0 / (1UL << 53));
            if (roll < emptyChance) return Empty;

            double rest = (roll - emptyChance) / (1.0 - emptyChance);
            int option = (int)(rest * optionCount);
            return option >= optionCount ? optionCount - 1 : option;
        }

        private static ulong Mix(ulong value)
        {
            value ^= value >> 33;
            value *= 0xFF51AFD7ED558CCDUL;
            value ^= value >> 33;
            value *= 0xC4CEB9FE1A85EC53UL;
            value ^= value >> 33;
            return value;
        }
    }
}
