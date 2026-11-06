using System;
using UnityEngine;

namespace Car3C.Gameplay
{
    /// <summary>
    /// 고가 경전철 노선(평면 x,z 꺾은선). 거리 s(0 ~ Length)로 위치와 진행 방향을 얻는다.
    /// </summary>
    public sealed class RailRoute
    {
        private readonly Vector2[] _points;
        private readonly float[] _cumulative;

        public float Length { get; }

        public RailRoute(Vector2[] points)
        {
            if (points == null || points.Length < 2) throw new ArgumentException("노선 점이 2개 이상 필요합니다.", nameof(points));
            _points = (Vector2[])points.Clone();
            _cumulative = new float[_points.Length];
            for (int i = 1; i < _points.Length; ++i) _cumulative[i] = _cumulative[i - 1] + Vector2.Distance(_points[i - 1], _points[i]);
            Length = _cumulative[_cumulative.Length - 1];
            if (Length <= 0f) throw new ArgumentException("노선 길이가 0입니다.", nameof(points));
        }

        /// <param name="direction">s가 증가하는 쪽의 단위 방향.</param>
        public Vector2 Sample(float s, out Vector2 direction)
        {
            s = Mathf.Clamp(s, 0f, Length);
            int lo = 0, hi = _cumulative.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (_cumulative[mid] <= s) lo = mid; else hi = mid;
            }
            float segment = _cumulative[hi] - _cumulative[lo];
            float t = segment > 0f ? (s - _cumulative[lo]) / segment : 0f;
            direction = (_points[hi] - _points[lo]).normalized;
            return Vector2.Lerp(_points[lo], _points[hi], t);
        }

        /// <summary>2차 베지어 곡선들을 이어 촘촘한 점 목록을 만든다(각 곡선은 시작·조절·끝 점).</summary>
        public static Vector2[] FromQuadraticCurves(Vector2[] controlTriplets, int samplesPerCurve)
        {
            if (controlTriplets.Length % 3 != 0) throw new ArgumentException("점은 3개씩 묶여야 합니다.");
            int curves = controlTriplets.Length / 3;
            var result = new System.Collections.Generic.List<Vector2>();
            for (int c = 0; c < curves; ++c)
            {
                Vector2 a = controlTriplets[c * 3], b = controlTriplets[c * 3 + 1], d = controlTriplets[c * 3 + 2];
                for (int i = c == 0 ? 0 : 1; i <= samplesPerCurve; ++i)
                {
                    float t = i / (float)samplesPerCurve;
                    float u = 1f - t;
                    result.Add(u * u * a + 2f * u * t * b + t * t * d);
                }
            }
            return result.ToArray();
        }
    }
}
