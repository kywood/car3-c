# MS2 기반 Unity 6 C# 코딩 컨벤션

분석 범위: 업로드된 `MS2-master.zip`의 `Client/M2/Assets/Scripts` 하위 **116개 C# 파일**. 기준은 기존 프로젝트의 **설계 의도**이며, 일관되지 않은 표기까지 무조건 복제하지 않는다.

## 실제 소스에서 확인한 특징

| 주제 | 확인된 관례 | 근거 파일 |
|---|---|---|
| 클래스/메서드 | `PascalCase` 클래스(`GameManager`, `PlayerStateManager`) 및 메서드(`SetState`, `OnEnter`) | `State/StateManager.cs`, `State/State.cs` |
| 비공개 필드 | `_networkManager`, `_stateManager`, `_session`처럼 `_camelCase`가 흔하지만 `mStateMap`, `_StateManager`, `PoolList` 등 예외 있음 | `App/AppManager.cs`, `State/StateManager.cs`, `State/Game/GameManager.cs`, `Res/ResPools.cs` |
| 프로퍼티 | `PascalCase`, 내부 필드 감싼 공개 접근자 | `App/AppManager.cs`, `State/Game/Player/Player.cs` |
| 상태 기계 | `State<T>`의 `OnEnter/OnLeave/OnUpdate`, `StateManager.SetState`, 구체 상태 클래스 매핑 | `State/State.cs`, `State/StateManager.cs`, `State/Game/GameState/GameStateManager.cs` |
| Unity 수명주기 | `Awake/Start/Update`에서 커스텀 `OnAwake/OnStart/OnUpdate` 호출하여 상속 확장 | `State/Game/Player/Player.cs`, `Game/BubbleManager.cs` |
| Manager/책임 | `AppManager`, `NetworkManager`, `GameManager`, `PlayerStateManager` 등 하위 시스템 관리 | `App/AppManager.cs`, `Network/NetworkManager.cs` |
| Observer | 인터페이스로 등록/제거/통지 | `Network/Game/IObserver.cs`, `Network/Game/AbSubJect.cs` |
| 오브젝트 풀 | 미리 생성하고 `SetActive`로 활성화/재사용 | `Res/Pool.cs`, `Res/ResPools.cs` |
| 큐/동기화 | `Queue<T>`와 `lock`으로 이벤트를 수집 후 메인 업데이트에서 처리 | `Network/Packet/PacketQueue.cs`, `Network/NetworkManager.cs` |
| 싱글턴 | `Instance` 접근 패턴 존재 | `App/Apps.cs`, `State/Game/GameManager.cs`, `Network/Packet/PacketQueue.cs` |
| 열거형/상수 | `E_GAME_STATE`, `E_PLAYER_STATE`, `E_SCENES`, `G_*` 등 C++식 대문자 표기 | `State/Game/GameState/GameStateManager.cs`, `Defines.cs` |
| Namespace | 일부에서만 사용, 전체적으로 일관되게 적용되지는 않음 | `Res/ResPools.cs` 등 |

## 네트워크 코드에 대한 예외

- MS2 `Network/`, `PacketQueue`, `Session`, 자체 소켓 통신, 패킷 직렬화와 자체 서버 코드는 **역사적 관찰 기록**일 뿐, 새 게임의 구현 기준이 아니다.
- 실제 게임 제작 시 네트워크는 **Photon Fusion**으로 구성하며, 초기 싱글플레이 MVP에는 네트워크 구현을 넣지 않는다.
- Observer/Queue 패턴은 필요 시 일반 게임 이벤트 처리 용도로만 참고한다.

## 새 프로젝트에 적용할 통일 규칙

