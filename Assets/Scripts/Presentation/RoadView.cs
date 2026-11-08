using System;
using Car3C.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;

namespace Car3C.Presentation
{
    /// <summary>
    /// 설정된 차선 수만큼의 도로 구간을 미리 만들어 두고, 주행 거리에 맞춰 플레이어 쪽으로 흘려보내며 재사용한다.
    /// </summary>
    public sealed class RoadView : MonoBehaviour
    {
        private const float SurfaceThickness = 0.1f;
        private const float LineWidth = 0.15f;
        private const float LineHeight = 0.01f;

        [SerializeField] private Material _asphaltMaterial;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Material _shoulderMaterial;
        [SerializeField, FormerlySerializedAs("_postMaterial")] private Material _guardrailMaterial;
        [SerializeField] private float _segmentLength = 20f;
        [SerializeField] private int _segmentCount = 16;
        [SerializeField] private int _segmentsBehind = 2;
        [SerializeField] private int _dashesPerSegment = 4;
        [SerializeField] private float _shoulderWidth = 3f;
        [SerializeField] private float _guardrailOffset = 0.5f;
        [SerializeField] private int _guardrailPostsPerSegment = 5;
        [Tooltip("움직이는 도로 구간 너머(앞) 고정 도로가 이어지는 끝 z. 카메라 원거리 클립(420m)보다 조금 길게")]
        [SerializeField] private float _farRoadEnd = 430f;
        [Tooltip("움직이는 도로 구간 뒤쪽 고정 도로의 끝 z(음수). 0이면 만들지 않는다(게임 카메라는 뒤를 보지 않음)")]
        [SerializeField] private float _farRoadBehind = 0f;

        private SpeedController _speed;
        private RoadScroller _scroller;
        private Transform[] _segments;

        public void Initialize(LaneLayout layout, SpeedController speed)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            _speed = speed ?? throw new ArgumentNullException(nameof(speed));
            _scroller = new RoadScroller(_segmentLength, _segmentCount, _segmentsBehind);
            _segments = new Transform[_segmentCount];

            for (int slot = 0; slot < _segmentCount; ++slot)
            {
                _segments[slot] = BuildSegment(layout, slot);
                SliceLongBlocks(_segments[slot], 2f);
            }

            // 움직이는 구간은 앞으로 약 (구간 수 - 뒤 구간 수) × 길이까지만 있다. 그 너머가 비어 보이지 않도록
            // 고정된 단순 도로(노면·갓길·가장자리 선·가드레일 빔)를 앞뒤로 잇는다. 움직이는 구간과 겹치는 부분은 살짝 낮춰 가려지게 한다.
            float aheadStart = (_segmentCount - _segmentsBehind - 1) * _segmentLength;
            if (_farRoadEnd > aheadStart) BuildFarStrip(layout, "FarRoadAhead", aheadStart, _farRoadEnd);
            float behindEnd = -(_segmentsBehind - 1) * _segmentLength;
            if (_farRoadBehind < behindEnd) BuildFarStrip(layout, "FarRoadBehind", _farRoadBehind, behindEnd);

            Scroll();
        }

        private void LateUpdate()
        {
            if (_scroller == null) return;
            Scroll();
        }

        /// <summary>차량 대신 도로가 다가온다: 주행 거리만큼 모든 구간을 뒤로 민다.</summary>
        private void Scroll()
        {
            double distance = _speed.Distance;
            long firstIndex = _scroller.GetFirstSegmentIndex(distance);

            for (long index = firstIndex; index < firstIndex + _segmentCount; ++index)
            {
                _segments[_scroller.GetSlot(index)].localPosition =
                    new Vector3(0f, 0f, _scroller.GetSegmentLocalZ(index, distance));
            }
        }

