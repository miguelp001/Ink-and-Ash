using System;
using System.Collections.Generic;
using NUnit.Framework;
using InkAndAsh.Core;

namespace InkAndAsh.Tests.EditMode
{
    [TestFixture]
    public class TimeManagerTests
    {
        private TimeManager _timeManager;

        [SetUp]
        public void SetUp()
        {
            _timeManager = new TimeManager();
            _timeManager.Reset(day: 1, hour: 8, minute: 0f);
        }

        #region 1. Temporal Compression Ratio (1 Real Minute = 1 In-Game Hour)

        [Test]
        public void TimeRatio_SixtyRealSecondsAdvancesExactlyOneInGameHour()
        {
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(1));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f));

            // 60 real seconds = 1 real minute = 1 in-game hour
            _timeManager.Tick(60.0f);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(9));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(1));
            Assert.That(_timeManager.TotalElapsedInGameMinutes, Is.EqualTo(60.0f).Within(0.0001f));
        }

        [Test]
        public void TimeRatio_SubMinuteDeltaDoesNotTriggerHourTick()
        {
            int hourTickCount = 0;
            _timeManager.OnTimeHourTick += (hour, day) => hourTickCount++;

            // 59.5 real seconds = 59.5 in-game minutes
            _timeManager.Tick(59.5f);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(59.5f).Within(0.0001f));
            Assert.That(hourTickCount, Is.EqualTo(0), "Hour tick must not fire before 60 seconds have elapsed.");
        }

        #endregion

        #region 2. Hour Tick Events

        [Test]
        public void HourTick_FiresSequentiallyForMultiHourAdvance()
        {
            var ticks = new List<(int hour, int day)>();
            _timeManager.OnTimeHourTick += (hour, day) => ticks.Add((hour, day));

            // Advance 180 real seconds = 3 in-game hours
            _timeManager.Tick(180.0f);

            Assert.That(ticks.Count, Is.EqualTo(3));
            Assert.That(ticks[0], Is.EqualTo((9, 1)));
            Assert.That(ticks[1], Is.EqualTo((10, 1)));
            Assert.That(ticks[2], Is.EqualTo((11, 1)));

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(11));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f).Within(0.0001f));
        }

        #endregion

        #region 3. Day Rollover & Day Tick Events

        [Test]
        public void DayTick_FiresOnMidnightCrossing()
        {
            // Start at 23:00 Day 1
            _timeManager.Reset(day: 1, hour: 23, minute: 0f);

            int dayTickFired = 0;
            int recordedDay = 0;
            _timeManager.OnTimeDayTick += (day) =>
            {
                dayTickFired++;
                recordedDay = day;
            };

            var hourTicks = new List<(int hour, int day)>();
            _timeManager.OnTimeHourTick += (h, d) => hourTicks.Add((h, d));

            // Tick 60 real seconds: crosses midnight into Day 2
            _timeManager.Tick(60.0f);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(0));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));

            Assert.That(dayTickFired, Is.EqualTo(1));
            Assert.That(recordedDay, Is.EqualTo(2));

            Assert.That(hourTicks.Count, Is.EqualTo(1));
            Assert.That(hourTicks[0], Is.EqualTo((0, 2)));
        }

        #endregion

        #region 4. Campsite Rest (8-Hour Skip)

        [Test]
        public void CampsiteRest_AdvancesExactlyEightInGameHours()
        {
            // Rest starting at 20:00 (Evening) Day 1
            _timeManager.Reset(day: 1, hour: 20, minute: 0f);

            int hourTicks = 0;
            _timeManager.OnTimeHourTick += (h, d) => hourTicks++;

            int dayTicks = 0;
            _timeManager.OnTimeDayTick += (d) => dayTicks++;

            int decayCallbacks = 0;
            float totalDecayMinutes = 0f;

            _timeManager.RestAtCampsite((minute) =>
            {
                decayCallbacks++;
                totalDecayMinutes += minute;
            });

            // 20:00 + 8 hours => 04:00 Day 2
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(4));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));

            Assert.That(hourTicks, Is.EqualTo(8), "8 sequential hour ticks must be emitted.");
            Assert.That(dayTicks, Is.EqualTo(1), "Day tick must be emitted on midnight crossing.");
            Assert.That(decayCallbacks, Is.EqualTo(8), "Decay callback must be invoked 8 times.");
            Assert.That(totalDecayMinutes, Is.EqualTo(8.0f).Within(0.0001f));
        }

        #endregion

        #region 5. Advance Time & Pause Mechanics

        [Test]
        public void AdvanceTime_AdvancesSpecifiedFractionalHours()
        {
            // Advance 4.5 in-game hours from 08:00 => 12:30
            _timeManager.AdvanceTime(4.5f);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(12));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(30.0f).Within(0.01f));
        }

        [Test]
        public void IsPaused_FreezesAllTimeAdvancement()
        {
            _timeManager.IsPaused = true;

            int hourTicks = 0;
            _timeManager.OnTimeHourTick += (h, d) => hourTicks++;

            _timeManager.Tick(120.0f); // 2 minutes would be 2 hours

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f));
            Assert.That(_timeManager.TotalElapsedInGameMinutes, Is.EqualTo(0f));
            Assert.That(hourTicks, Is.EqualTo(0));

            // Resume
            _timeManager.IsPaused = false;
            _timeManager.Tick(60.0f);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(9));
            Assert.That(hourTicks, Is.EqualTo(1));
        }

        [Test]
        public void Reset_InitializesSpecifiedDayAndClock()
        {
            _timeManager.Reset(day: 5, hour: 14, minute: 30f);

            Assert.That(_timeManager.CurrentDay, Is.EqualTo(5));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(14));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(30f));
            Assert.That(_timeManager.TotalElapsedInGameMinutes, Is.EqualTo(0f));
            Assert.That(_timeManager.IsPaused, Is.False);
        }

        [Test]
        public void Tick_ZeroOrNegativeDeltaIgnoredSafely()
        {
            _timeManager.Tick(0f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));

            _timeManager.Tick(-15f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
        }

        #endregion
    }
}
