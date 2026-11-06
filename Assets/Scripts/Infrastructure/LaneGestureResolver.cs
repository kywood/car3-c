using System;

namespace Car3C.Infrastructure
{
    /// <summary>
    /// 포인터(터치/마우스) 누름~뗌 정보를 차선 변경 방향으로 바꾼다.
    /// 일정 거리 이상 가로로 밀면 스와이프 방향, 아니면 화면 좌/우 절반 탭으로 판정한다.
    /// </summary>
    public static class LaneGestureResolver
    {
        public static int Resolve(float startX, float endX, float screenWidth, float minSwipeDistance)
        {
            if (screenWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(screenWidth));

            float swipe = endX - startX;
            if (Math.Abs(swipe) >= minSwipeDistance) return Math.Sign(swipe);
            return startX < screenWidth * 0.5f ? -1 : 1;
        }

        /// <summary>위쪽으로 충분히 밀었고 세로 이동이 가로보다 크면 점프 제스처.</summary>
        public static bool IsJumpSwipe(float startX, float startY, float endX, float endY, float minSwipeDistance)
        {
            float dy = endY - startY;
            return dy >= minSwipeDistance && dy > Math.Abs(endX - startX);
        }
    }
}