1. 클래스, 인터페이스, 메서드, 프로퍼티: `PascalCase`. 인터페이스는 `I` 접두사 (`IObstacleSpawner`).
2. `private`/`protected` 인스턴스 필드: `_camelCase`. 로컬 변수/매개변수: `camelCase`.
3. 상수: C# 일반 관례인 `PascalCase` (`MaxLanes`)를 기본으로 하고, 원본의 `G_*`, `E_*`, `m*`, `c*`는 레거시 이름을 참조할 때만 유지한다.
4. 각 스크립트는 가능하면 하나의 주요 public 타입을 포함하고 파일명과 타입명을 일치시킨다.
5. 클래스와 공개 메서드는 동작과 책임이 드러나도록 이름 짓는다. 불명확한 축약형은 새 코드에 사용하지 않는다.
6. 네임스페이스는 새 프로젝트에서 `Car3C`를 루트로 사용하고 기능별로 `Car3C.Core`, `Car3C.Gameplay`, `Car3C.Presentation`, `Car3C.Infrastructure`로 구분한다.
7. **원본 아키텍처 계승**: 상태 전환은 명시적 상태 기계, 복수 오브젝트는 풀링, 메시지/이벤트는 소비 주체가 명확한 구조로 설계한다.
8. **Unity 어댑터 분리**: `MonoBehaviour`는 입력·씬 참조·Unity 수명주기 연결만 담당하고, 속도/차선/점수 등 비즈니스 규칙은 순수 C# 클래스로 둔다.
9. Unity 기본 `Awake`, `Start`, `Update`를 우선하고, 공통 생명주기 기반 클래스가 필요한 경우에만 원본의 `OnAwake/OnStart/OnUpdate` 래핑을 사용한다. `OnEnable/OnDisable`로 이벤트를 등록/해제한다.
10. 싱글턴을 기본값으로 사용하지 않는다. 서비스 간 의존성은 생성자 주입(순수 C#), 명시적 초기화 및 `[SerializeField]` 참조를 우선한다.
11. 인스펙터 필드는 `private [SerializeField]`, 외부 접근은 필요한 읽기 전용 프로퍼티/메서드로 제한한다.
12. 매 프레임 `Find`, 반복 `GetComponent`, 불필요한 LINQ/할당, 로그 남발을 피한다. 플레이어와 장애물은 필요에 맞게 풀링한다.
13. 상태 전환, 충돌 판정, 점수 계산은 EditMode 단위 테스트를 작성한다. PlayMode에서 씬/물리/입력을 검증한다.
14. 새 파일에는 임시 주석 코드와 사용하지 않는 using을 남기지 않는다. 오류를 삼키거나 테스트 결과를 추측하지 않는다.

## 권장 기능 디렉터리

```text
Assets/Scripts/
  Core/             # 상태 기계, 공통 인터페이스, 이벤트
  Gameplay/         # Vehicle, Lane, Obstacle, Score, GameState
  Presentation/     # Camera, UI, View, Effects
  Infrastructure/   # Pool, Save, DeviceInput
  Bootstrap/        # Unity 씬 의존성 및 초기화
Assets/Tests/EditMode/
Assets/Tests/PlayMode/
```

## 신규 코드 예시

```csharp
namespace Car3C.Gameplay
{
    public sealed class LaneController
    {
        private readonly int _laneCount;
        private int _currentLane;

        public int CurrentLane => _currentLane;

        public LaneController(int laneCount, int initialLane)
        {
            if (laneCount < 1) throw new System.ArgumentOutOfRangeException(nameof(laneCount));
            if (initialLane < 0 || initialLane >= laneCount) throw new System.ArgumentOutOfRangeException(nameof(initialLane));
            _laneCount = laneCount;
            _currentLane = initialLane;
        }

        public bool TryMove(int delta)
        {
            int nextLane = _currentLane + delta;
            if (nextLane < 0 || nextLane >= _laneCount) return false;
            _currentLane = nextLane;
            return true;
        }
    }
}
```

## 원본과 의도적으로 달라지는 지점

- 원본은 `_field`, `mField`, 대문자 public 필드가 섞여 있다 → 신규 코드에서는 `_camelCase` + 캡슐화로 정리.
- 원본의 `E_*`, `G_*`, `c*` 표기는 역사적 맥락으로 기록하되 신규 코드 표준은 C#식 표기 사용.
- 원본 Singleton/Resources.Load/전역 매니저에 기대는 패턴은 그대로 확대하지 않는다.
- 원본 소스에는 전용 네임스페이스가 없는 파일이 다수다 → 새 프로젝트는 명시적 네임스페이스 사용.

**주의:** 이는 실제 코드 기반의 '스타일 계승 + 현대화' 규칙이다. 모든 116개 파일에서 예외 없이 지켜진 규칙이라고 주장하지 않는다.
