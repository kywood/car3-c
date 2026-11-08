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

            Rect normalized = Normalize(area, Screen.width, Screen.height);
            _rect.anchorMin = normalized.min;
            _rect.anchorMax = normalized.max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 안전 영역을 0~1 비율로 바꾼다. 에뮬레이터 등이 화면 밖·0 크기·회전 전 좌표 같은 비정상 값을 주면
        /// HUD가 사라지지 않도록 전체 화면(0~1)을 쓴다.
        /// </summary>
        public static Rect Normalize(Rect safeArea, float screenWidth, float screenHeight)
        {
            Rect full = new Rect(0f, 0f, 1f, 1f);
            if (screenWidth <= 0f || screenHeight <= 0f) return full;

            float xMin = safeArea.xMin / screenWidth, xMax = safeArea.xMax / screenWidth;
            float yMin = safeArea.yMin / screenHeight, yMax = safeArea.yMax / screenHeight;
            bool finite = !(float.IsNaN(xMin) || float.IsNaN(xMax) || float.IsNaN(yMin) || float.IsNaN(yMax));
            bool inside = xMin >= -0.01f && yMin >= -0.01f && xMax <= 1.01f && yMax <= 1.01f;
            bool bigEnough = xMax - xMin >= 0.6f && yMax - yMin >= 0.6f;
            if (!finite || !inside || !bigEnough) return full;

            return Rect.MinMaxRect(Mathf.Clamp01(xMin), Mathf.Clamp01(yMin), Mathf.Clamp01(xMax), Mathf.Clamp01(yMax));
        }
    }
}
