# car3-c — Claude Code 프로젝트 규칙

## 프로젝트
- Unity 6 / C# / Android 우선, 5차선 3D 아케이드 자동차 주행 게임.
- Unity 프로젝트 루트: `D:\project\wood\unity\car3-c`.
- 기준 소스: 사용자 제공 MS2 (`Client/M2/Assets/Scripts`)의 구조와 디자인 의도.
- `docs/CODING_CONVENTIONS.md`를 반드시 먼저 읽고 신규 코드에 적용한다.
- MS2 소스는 읽기 전용, 기존 게임의 그래픽·음향·고유 게임명 복제 금지.

## Unity Agent Plugin
1. `unity@unity-agent-plugin` 설치 및 활성화 상태를 실제로 확인한다.
2. Unity 작업 전 관련 스킬의 실제 명칭·내용을 조회한 뒤 적용한다. 존재하지 않는 Skill 이름을 꾸며내지 않는다.
3. 명령은 `unity commands --grep <term>` 또는 `unity <subcommand> --help`로 지원 여부를 확인한다.
4. `ProjectSettings/ProjectVersion.txt` 존재 확인 후 `unity status`로 연결 확인. 실패 시 프로젝트 생성/연결 상태부터 진단한다.
5. 가능한 경우 CLI로 Scene, Editor Console, 컴파일, PlayMode, 테스트, Android 빌드를 확인하고 **실행한 명령과 실제 결과**를 기록한다.

## 네트워크 및 서버 범위 (확정)
- 멀티플레이 구현 시 **Photon Fusion** 사용. 별도의 Python/C# 자체 게임 서버, MS2 서버, 기존 소켓/패킷 프로토콜은 구현·포팅하지 않는다.
- 최초 MVP는 **싱글플레이**로 개발하고 Photon Fusion 연동은 후속 단계로 둔다.
- 게임 규칙을 네트워크 API로부터 분리하여 추후 Photon Fusion 연동 시 교체 범위를 최소화한다.
- MS2 `Network/` 코드는 코드 컨벤션 참고 대상에서 제외한다. 재사용 대상은 클라이언트 상태 패턴, 풀링, 입력/게임 로직 구조 등으로 한정한다.
- Photon Fusion SDK 설치 및 연결·동기화 작업은 실제 사용 단계에서 별도 진행하고, 설치 전 Photon API가 이미 동작한다고 가정하지 않는다.

## 아키텍처
- Core/Gameplay/Presentation/Infrastructure/Bootstrap 분리.
- MonoBehaviour는 어댑터, 핵심 로직은 순수 C# 클래스로 분리.
- MS2의 상태 패턴(`OnEnter/OnLeave/OnUpdate`), 오브젝트 풀, Observer/Queue 설계를 상황에 맞게 계승.
- SOLID/OOP, 명시적 의존성, 성능·GC 고려, 무분별한 Singleton 방지.
- 클래스/메서드/프로퍼티 PascalCase, private 필드 `_camelCase`, 인스펙터 필드 `[SerializeField] private`.

## 작업 루프
계획 → 소규모 구현 → 컴파일 → 테스트 → 로그/오류 확인 → 수정 → 재검증 → 결과 보고.
검증되지 않은 성공을 주장하지 않는다. 씬/에셋 변경 전 Git 상태를 확인하고 사용자 작업을 덮어쓰지 않는다.

## 초기 MVP 작업 순서
1. Unity Editor 연결 및 Skill 적용 확인.
2. 5차선 도로(차선 0~4, 시작 차선 2), 독자적인 단순 차량, 추적 카메라.
3. 모바일 좌우 차선 이동과 자동 전진.
4. 장애물 생성·풀링·충돌.
5. 점수/게임오버/재시작.
6. Android 빌드 및 실기기 검증.
7. 후속 단계에서 Photon Fusion 멀티플레이 설계·연동 (현재 구현하지 않음).

## 차선 설계 (확정: 5차선)
- 차선 수 5, 차선 인덱스 0~4, 시작 차선 2(중앙).
- 좌우 입력 1회당 한 차선 이동, 0 미만·4 초과로는 이동하지 않는다.
- 차선 수·차선 폭·시작 차선은 코드에 하드코딩하지 않고 `TrackSettings` 에셋(`Assets/Settings/Game/TrackSettings.asset`)에서 관리한다.
- 도로, 장애물 생성, 충돌 판정은 모두 같은 `LaneLayout`(TrackSettings로 생성)을 기준으로 계산한다.

## 주행 설계 (확정)
- 플레이어 차량은 전진하지 않는다. 도로와 교통 차량이 속도만큼 다가온다(`SpeedController.Distance`로 스크롤).
- 카메라는 앞뒤로 고정, 좌우만 따라간다. 속도가 기본보다 높으면 차량이 화면 앞쪽으로(`ForwardOffset`), 낮으면 뒤로 밀린다.
- 교통 차량을 가까이 스쳐 지나가면(근접 추월) 가속, 연속이면 콤보 보너스. 부딪히면 감속 후 잠시 무적.
- 교통 차량은 생성 시 앞으로 나란히 설 가능성까지 계산해 항상 피할 차선을 하나 이상 남긴다.
- 조정값은 `DriveSettings`, `TrafficSettings` 에셋(`Assets/Settings/Game/`)에서 관리한다.
- 특정 기존 게임의 이름·그래픽·고유 요소는 복제하지 않고, 장르 공통 메커닉만 사용한다.

