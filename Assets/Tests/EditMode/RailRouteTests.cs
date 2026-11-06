using Car3C.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Car3C.Tests.EditMode
{
    public sealed class RailRouteTests
    {
        [Test]
        public void Sample_InterpolatesAlongPolyline()
        {
            var route = new RailRoute(new[] { new Vector2(0f, 0f), new Vector2(0f, 10f), new Vector2(10f, 10f) });
            Assert.AreEqual(20f, route.Length, 1e-4f);
            Vector2 p = route.Sample(5f, out Vector2 dir);
            Assert.AreEqual(new Vector2(0f, 5f), p);
            Assert.AreEqual(new Vector2(0f, 1f), dir);
            p = route.Sample(15f, out dir);
            Assert.AreEqual(5f, p.x, 1e-4f);
            Assert.AreEqual(new Vector2(1f, 0f), dir);
        }

        [Test]
        public void Curves_StartAndEndAtControlPoints()
        {
            Vector2[] points = RailRoute.FromQuadraticCurves(new[]
            {
                new Vector2(100f, -60f), new Vector2(16f, -10f), new Vector2(16f, 40f),
                new Vector2(16f, 40f), new Vector2(16f, 120f), new Vector2(16f, 200f),
            }, 8);
            Assert.AreEqual(new Vector2(100f, -60f), points[0]);
            Assert.AreEqual(new Vector2(16f, 200f), points[points.Length - 1]);
            Assert.AreEqual(17, points.Length);
        }
    }
}
