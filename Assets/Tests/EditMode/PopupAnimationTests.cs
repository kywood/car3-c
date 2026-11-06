using Car3C.Presentation;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class PopupAnimationTests
    {
        [Test]
        public void HiddenUntilPlayed()
        {
            PopupAnimation popup = new PopupAnimation(1f, 1.5f, 0.2f, 0.4f);
            Assert.IsFalse(popup.IsVisible);
            Assert.AreEqual(0f, popup.Alpha, 1e-5f);
        }

        [Test]
        public void Play_StartsLargeAndOpaque_ThenSettles()
        {
            PopupAnimation popup = new PopupAnimation(1f, 1.5f, 0.2f, 0.4f);
            popup.Play();
            Assert.IsTrue(popup.IsVisible);
            Assert.AreEqual(1.5f, popup.Scale, 1e-5f);
            Assert.AreEqual(1f, popup.Alpha, 1e-5f);

            popup.Tick(0.3f);
            Assert.AreEqual(1f, popup.Scale, 1e-5f);
            Assert.AreEqual(1f, popup.Alpha, 1e-5f);
        }

        [Test]
        public void FadesOutAtEnd()
        {
            PopupAnimation popup = new PopupAnimation(1f, 1.5f, 0.2f, 0.4f);
            popup.Play();
            popup.Tick(0.8f);
            Assert.AreEqual(0.5f, popup.Alpha, 1e-4f);
            popup.Tick(0.5f);
            Assert.IsFalse(popup.IsVisible);
            Assert.AreEqual(0f, popup.Alpha, 1e-5f);
        }

        [Test]
        public void Replay_RestartsAnimation()
        {
            PopupAnimation popup = new PopupAnimation(1f, 1.5f, 0.2f, 0.4f);
            popup.Play();
            popup.Tick(0.9f);
            popup.Play();
            Assert.AreEqual(1f, popup.Alpha, 1e-5f);
            Assert.AreEqual(1.5f, popup.Scale, 1e-5f);
        }
    }
}
