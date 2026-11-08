using System;

namespace Car3C.Gameplay
{
    public enum ViewMode
    {
        /// <summary>차 뒤 위에서 보는 기본 추적 시점.</summary>
        Chase,
        /// <summary>운전석 시점.</summary>
        Cockpit,
    }

    /// <summary>
    /// 카메라 시점 상태. 버튼·키 입력으로 바꾸고 Changed로 알린다. 화면 연출(카메라 이동)은 FollowCamera가 한다.
    /// </summary>
    public sealed class ViewModeController
    {
        public ViewMode Mode { get; private set; } = ViewMode.Chase;

        public event Action Changed;

        /// <summary>추적 ↔ 운전석 전환.</summary>
        public void Toggle()
        {
            Set(Mode == ViewMode.Chase ? ViewMode.Cockpit : ViewMode.Chase);
        }

        public void Set(ViewMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            Changed?.Invoke();
        }
    }
}