        private Transform BuildSegment(LaneLayout layout, int slot)
        {
            Transform root = new GameObject($"RoadSegment_{slot}").transform;
            root.SetParent(transform, false);

            float halfLength = _segmentLength * 0.5f;
            float roadWidth = layout.RoadWidth;

            CreateBlock(root, "Asphalt", _asphaltMaterial,
                new Vector3(0f, -SurfaceThickness * 0.5f, halfLength),
                new Vector3(roadWidth, SurfaceThickness, _segmentLength));

            float shoulderOffset = (roadWidth + _shoulderWidth) * 0.5f;
            CreateBlock(root, "ShoulderLeft", _shoulderMaterial,
                new Vector3(-shoulderOffset, -SurfaceThickness * 0.5f - 0.02f, halfLength),
                new Vector3(_shoulderWidth, SurfaceThickness, _segmentLength));
            CreateBlock(root, "ShoulderRight", _shoulderMaterial,
                new Vector3(shoulderOffset, -SurfaceThickness * 0.5f - 0.02f, halfLength),
                new Vector3(_shoulderWidth, SurfaceThickness, _segmentLength));

            CreateBlock(root, "EdgeLineLeft", _lineMaterial,
                new Vector3(layout.GetBoundary(0) + LineWidth, LineHeight * 0.5f, halfLength),
                new Vector3(LineWidth, LineHeight, _segmentLength));
            CreateBlock(root, "EdgeLineRight", _lineMaterial,
                new Vector3(layout.GetBoundary(layout.LaneCount) - LineWidth, LineHeight * 0.5f, halfLength),
                new Vector3(LineWidth, LineHeight, _segmentLength));

            float dashSpacing = _segmentLength / _dashesPerSegment;
            float dashLength = dashSpacing * 0.5f;
            for (int boundary = 1; boundary < layout.LaneCount; ++boundary)
            {
                float x = layout.GetBoundary(boundary);
                for (int dash = 0; dash < _dashesPerSegment; ++dash)
                {
                    CreateBlock(root, "LaneDash", _lineMaterial,
                        new Vector3(x, LineHeight * 0.5f, dash * dashSpacing + dashLength * 0.5f),
                        new Vector3(LineWidth, LineHeight, dashLength));
                }
            }

            float railX = roadWidth * 0.5f + _guardrailOffset;
            BuildGuardrail(root, -railX, halfLength);
            BuildGuardrail(root, railX, halfLength);

            return root;
        }