## 외부 에셋
- 외부 무료 에셋은 `Assets/ThirdParty/<제작자>/`에 두고 원본 License.txt를 함께 보관하며, `Assets/ThirdParty/CREDITS.md`에 출처·라이선스를 기록한다.
- 라이선스가 확인된 것(CC0 등)만 사용한다. 현재: Quaternius 로우폴리 팩(CC0).
- 배경은 `SceneryView`가 40m 구간 단위로 풀링·스크롤하며, 배치 지점과 후보 프리팹은 씬의 Scenery 오브젝트에서 조정한다.
- 원경 산은 FarScenery(160m 구간), 터널은 Scenery의 Tunnel 설정(`TunnelSchedule`)으로 반복 배치한다.
- 차량 모델은 `Assets/Vehicles`(car3에서 GUID 유지 복사, 출처는 `Vehicles/SOURCES.md`). Cybercab은 출처 미확인.

## 맵 구간 (맵 툴 도입 전 임시)
- `SceneryView`의 구간 표: 항구 6 → 평지 3 → 발사장 5 → 도시 6 → 터널 4 (구간 40m, 960m 반복). 순수 로직은 `MapZoneSchedule`.
- 배치 지점(`ScenerySpot`)은 `MapZoneMask`로 나타날 구간을 지정한다. 터널·발사장·경전철 위치는 모두 구간 표에서 계산한다.
- 향후 맵 툴이 만들 데이터는 이 구간 표/배치 지점을 대체한다.

## 맵 이벤트·기능
- 우주 발사장: `RocketLaunchSequence`(순수, 시작 시각만으로 상태 계산 → Photon 동기화는 시작 시각 하나), `LaunchSiteSchedule`, `LaunchSiteController`(배치·이벤트 시작, `LaunchRequested`/`BeginLaunch`로 네트워크 확장), `RocketLaunchView`/`LaunchCountdownView`/`LaunchAudio`(연출·사운드 훅). 카메라 흔들림은 기본 꺼짐.
- 고가 경전철은 항구 구간에만: `RailRoute`(곡선 노선) + `RailLineView`.
- 점프: `JumpMotion`(순수), Space/W/↑, 위로 스와이프, JUMP 버튼. 공중에서는 차량 위를 넘어가며 근접 추월로 계산.
- 충돌한 교통 차량은 돌며 깜빡이다 `CrashRemoveSeconds` 뒤 사라진다.

## 렌더링·환경 (실사풍)
- 재질: Poly Haven CC0 PBR 텍스처, 하늘: Poly Haven HDRI(맑음/흐림/밤) + `Skybox/Panoramic`.
- 태양광 방향은 낮 HDRI의 태양 위치(고도 9.1°)에 맞춘다. 파노라마 스카이박스 `_Rotation`은 하늘 구를 회전시키므로 회전값 = 목표 방위각 − 텍스처 방위각.
- 날씨/밤낮 상태는 순수 C# `EnvironmentController`, 표현은 `EnvironmentView`(하늘·태양·안개·비/눈 입자·야간 조명), 버튼은 `EnvironmentButtonsView`.
- 머티리얼 에셋은 런타임에 수정하지 않는다(야간 조명은 `NightLight`로 오브젝트 켜고 끄기).
- 후처리: `Assets/Settings/GameVolumeProfile.asset`(ACES, Bloom, Color Adjustments, Vignette).
- 날씨별 바닥: `WeatherSurfaceView`가 노면 머티리얼을 마른/젖은/눈 변형으로 교체한다. 눈 도로 텍스처(`Assets/Art/Textures/Road_SnowTracks.png`)는 TrackSettings의 차선 위치로 바퀴 자국을 그려 만든다(차선 수·폭을 바꾸면 재생성).
- 야간 조명은 실시간 조명 대신 가산 블렌딩 빛 웅덩이(FX_*Pool)와 발광 머티리얼로 표현한다. 실시간 조명은 플레이어 전조등 2개뿐.
- 모바일 예산: 교통 차량 16대, 배경 구간 8개, 건물·소품 LODGroup 컬링, Quaternius 모델 Mesh LOD, Mobile_RPAsset 그림자 60m·캐스케이드 1, FXAA. `Assets/Vehicles`의 원본 FBX 설정은 바꾸지 않는다.
- 측정(에디터): 화면에 보이는 렌더러 약 570개, LOD0 기준 약 26만 삼각형. 실기기 FPS는 아직 미검증.


## Git Branch Development Policy

모든 기능 추가 및 버그 수정은 반드시 별도 작업 브랜치에서 진행한다.

1. main/master 브랜치에서 직접 코드를 수정하지 않는다.
2. 작업 시작 전에 Git 상태를 확인한다.
3. TODO마다 독립적인 작업 브랜치를 생성한다.
   - feature/<task-name>
   - fix/<task-name>
4. 기존 미커밋 변경 사항을 임의로 삭제하거나 덮어쓰지 않는다.
5. Claude Code는 해당 작업 브랜치에서만 코드를 수정한다.
6. Unity 빌드와 테스트 결과를 확인한다.
7. 검증된 변경 사항을 Git에 커밋한다.
8. 원격 저장소의 작업 브랜치로 Push한다.
9. main/master로의 Merge는 사용자 승인 후 진행한다.
10. force push 및 위험한 Git 명령은 사용자 승인 없이 실행하지 않는다.

Git 브랜치 생성, Commit, Push는 Python Harness가 관리한다.
Claude Code는 임의로 브랜치를 변경하거나 Commit/Push하지 않는다.
