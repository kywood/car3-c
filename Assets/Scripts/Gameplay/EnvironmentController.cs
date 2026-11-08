using System;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 날씨(맑음/비/눈)와 단계, 밤낮 상태. 비와 눈은 동시에 켜지지 않는다.
    /// 단계: 맑음 0=높은 가을 하늘, 1=구름 / 비 0=이슬비, 1=폭우 / 눈 0=조금, 1=함박눈, 2=폭설.
    /// 같은 날씨 버튼을 누를 때마다 다음 단계로, 마지막 단계에서 누르면 맑음으로 돌아간다.
    /// 상태가 바뀌면 Changed로 알린다. 상태가 작아 네트워크 동기화 시 (날씨, 단계, 밤) 값만 보내면 된다.
    /// </summary>
    public sealed class EnvironmentController
    {
        private readonly int[] _levels = new int[3];

        public Weather Weather { get; private set; } = Weather.Clear;
        public bool IsNight { get; private set; }
        /// <summary>무지개 도로 모드. 날씨·밤낮과 독립적으로 켜고 끈다.</summary>
        public bool IsRainbowRoad { get; private set; }

        /// <summary>현재 날씨의 단계(0부터).</summary>
        public int Level => _levels[(int)Weather];

        /// <summary>맑은 하늘 종류. 비·눈이 그친 뒤 돌아갈 하늘로도 쓰인다.</summary>
        public int ClearLevel => _levels[(int)Weather.Clear];

        public event Action Changed;

        public static int LevelCount(Weather weather)
        {
            switch (weather)
            {
                case Weather.Clear: return 2;
                case Weather.Rain: return 2;
                case Weather.Snow: return 3;
                default: throw new ArgumentOutOfRangeException(nameof(weather), weather, null);
            }
        }

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

        /// <summary>비 버튼: 꺼짐 → 이슬비 → 폭우 → 꺼짐.</summary>
        public void CycleRain() => CycleWeather(Weather.Rain);

        /// <summary>눈 버튼: 꺼짐 → 조금 → 함박눈 → 폭설 → 꺼짐.</summary>
        public void CycleSnow() => CycleWeather(Weather.Snow);

        /// <summary>하늘 버튼: 비·눈 중이면 맑게, 맑으면 하늘 종류를 바꾼다.</summary>
        public void CycleSky()
        {
            if (Weather != Weather.Clear)
            {
                Weather = Weather.Clear;
            }
            else
            {
                _levels[(int)Weather.Clear] = (ClearLevel + 1) % LevelCount(Weather.Clear);
            }
            Changed?.Invoke();
        }

        public void ToggleNight()
        {
            IsNight = !IsNight;
            Changed?.Invoke();
        }

        /// <summary>날씨를 바꾼다. 비·눈은 첫 단계부터 시작한다.</summary>
        public void SetWeather(Weather weather)
        {
            if (Weather == weather) return;
            Weather = weather;
            if (weather != Weather.Clear) _levels[(int)weather] = 0;
            Changed?.Invoke();
        }

        /// <summary>날씨와 단계를 함께 지정한다(네트워크 동기화·저장 복원용).</summary>
        public void Set(Weather weather, int level)
        {
            if (level < 0 || level >= LevelCount(weather)) throw new ArgumentOutOfRangeException(nameof(level));
            if (Weather == weather && Level == level) return;
            Weather = weather;
            _levels[(int)weather] = level;
            Changed?.Invoke();
        }

        private void CycleWeather(Weather weather)
        {
            if (Weather != weather)
            {
                Weather = weather;
                _levels[(int)weather] = 0;
            }
            else if (Level + 1 < LevelCount(weather))
            {
                _levels[(int)weather] = Level + 1;
            }
            else
            {
                Weather = Weather.Clear;
            }
            Changed?.Invoke();
        }
    }
}
