using System.Collections.Generic;
using Car3C.Gameplay.Launch;
using NUnit.Framework;

namespace Car3C.Tests.EditMode
{
    public sealed class LaunchTests
    {
        private sealed class Recorder : ILaunchSequenceListener
        {
            public readonly List<LaunchPhase> Phases = new List<LaunchPhase>();
            public readonly List<int> Ticks = new List<int>();
            public void OnLaunchPhaseChanged(LaunchPhase phase) => Phases.Add(phase);
            public void OnCountdownTick(int secondsLeft) => Ticks.Add(secondsLeft);
        }

        private static readonly LaunchTimeline Timeline = new LaunchTimeline(10f, 3f, 20f, 2f, 5f);

        [Test]
        public void Evaluate_FollowsPhasesInOrder()
        {
            Assert.AreEqual(LaunchPhase.Countdown, RocketLaunchSequence.Evaluate(Timeline, 0.5).Phase);
            Assert.AreEqual(10, RocketLaunchSequence.Evaluate(Timeline, 0.5).CountdownSeconds);
            Assert.AreEqual(1, RocketLaunchSequence.Evaluate(Timeline, 9.5).CountdownSeconds);
            Assert.AreEqual(LaunchPhase.Ignition, RocketLaunchSequence.Evaluate(Timeline, 11.0).Phase);
            Assert.AreEqual(LaunchPhase.Ascent, RocketLaunchSequence.Evaluate(Timeline, 14.0).Phase);
            Assert.AreEqual(LaunchPhase.Complete, RocketLaunchSequence.Evaluate(Timeline, 40.0).Phase);
        }

        [Test]
        public void Evaluate_AltitudeRisesWithAcceleration_AndOnlyAfterIgnition()
        {
            Assert.AreEqual(0f, RocketLaunchSequence.Evaluate(Timeline, 12.0).Altitude, 1e-4f);
            float a = RocketLaunchSequence.Evaluate(Timeline, 15.0).Altitude;
            float b = RocketLaunchSequence.Evaluate(Timeline, 18.0).Altitude;
            Assert.AreEqual(4f, a, 1e-3f);
            Assert.AreEqual(25f, b, 1e-3f);
        }

        [Test]
        public void Evaluate_ShakePeaksAtLiftoff_ThenFades()
        {
            Assert.AreEqual(0f, RocketLaunchSequence.Evaluate(Timeline, 5.0).Shake, 1e-4f);
            Assert.AreEqual(1f, RocketLaunchSequence.Evaluate(Timeline, 13.0).Shake, 1e-3f);
            Assert.AreEqual(0f, RocketLaunchSequence.Evaluate(Timeline, 20.0).Shake, 1e-4f);
        }

        [Test]
        public void Sequence_RaisesEventsOnceEach()
        {
            var sequence = new RocketLaunchSequence(Timeline);
            var rec = new Recorder();
            sequence.AddListener(rec);
            sequence.Begin(100.0);
            for (double now = 100.0; now < 140.0; now += 0.1) sequence.Update(now);

            CollectionAssert.AreEqual(new[] { LaunchPhase.Countdown, LaunchPhase.Ignition, LaunchPhase.Ascent, LaunchPhase.Complete }, rec.Phases);
            CollectionAssert.AreEqual(new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 }, rec.Ticks);
        }

        [Test]
        public void Sequence_SameStartTime_GivesSameStateOnTwoClients()
        {
            var host = new RocketLaunchSequence(Timeline);
            var client = new RocketLaunchSequence(Timeline);
            host.Begin(50.0);
            client.Begin(50.0);
            host.Update(66.0);
            client.Update(66.0);
            Assert.AreEqual(host.Current.Phase, client.Current.Phase);
            Assert.AreEqual(host.Current.Altitude, client.Current.Altitude, 1e-5f);
        }

        [Test]
        public void Schedule_TriggersOncePerSite()
        {
            var schedule = new LaunchSiteSchedule(firstDistance: 1000, repeatDistance: 3000, triggerAhead: 600f, visibleAhead: 900f, visibleBehind: 300f);
            Assert.AreEqual(0L, schedule.GetSiteIndex(0));
            Assert.IsFalse(schedule.IsVisible(schedule.GetSiteZ(0, 0)));
            Assert.IsTrue(schedule.IsVisible(schedule.GetSiteZ(0, 200)));
            Assert.IsFalse(schedule.ShouldTrigger(schedule.GetSiteZ(0, 300)));
            Assert.IsTrue(schedule.ShouldTrigger(schedule.GetSiteZ(0, 450)));
            Assert.AreEqual(1L, schedule.GetSiteIndex(1400));
            Assert.AreEqual(2600f, schedule.GetSiteZ(1, 1400), 1e-3f);
        }
    }
}
