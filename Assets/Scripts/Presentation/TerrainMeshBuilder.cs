using System;
using System.Collections.Generic;
using UnityEngine;

namespace Car3C.Presentation
{
    /// <summary>
    /// 로우폴리(면마다 평면 셰이딩) 산 메시 생성기.
    /// Ridge: 도로 위를 덮는 산등성이 한 구간(터널 구멍 포함). 구간 앞뒤 높이가 같아 이어 붙여도 틈이 없다.
    /// RidgeCap: 터널 입구/출구의 산 단면(가운데 터널 구멍).
    /// Peak: 먼 배경용 산봉우리.
    /// </summary>
    public static class TerrainMeshBuilder
    {
        public readonly struct RidgeShape
        {
            public readonly float Length;
            public readonly float HalfWidth;
            public readonly float PeakHeight;
            public readonly float TunnelHalfWidth;
            public readonly float TunnelHeight;
            public readonly int ProfileSteps;
            public readonly int LengthSteps;
            public readonly int Seed;

            public RidgeShape(float length, float halfWidth, float peakHeight, float tunnelHalfWidth, float tunnelHeight,
                int profileSteps, int lengthSteps, int seed)
            {
                if (tunnelHalfWidth >= halfWidth) throw new ArgumentException("터널 폭이 산 폭보다 넓습니다.");
                if (profileSteps < 4 || lengthSteps < 1) throw new ArgumentException("분할 수가 너무 적습니다.");
                Length = length;
                HalfWidth = halfWidth;
                PeakHeight = peakHeight;
                TunnelHalfWidth = tunnelHalfWidth;
                TunnelHeight = tunnelHeight;
                ProfileSteps = profileSteps;
                LengthSteps = lengthSteps;
                Seed = seed;
            }
        }

        public static Mesh BuildRidge(RidgeShape shape)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float dz = shape.Length / shape.LengthSteps;

            for (int j = 0; j < shape.LengthSteps; ++j)
            {
                for (int i = 0; i < shape.ProfileSteps; ++i)
                {
                    Vector3 a = OuterPoint(shape, i, j, j * dz);
                    Vector3 b = OuterPoint(shape, i + 1, j, j * dz);
                    Vector3 c = OuterPoint(shape, i + 1, j + 1, (j + 1) * dz);
                    Vector3 d = OuterPoint(shape, i, j + 1, (j + 1) * dz);
                    Vector3 outward = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f + 5f, 0f);
                    AddFacingTriangle(vertices, triangles, a, b, c, outward);
                    AddFacingTriangle(vertices, triangles, a, c, d, outward);
                }
            }

