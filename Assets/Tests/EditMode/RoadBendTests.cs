using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class RoadBendTests
    {
        private static RoadBendProfile Make(float strength = 1f) =>
            new RoadBendProfile(new[] { 3f, 5f, 8f }, new[] { 260f, 470f, 830f }, strength, 7);

        [Test]
        public void NearTheCar_RoadIsFlat_AtAnyDistance()
        {
            var p = Make();
            foreach (double d in new[] { 0d, 123.4d, 5000d, 1_000_000d })
            {
                Assert.AreEqual(0d, p.Offset(d, 0d), 1e-6);
                // 기울기도 0: 1m 앞 굴곡은 아주 작다(2차 항만 남음).
                Assert.Less(System.Math.Abs(p.Offset(d, 1d)), 0.01d);
            }
        }

        [Test]
        public void HillsAreFixedToTheRoad_AndApproachTheCar()
        {
            var p = Make();
            // 도로 높이 H(s)가 도로에 붙어 있으면: 두 지점의 높이 차는 차가 어디 있든 같다(기울기 보정 항 제외하면).
            // 200m 앞으로 간 뒤의 굴곡 모양이 달라야(정지 그림이 아니어야) 한다.
            double a = p.Offset(0d, 300d);
            double b = p.Offset(200d, 300d);
            Assert.Greater(System.Math.Abs(a - b), 1e-3);
            // 값의 크기는 진폭 합 수준을 넘지 않는다(멀리 400m 앞도 수십 m 이내).
            for (double z = 0; z <= 400; z += 20) Assert.Less(System.Math.Abs(p.Offset(777d, z)), 100d);
        }

        [Test]
        public void StrengthZero_IsStraightRoad()
        {
            var p = Make(0f);
            Assert.IsTrue(p.IsFlat);
            Assert.AreEqual(0d, p.Offset(500d, 300d), 1e-9);
        }

        [Test]
        public void PhaseStaysWrapped_AtLargeDistance()
        {
            var p = Make();
            float phase = p.Phase(0, 10_000_000d);
            Assert.That(phase, Is.InRange(0f, (float)(2 * System.Math.PI)));
        }
    }
}
