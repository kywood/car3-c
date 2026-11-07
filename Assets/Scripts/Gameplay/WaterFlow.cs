using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 고정된 바다 평면의 물결 무늬를 주행 거리만큼 흘려보내기 위한 오프셋 계산.
    /// 물결 무늬가 TileLength 주기로 반복되므로 거리를 그 주기로 감아 큰 거리에서도 float 정밀도를 유지한다.
    /// </summary>
    public static class WaterFlow
    {
        public static float GetOffset(double distance, double tileLength)
        {
            if (tileLength <= 0d) throw new ArgumentOutOfRangeException(nameof(tileLength));
            double wrapped = distance % tileLength;
            if (wrapped < 0d) wrapped += tileLength;
            return (float)wrapped;
        }
    }
}
