using System;
using System.Collections.Generic;
using Car3C.Gameplay;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 도로 양옆 배경(건물·가로등·항구·배·산 등)을 일정 길이 구간 단위로 미리 만들어 두고(오브젝트 풀),
    /// 도로와 같은 주행 거리로 흘려보낸다. 구간이 앞쪽으로 재사용될 때 구간 번호에 따라 소품을 다시 고른다.
    /// 터널을 쓰면 터널 구간에는 일반 소품 대신 터널(산·입구·출구)을 보여준다.
    /// </summary>
    public sealed class SceneryView : MonoBehaviour
    {
        [SerializeField] private float _chunkLength = 40f;
        [SerializeField] private int _chunkCount = 9;
        [SerializeField] private int _chunksBehind = 1;
        [SerializeField] private int _seed = 7321;
        [SerializeField] private ScenerySpot[] _spots = Array.Empty<ScenerySpot>();

        [Header("Tunnel")]
        [SerializeField] private bool _useTunnels;
        [SerializeField] private int _tunnelFirstChunk = 12;
        [SerializeField] private int _tunnelPeriod = 22;
        [SerializeField] private int _tunnelLength = 4;
        [SerializeField] private GameObject _tunnelBodyPrefab;
        [SerializeField] private GameObject _tunnelEntrancePrefab;
        [SerializeField] private GameObject _tunnelExitPrefab;

        [Header("Zones (맵 툴 도입 전 임시 구간 표)")]
        [SerializeField] private bool _useZones;
        [SerializeField] private ZoneSegment[] _zoneSegments = Array.Empty<ZoneSegment>();

        [Serializable]
        private struct ZoneSegment
        {
            [SerializeField] private MapZone _zone;
            [SerializeField, Min(1)] private int _chunks;

            public MapZone Zone => _zone;
            public int Chunks => _chunks;
        }

        private readonly List<NightLight> _nightLights = new List<NightLight>();
        private MapZoneSchedule _zones;
        private readonly List<NightLight> _nightLightBuffer = new List<NightLight>();
        private SpeedController _speed;
        private RoadScroller _scroller;
        private TunnelSchedule _tunnels;
        private Transform[] _chunks;
        private GameObject[][][] _instances;
        private int[][] _activeOption;
        private GameObject[] _tunnelBodies;
        private GameObject[] _tunnelEntrances;
        private GameObject[] _tunnelExits;
        private long[] _chunkIndexBySlot;

        /// <summary>터널을 쓰지 않으면 null.</summary>
        public TunnelSchedule Tunnels => _tunnels;
        /// <summary>구간을 쓰지 않으면 null.</summary>
        public MapZoneSchedule Zones => _zones;
        public IReadOnlyList<NightLight> NightLights => _nightLights;

        public void Initialize(SpeedController speed)
        {
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            _scroller = new RoadScroller(_chunkLength, _chunkCount, _chunksBehind);
            _zones = _useZones && _zoneSegments.Length > 0 ? CreateZoneSchedule() : null;
            if (_useTunnels)
                _tunnels = _zones != null ? _zones.CreateTunnelSchedule() : new TunnelSchedule(_tunnelFirstChunk, _tunnelPeriod, _tunnelLength, _chunkLength);
            _chunks = new Transform[_chunkCount];
            _instances = new GameObject[_chunkCount][][];
            _activeOption = new int[_chunkCount][];
            _tunnelBodies = new GameObject[_chunkCount];
            _tunnelEntrances = new GameObject[_chunkCount];
            _tunnelExits = new GameObject[_chunkCount];
            _chunkIndexBySlot = new long[_chunkCount];

            for (int slot = 0; slot < _chunkCount; ++slot)
            {
                _chunks[slot] = BuildChunk(slot);
                _chunkIndexBySlot[slot] = -1;
            }

            Scroll();
        }

        private void LateUpdate()
        {
            if (_scroller == null) return;
            Scroll();
        }

        private void Scroll()
        {
            double distance = _speed.Distance;
            long firstIndex = _scroller.GetFirstSegmentIndex(distance);

            for (long index = firstIndex; index < firstIndex + _chunkCount; ++index)
            {
                int slot = _scroller.GetSlot(index);
                if (_chunkIndexBySlot[slot] != index)
                {
                    _chunkIndexBySlot[slot] = index;
                    Populate(slot, index);
                }

                _chunks[slot].localPosition = new Vector3(0f, 0f, _scroller.GetSegmentLocalZ(index, distance));
            }
        }

        private Transform BuildChunk(int slot)
        {
            Transform chunk = new GameObject($"SceneryChunk_{slot}").transform;
            chunk.SetParent(transform, false);
            _instances[slot] = new GameObject[_spots.Length][];
            _activeOption[slot] = new int[_spots.Length];

            for (int spotIndex = 0; spotIndex < _spots.Length; ++spotIndex)
            {
                ScenerySpot spot = _spots[spotIndex];
                GameObject[] options = spot.Options;
                GameObject[] created = new GameObject[options.Length];
                Quaternion rotation = Quaternion.Euler(0f, spot.Yaw, 0f);

                for (int option = 0; option < options.Length; ++option)
                {
                    if (options[option] == null) continue;
                    created[option] = CreateInactive(options[option], chunk, spot.LocalPosition, rotation);
                }

                _instances[slot][spotIndex] = created;
                _activeOption[slot][spotIndex] = SceneryPicker.Empty;
            }

            if (_tunnels != null)
            {
                _tunnelBodies[slot] = CreateInactive(_tunnelBodyPrefab, chunk, Vector3.zero, Quaternion.identity);
                _tunnelEntrances[slot] = CreateInactive(_tunnelEntrancePrefab, chunk, Vector3.zero, Quaternion.identity);
                _tunnelExits[slot] = CreateInactive(_tunnelExitPrefab, chunk, Vector3.zero, Quaternion.identity);
            }

            return chunk;
        }

        private GameObject CreateInactive(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab, parent);
            instance.transform.SetLocalPositionAndRotation(position, rotation);
            instance.SetActive(false);
            instance.GetComponentsInChildren(true, _nightLightBuffer);
            _nightLights.AddRange(_nightLightBuffer);
            return instance;
        }

        private void Populate(int slot, long chunkIndex)
        {
            bool inTunnel = _tunnels != null && _tunnels.IsTunnel(chunkIndex);
            if (_tunnels != null)
            {
                SetActive(_tunnelBodies[slot], inTunnel);
                SetActive(_tunnelEntrances[slot], _tunnels.IsEntrance(chunkIndex));
                SetActive(_tunnelExits[slot], _tunnels.IsExit(chunkIndex));
            }

            bool hasZone = _zones != null;
            MapZone zone = hasZone ? _zones.GetZone(chunkIndex) : MapZone.City;

            for (int spotIndex = 0; spotIndex < _spots.Length; ++spotIndex)
            {
                GameObject[] created = _instances[slot][spotIndex];
                int previous = _activeOption[slot][spotIndex];
                // 구간 표가 있으면 Tunnel 마스크를 가진 지점(도심 터널 옆 건물 등)만 터널 구간에 나타난다.
                bool allowed = hasZone ? MapZoneSchedule.Contains(_spots[spotIndex].Zones, zone) : !inTunnel;
                int next = allowed
                    ? SceneryPicker.Pick(_seed, chunkIndex, spotIndex, created.Length, _spots[spotIndex].EmptyChance)
                    : SceneryPicker.Empty;
                if (previous == next) continue;

                if (previous != SceneryPicker.Empty) SetActive(created[previous], false);
                if (next != SceneryPicker.Empty) SetActive(created[next], true);
                _activeOption[slot][spotIndex] = next;
            }
        }

        private MapZoneSchedule CreateZoneSchedule()
        {
            var segments = new (MapZone, int)[_zoneSegments.Length];
            for (int i = 0; i < _zoneSegments.Length; ++i) segments[i] = (_zoneSegments[i].Zone, _zoneSegments[i].Chunks);
            return new MapZoneSchedule(segments, _chunkLength);
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive) target.SetActive(isActive);
        }
    }
}
