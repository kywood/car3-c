using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 날씨(맑음/비/눈)와 밤낮 상태. 비와 눈은 동시에 켜지지 않으며, 같은 버튼을 다시 누르면 맑음으로 돌아간다.
    /// 상태가 바뀌면 Changed로 알린다.
    /// </summary>
    public sealed class EnvironmentController
    {
        public Weather Weather { get; private set; } = Weather.Clear;
        public bool IsNight { get; private set; }
        /// <summary>무지개 도로 모드. 날씨·밤낮과 독립적으로 켜고 끈다.</summary>
        public bool IsRainbowRoad { get; private set; }

        public event Action Changed;

        public void ToggleRainbowRoad()
        {
            IsRainbowRoad = !IsRainbowRoad;
            Changed?.Invoke();
        }

        public void ToggleRain()
        {
            SetWeather(Weather == Weather.Rain ? Weather.Clear : Weather.Rain);
        }

        public void ToggleSnow()
        {
            SetWeather(Weather == Weather.Snow ? Weather.Clear : Weather.Snow);
        }

        public void ToggleNight()
        {
            IsNight = !IsNight;
            Changed?.Invoke();
        }

        public void SetWeather(Weather weather)
        {
            if (Weather == weather) return;
            Weather = weather;
            Changed?.Invoke();
        }
    }
}
