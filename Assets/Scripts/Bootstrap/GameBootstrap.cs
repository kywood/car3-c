using System;
using System.Collections.Generic;
using Car3C.Gameplay;
using Car3C.Infrastructure;
using Car3C.Presentation;
using Car3C.Presentation.Launch;
using UnityEngine;

namespace Car3C.Bootstrap
{
    /// <summary>
    /// 설정 에셋으로 DriveSimulation을 만들고 뷰·입력을 연결한 뒤 매 프레임 진행시킨다.
    /// 싱글턴 없이 직렬화 참조로 연결한다.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private TrackSettings _trackSettings;
        [SerializeField] private DriveSettings _driveSettings;
        [SerializeField] private TrafficSettings _trafficSettings;
        [SerializeField] private VehicleBehaviour _vehicle;
        [SerializeField] private DeviceLaneInput _input;
        [SerializeField] private RoadView _road;
        [SerializeField] private TrafficView _traffic;
        [SerializeField] private FollowCamera _followCamera;
        [SerializeField] private HudView _hud;
        [SerializeField] private SceneryView _scenery;
        [SerializeField] private RailTrainView _railTrain;
        [Tooltip("선택: 맵 일부에만 있는 고가 경전철 노선")]
        [SerializeField] private RailLineView _railLine;
        [Tooltip("선택: 다른 구간의 철도 노선들(각 노선의 Zone 구간에 놓인다)")]
        [SerializeField] private RailLineView[] _extraRailLines = Array.Empty<RailLineView>();
        [Tooltip("선택: 하늘을 지나가는 비행기")]
        [SerializeField] private AirplaneView _airplane;
        [Tooltip("선택: 도로 굴곡(상하·좌우) 화면 효과")]
        [SerializeField] private RoadBendView _roadBend;
        [SerializeField] private RoadBendSettings _roadBendSettings;
        [Tooltip("선택: 세계 랜드마크(각국 상징 조형)")]
        [SerializeField] private LandmarkView _landmarks;
        [SerializeField] private SceneryView _farScenery;
        [SerializeField] private EnvironmentView _environmentView;
        [SerializeField] private EnvironmentButtonsView _environmentButtons;
        [SerializeField] private WheelSpinner _playerWheels;
        [Tooltip("선택: 우주 발사장 맵 이벤트")]
        [SerializeField] private LaunchSiteController _launchSite;
        [Tooltip("선택: 항구 바다 물결 흐름")]
        [SerializeField] private SeaView _sea;
        [Tooltip("선택: 가속 발판 표시")]
        [SerializeField] private BoostPadView _boostPads;

        [Tooltip("최소 목표 프레임. 실제 목표는 이 값과 화면 주사율(90/120Hz 폰 등) 중 큰 값. 안드로이드는 지정하지 않으면 30으로 묶인다")]
        [SerializeField] private int _targetFrameRate = 60;

        private DriveSimulation _simulation;
        private EnvironmentController _environment;
        private ViewModeController _viewMode;

        public DriveSimulation Simulation => _simulation;
        public EnvironmentController Environment => _environment;
        public ViewModeController ViewMode => _viewMode;

        private void Awake()
        {
            if (_trackSettings == null || _driveSettings == null || _trafficSettings == null || _vehicle == null ||
                _input == null || _road == null || _traffic == null || _followCamera == null || _hud == null ||
                _scenery == null || _farScenery == null || _environmentView == null ||
                _environmentButtons == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)}: 씬 참조 또는 설정 에셋이 비어 있습니다.", this);
                enabled = false;
                return;
            }

            // 모바일 기본값(30FPS 제한)을 풀어 목표 프레임으로 돌린다. 모바일은 vSync를 무시하므로 0으로 둔다.
            QualitySettings.vSyncCount = 0;
            int refresh = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            Application.targetFrameRate = Mathf.Max(_targetFrameRate, refresh);

            LaneLayout layout = _trackSettings.CreateLayout();
            _simulation = new DriveSimulation(layout, _trackSettings.InitialLane, _driveSettings.Config,
                _trafficSettings.Config, System.Environment.TickCount);

            _vehicle.Initialize(_simulation);
            _road.Initialize(layout, _simulation.Speed);
            _traffic.Initialize(_simulation.Traffic);
            _viewMode = new ViewModeController();
            _followCamera.Initialize(_vehicle.transform, _simulation.Speed, _viewMode);
            _hud.Initialize(_simulation);
            _scenery.Initialize(_simulation.Speed);
            if (_railTrain != null) _railTrain.Initialize(_simulation.Speed);
            _farScenery.Initialize(_simulation.Speed);
            if (_sea != null) _sea.Initialize(_simulation.Speed);
            if (_boostPads != null) _boostPads.Initialize(_simulation.BoostPads, _simulation.Speed);

            _environment = new EnvironmentController();
            var nightLights = new List<NightLight>(_scenery.NightLights);
            nightLights.AddRange(_traffic.NightLights);
            _environmentView.Initialize(_environment, _simulation.Speed, _scenery.Tunnels, nightLights, _road.transform);
            _environmentButtons.Initialize(_environment);

            if (_playerWheels != null) _vehicle.WheelsRolled += _playerWheels.Spin;
            InitializeRailLine(_railLine);
            for (int i = 0; i < _extraRailLines.Length; ++i) InitializeRailLine(_extraRailLines[i]);
            if (_airplane != null) _airplane.Initialize(new SkyTrafficSchedule(System.Environment.TickCount));
            if (_landmarks != null) _landmarks.Initialize(_simulation.Speed);
            if (_roadBend != null && _roadBendSettings != null)
                _roadBend.Initialize(_roadBendSettings.CreateVertical(), _roadBendSettings.CreateLateral(), _simulation.Speed);

            if (_launchSite != null)
            {
                MapZoneSchedule zones = _scenery.Zones;
                double launchCenter = zones != null ? zones.GetZoneCenterDistance(MapZone.Launch) : -1d;
                if (launchCenter >= 0d) _launchSite.Initialize(_simulation.Speed, launchCenter, zones.CycleDistance);
                else _launchSite.Initialize(_simulation.Speed);
            }
        }

        private void InitializeRailLine(RailLineView line)
        {
            if (line == null || _scenery.Zones == null) return;
            var (chunk, _) = _scenery.Zones.FindZone(line.Zone);
            if (chunk >= 0) line.Initialize(_simulation.Speed, chunk * (double)_scenery.Zones.ChunkLength, _scenery.Zones.CycleDistance);
        }

        private void OnDestroy()
        {
            if (_playerWheels != null && _vehicle != null) _vehicle.WheelsRolled -= _playerWheels.Spin;
        }

        private void Update()
        {
            while (_input.TryConsumeLaneDelta(out int delta))
            {
                _simulation.RequestLaneChange(delta);
            }

            if (_input.TryConsumeJump()) _simulation.RequestJump();
            if (_input.TryConsumeViewToggle()) _viewMode.Toggle();

            _simulation.Tick(Time.deltaTime);
        }
    }
}
