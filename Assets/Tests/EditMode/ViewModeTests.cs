using Car3C.Gameplay;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class ViewModeTests
    {
        [Test]
        public void StartsInChase_ToggleGoesCockpitAndBack()
        {
            var view = new ViewModeController();
            Assert.AreEqual(ViewMode.Chase, view.Mode);
            view.Toggle();
            Assert.AreEqual(ViewMode.Cockpit, view.Mode);
            view.Toggle();
            Assert.AreEqual(ViewMode.Chase, view.Mode);
        }

        [Test]
        public void Changed_FiresOnlyOnRealChange()
        {
            var view = new ViewModeController();
            int changes = 0;
            view.Changed += () => ++changes;
            view.Set(ViewMode.Chase);
            view.Set(ViewMode.Cockpit);
            view.Set(ViewMode.Cockpit);
            view.Toggle();
            Assert.AreEqual(2, changes);
        }
    }
}