            return CreateMesh("Ridge", vertices, triangles);
        }

        /// <param name="facingBack">false면 -z(플레이어 쪽, 입구)를, true면 +z(출구 뒤쪽)를 바라본다.</param>
        public static Mesh BuildRidgeCap(RidgeShape shape, bool facingBack)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            int row = facingBack ? shape.LengthSteps : 0;
            float z = facingBack ? shape.Length : 0f;
            Vector3 facing = facingBack ? Vector3.forward : Vector3.back;

            for (int i = 0; i < shape.ProfileSteps; ++i)
            {
                Vector3 o0 = OuterPoint(shape, i, row, z);
                Vector3 o1 = OuterPoint(shape, i + 1, row, z);
                Vector3 n0 = InnerPoint(shape, i / (float)shape.ProfileSteps, z);
                Vector3 n1 = InnerPoint(shape, (i + 1) / (float)shape.ProfileSteps, z);
                AddFacingTriangle(vertices, triangles, o0, o1, n1, facing, true);
                AddFacingTriangle(vertices, triangles, o0, n1, n0, facing, true);
            }

            return CreateMesh(facingBack ? "RidgeCapBack" : "RidgeCapFront", vertices, triangles);
        }

        public static Mesh BuildPeak(float radius, float height, int segments, int rings, int seed)
        {
            if (segments < 3 || rings < 1) throw new ArgumentException("분할 수가 너무 적습니다.");
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector3[,] grid = new Vector3[rings + 1, segments];

            for (int r = 0; r <= rings; ++r)
            {
                float t = r / (float)rings;
                float ringRadius = radius * (1f - t);
                float y = height * (1f - (1f - t) * (1f - t));
                for (int s = 0; s < segments; ++s)
                {
                    float angle = s / (float)segments * Mathf.PI * 2f;
                    float jitter = r == 0 || r == rings ? 0f : (Noise(seed, r, s) - 0.5f) * 0.35f;
                    float rr = ringRadius * (1f + jitter);
                    float yy = r == 0 ? 0f : y * (1f + (Noise(seed + 17, r, s) - 0.5f) * 0.2f);
                    grid[r, s] = new Vector3(Mathf.Cos(angle) * rr, yy, Mathf.Sin(angle) * rr);
                }
            }

            Vector3 apex = new Vector3(0f, height * 1.05f, 0f);
            for (int r = 0; r < rings; ++r)
            {
                for (int s = 0; s < segments; ++s)
                {
                    int next = (s + 1) % segments;
                    Vector3 a = grid[r, s];
                    Vector3 b = grid[r, next];
                    Vector3 c = r + 1 == rings ? apex : grid[r + 1, next];
                    Vector3 d = r + 1 == rings ? apex : grid[r + 1, s];
                    Vector3 outward = new Vector3((a.x + b.x) * 0.5f, 0f, (a.z + b.z) * 0.5f);
                    AddFacingTriangle(vertices, triangles, a, b, c, outward);
                    if (r + 1 < rings) AddFacingTriangle(vertices, triangles, a, c, d, outward);
                }
            }

            return CreateMesh("Peak", vertices, triangles);
        }

        /// <summary>산등성이 단면 바깥선 위의 점. i: 좌→우 분할 번호, j: 길이 방향 분할 번호(앞뒤 끝은 같은 모양).</summary>
        private static Vector3 OuterPoint(RidgeShape shape, int i, int j, float z)
        {
            float t = i / (float)shape.ProfileSteps;
            float x = Mathf.Lerp(-shape.HalfWidth, shape.HalfWidth, t);
            if (i == 0 || i == shape.ProfileSteps) return new Vector3(x, 0f, z);

            float ridge = 1f - Mathf.Pow(Mathf.Abs(2f * t - 1f), 1.6f);
            int wrappedJ = j % shape.LengthSteps;
            float noise = (Noise(shape.Seed, i, wrappedJ) - 0.5f) * 0.3f;
            float y = shape.PeakHeight * ridge * (1f + noise);

            float clearance = shape.TunnelHeight + 4f;
            if (Mathf.Abs(x) < shape.TunnelHalfWidth + 6f) y = Mathf.Max(y, clearance);
            return new Vector3(x, y, z);
        }

        /// <summary>터널 구멍 둘레(왼쪽 아래 → 왼쪽 위 → 오른쪽 위 → 오른쪽 아래)를 0~1로 따라가는 점.</summary>
        private static Vector3 InnerPoint(RidgeShape shape, float s, float z)
        {
            float w = shape.TunnelHalfWidth;
            float h = shape.TunnelHeight;
            float total = h + 2f * w + h;
            float d = s * total;
            if (d <= h) return new Vector3(-w, d, z);
            d -= h;
            if (d <= 2f * w) return new Vector3(-w + d, h, z);
            d -= 2f * w;
            return new Vector3(w, h - d, z);
        }

        private static void AddFacingTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c,
            Vector3 desiredNormal, bool skipDegenerate = true)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (skipDegenerate && normal.sqrMagnitude < 1e-6f) return;
            if (Vector3.Dot(normal, desiredNormal) < 0f)
            {
                Vector3 swap = b;
                b = c;
                c = swap;
            }

            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        private static Mesh CreateMesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, BoxProjectUvs(vertices));
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 삼각형마다 가장 가까운 축 평면으로 투영한 월드 크기 UV(UvTileSize m당 1회 반복).
        /// 정점이 삼각형마다 따로 있어 면 단위로 투영할 수 있다.
        /// </summary>
        private static List<Vector2> BoxProjectUvs(List<Vector3> vertices)
        {
            const float UvTileSize = 12f;
            var uvs = new List<Vector2>(vertices.Count);
            for (int i = 0; i + 2 < vertices.Count; i += 3)
            {
                Vector3 a = vertices[i];
                Vector3 b = vertices[i + 1];
                Vector3 c = vertices[i + 2];
                Vector3 n = Vector3.Cross(b - a, c - a);
                float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
                for (int k = 0; k < 3; ++k)
                {
                    Vector3 v = vertices[i + k];
                    Vector2 uv = ay >= ax && ay >= az ? new Vector2(v.x, v.z) : ax >= az ? new Vector2(v.z, v.y) : new Vector2(v.x, v.y);
                    uvs.Add(uv / UvTileSize);
                }
            }
            return uvs;
        }

        /// <summary>
        /// 부드러운 산봉우리: 정점을 공유하는 고리 격자라 매끈하게 음영지고, 둘레를 따라 굵은·잔 능선이 뻗는다.
        /// 오목한 경사(아래가 넓게 퍼짐)로 실제 산 모양에 가깝게 만든다.
        /// </summary>
        public static Mesh BuildSmoothPeak(float radius, float height, int segments, int rings, int seed)
        {
            if (segments < 8 || rings < 4) throw new ArgumentException("분할 수가 너무 적습니다.");
            var vertices = new List<Vector3>((rings + 1) * segments + 1);
            var triangles = new List<int>(rings * segments * 6);

            // 바닥이 약간 타원(씨앗별 비율·방향)이라 원뿔처럼 보이지 않는다.
            float stretch = 1.15f + 0.35f * Noise(seed, 91, 0);
            float stretchAngle = Noise(seed, 92, 0) * Mathf.PI;
            float topY = 0f;
            for (int r = 0; r <= rings; ++r)
            {
                float t = r / (float)rings;            // 0 = 바닥 둘레, 1 = 꼭대기
                for (int s = 0; s < segments; ++s)
                {
                    float a = s / (float)segments;
                    // 능선: 둘레 방향으로 반복되는 굵은 굴곡(4칸) + 잔 굴곡(11칸).
                    float spur = PeriodicNoise(seed, a * 4f, 4) * 0.6f + PeriodicNoise(seed + 31, a * 11f, 11) * 0.4f;
                    float rough = PeriodicNoise(seed + 7, a * 23f, 23);
                    float d = 1f - t;                  // 꼭대기에서의 거리 비율
                    float angle = a * Mathf.PI * 2f;
                    float ellipse = 1f + (stretch - 1f) * Mathf.Abs(Mathf.Cos(angle - stretchAngle));
                    float rr = radius * d * (0.7f + 0.6f * spur) * ellipse;
                    // 꼭대기는 둥글고(1 - d²), 아래는 넓게 퍼지는(^1.6) 단면.
                    float profile = Mathf.Pow(1f - d * d, 1.6f);
                    float y = r == 0 ? 0f : height * profile * (0.85f + 0.3f * spur * d + 0.15f) + (rough - 0.5f) * height * 0.05f * d;
                    y = Mathf.Max(0f, y);
                    if (r == rings) topY += y / segments;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * rr, y, Mathf.Sin(angle) * rr));
                }
            }
            int apex = vertices.Count;
            vertices.Add(new Vector3(0f, topY + height * 0.01f, 0f));

            for (int r = 0; r < rings; ++r)
            {
                for (int s = 0; s < segments; ++s)
                {
                    int next = (s + 1) % segments;
                    int a0 = r * segments + s, a1 = r * segments + next;
                    if (r + 1 == rings)
                    {
                        triangles.Add(a0); triangles.Add(apex); triangles.Add(a1);
                        continue;
                    }
                    int b0 = (r + 1) * segments + s, b1 = (r + 1) * segments + next;
                    triangles.Add(a0); triangles.Add(b0); triangles.Add(a1);
                    triangles.Add(a1); triangles.Add(b0); triangles.Add(b1);
                }
            }

            Mesh mesh = new Mesh { name = "SmoothPeak" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            // 면이 바깥(위)을 보게: 중간 고리 정점 법선이 바깥쪽이 아니면 감김 방향을 뒤집는다.
            int probe = (rings / 2) * segments;
            Vector3 outward = new Vector3(vertices[probe].x, 0f, vertices[probe].z);
            if (Vector3.Dot(mesh.normals[probe], outward) < 0f)
            {
                for (int i = 0; i < triangles.Count; i += 3) (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
            }
            // UV: 정점 법선이 가리키는 축 평면으로 투영(UvTileSize 12m 반복과 같은 크기).
            var normals = mesh.normals;
            var uvs = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; ++i)
            {
                Vector3 n = normals[i], p = vertices[i];
                float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
                Vector2 uv = ay >= ax && ay >= az ? new Vector2(p.x, p.z) : ax >= az ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
                uvs[i] = uv / 12f;
            }
            mesh.uv = uvs;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 삼각형마다 정점이 따로 있는 메시의 법선을 부드럽게 한다. 같은 위치의 정점끼리, 면 각도가 maxAngle 이하인 면만 평균하여
        /// 완만한 경사는 매끈하게, 터널 입구 같은 날카로운 모서리는 그대로 둔다.
        /// </summary>
        public static void SmoothNormals(Mesh mesh, float maxAngle)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            var faceNormals = new Vector3[triangles.Length / 3];
            for (int f = 0; f < faceNormals.Length; ++f)
            {
                Vector3 a = vertices[triangles[f * 3]], b = vertices[triangles[f * 3 + 1]], c = vertices[triangles[f * 3 + 2]];
                faceNormals[f] = Vector3.Cross(b - a, c - a).normalized;
            }

            var vertexFace = new int[vertices.Length];
            for (int i = 0; i < triangles.Length; ++i) vertexFace[triangles[i]] = i / 3;

            var groups = new Dictionary<Vector3Int, List<int>>();
            for (int i = 0; i < vertices.Length; ++i)
            {
                Vector3Int key = Vector3Int.RoundToInt(vertices[i] * 100f);
                if (!groups.TryGetValue(key, out var list)) groups.Add(key, list = new List<int>(6));
                list.Add(i);
            }

            float minDot = Mathf.Cos(maxAngle * Mathf.Deg2Rad);
            var normals = new Vector3[vertices.Length];
            foreach (var list in groups.Values)
            {
                foreach (int i in list)
                {
                    Vector3 own = faceNormals[vertexFace[i]];
                    Vector3 sum = Vector3.zero;
                    foreach (int j in list)
                    {
                        Vector3 other = faceNormals[vertexFace[j]];
                        if (Vector3.Dot(own, other) >= minDot) sum += other;
                    }
                    normals[i] = sum.sqrMagnitude > 1e-8f ? sum.normalized : own;
                }
            }
            mesh.normals = normals;
            mesh.RecalculateTangents();
        }

        /// <summary>period 칸마다 반복되는 1차원 부드러운 노이즈(0~1).</summary>
        private static float PeriodicNoise(int seed, float x, int period)
        {
            int i0 = Mathf.FloorToInt(x);
            float f = x - i0;
            float u = f * f * (3f - 2f * f);
            int a = ((i0 % period) + period) % period;
            int b = (a + 1) % period;
            return Mathf.Lerp(Noise(seed, a, 0), Noise(seed, b, 0), u);
        }

        private static float Noise(int seed, int x, int y)
        {
            unchecked
            {
                uint h = (uint)seed * 374761393u + (uint)x * 668265263u + (uint)y * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
