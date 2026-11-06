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
