using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// RectTransform을 기기의 안전 영역(Screen.safeArea)에 맞춘다. 노치·펀치홀·내비게이션 바에 UI가 가려지지 않게 한다.
    /// 화면 회전이나 해상도 변경 시에만 다시 계산한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _appliedArea;
        private Vector2Int _appliedScreen;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _appliedArea || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y) Apply();
        }

        private void Apply()
        {
            Rect area = Screen.safeArea;
            _appliedArea = area;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            _rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            _rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