        /// <summary>
        /// 길이 방향(z)으로 여러 칸 나눈 단위 상자. 도로 굴곡은 정점에서 휘므로, 긴 고정 도로는 정점이 촘촘해야 함께 휜다.
        /// </summary>
        private static Mesh BuildSlicedCube(int slices)
        {
            var vertices = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var triangles = new System.Collections.Generic.List<int>();
            // 위·아래·왼쪽·오른쪽 면은 z로 나누고, 앞뒤 끝면은 한 장.
            void Strip(Vector3 normal, System.Func<float, float, Vector3> point)
            {
                int start = vertices.Count;
                for (int i = 0; i <= slices; ++i)
                {
                    float t = i / (float)slices;
                    vertices.Add(point(-0.5f, t - 0.5f)); vertices.Add(point(0.5f, t - 0.5f));
                    normals.Add(normal); normals.Add(normal);
                    uvs.Add(new Vector2(0f, t)); uvs.Add(new Vector2(1f, t));
                }
                for (int i = 0; i < slices; ++i)
                {
                    int a = start + i * 2;
                    Vector3 n = Vector3.Cross(vertices[a + 2] - vertices[a], vertices[a + 1] - vertices[a]);
                    if (Vector3.Dot(n, normal) >= 0f) triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                    else triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
            Strip(Vector3.up, (u, z) => new Vector3(u, 0.5f, z));
            Strip(Vector3.down, (u, z) => new Vector3(u, -0.5f, z));
            Strip(Vector3.left, (u, z) => new Vector3(-0.5f, u, z));
            Strip(Vector3.right, (u, z) => new Vector3(0.5f, u, z));
            var mesh = new Mesh { name = $"SlicedCube_{slices}" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void BuildFarStrip(LaneLayout layout, string name, float startZ, float endZ)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0f, -0.012f, 0f);

            float length = endZ - startZ;
            float centerZ = (startZ + endZ) * 0.5f;
            float roadWidth = layout.RoadWidth;
            CreateBlock(root, "Asphalt", _asphaltMaterial, new Vector3(0f, -SurfaceThickness * 0.5f, centerZ), new Vector3(roadWidth, SurfaceThickness, length));
            float shoulderOffset = (roadWidth + _shoulderWidth) * 0.5f;
            CreateBlock(root, "ShoulderLeft", _shoulderMaterial, new Vector3(-shoulderOffset, -SurfaceThickness * 0.5f - 0.02f, centerZ), new Vector3(_shoulderWidth, SurfaceThickness, length));
            CreateBlock(root, "ShoulderRight", _shoulderMaterial, new Vector3(shoulderOffset, -SurfaceThickness * 0.5f - 0.02f, centerZ), new Vector3(_shoulderWidth, SurfaceThickness, length));
            CreateBlock(root, "EdgeLineLeft", _lineMaterial, new Vector3(layout.GetBoundary(0) + LineWidth, LineHeight * 0.5f, centerZ), new Vector3(LineWidth, LineHeight, length));
            CreateBlock(root, "EdgeLineRight", _lineMaterial, new Vector3(layout.GetBoundary(layout.LaneCount) - LineWidth, LineHeight * 0.5f, centerZ), new Vector3(LineWidth, LineHeight, length));
            float railX = roadWidth * 0.5f + _guardrailOffset;
            CreateBlock(root, "GuardrailBeam", _guardrailMaterial, new Vector3(-railX, 0.72f, centerZ), new Vector3(0.08f, 0.3f, length));
            CreateBlock(root, "GuardrailBeam", _guardrailMaterial, new Vector3(railX, 0.72f, centerZ), new Vector3(0.08f, 0.3f, length));

            SliceLongBlocks(root, 4f);
        }

        private readonly System.Collections.Generic.Dictionary<int, Mesh> _slicedMeshes = new System.Collections.Generic.Dictionary<int, Mesh>();

        /// <summary>
        /// 도로 굴곡은 정점에서 휘므로, 길이 방향으로 긴 상자는 step(m)마다 나눈 메시로 바꾼다.
        /// 나누지 않으면 오목한 곳에서 짧은 차선 점선이 긴 노면 아래로 묻힌다.
        /// </summary>
        private void SliceLongBlocks(Transform root, float step)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                float length = filter.transform.localScale.z;
                if (length < step * 1.5f) continue;
                int slices = Mathf.CeilToInt(length / step);
                if (!_slicedMeshes.TryGetValue(slices, out Mesh mesh))
                {
                    mesh = BuildSlicedCube(slices);
                    _slicedMeshes.Add(slices, mesh);
                }
                filter.sharedMesh = mesh;
            }
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in _slicedMeshes.Values) Destroy(mesh);
        }

        /// <summary>가드레일: 일정 간격의 기둥 위에 철제 빔 두 줄. 구간 길이만큼 이어져 끊김 없이 보인다.</summary>
        private void BuildGuardrail(Transform parent, float x, float halfLength)
        {
            CreateBlock(parent, "GuardrailBeam", _guardrailMaterial, new Vector3(x, 0.72f, halfLength), new Vector3(0.08f, 0.3f, _segmentLength));
            CreateBlock(parent, "GuardrailBeamLow", _guardrailMaterial, new Vector3(x, 0.42f, halfLength), new Vector3(0.06f, 0.08f, _segmentLength));

            float spacing = _segmentLength / _guardrailPostsPerSegment;
            float postX = x + Mathf.Sign(x) * 0.12f;
            for (int i = 0; i < _guardrailPostsPerSegment; ++i)
            {
                CreateBlock(parent, "GuardrailPost", _guardrailMaterial, new Vector3(postX, 0.45f, i * spacing), new Vector3(0.12f, 0.9f, 0.12f));
            }
        }

        private static void CreateBlock(Transform parent, string name, Material material, Vector3 localPosition, Vector3 localScale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            Destroy(block.GetComponent<Collider>());
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
