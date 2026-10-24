#nullable enable
using System;
using System.Collections.Generic;
using InkAndAsh.StandaloneTests.E2ETests.Contracts;
using NUnit.Framework;

namespace InkAndAsh.StandaloneTests.E2ETests
{
    [TestFixture]
    public class TimeAndWeatherE2ETests
    {
        private TimeManager _timeManager = null!;
        private WeatherSystem _weatherSystem = null!;

        [SetUp]
        public void Setup()
        {
            _timeManager = new TimeManager();
            _weatherSystem = new WeatherSystem();
        }

        #region Tier 1: Feature Coverage (Baseline Happy Path)

        [Test]
        public void F4_T1_01_OneMinuteToInGameHourRatio()
        {
            // Initial state: Day 1, Hour 8, Minute 0
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(1));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f));

            // Advancing 60 real-time seconds must equal exactly 1 in-game hour
            _timeManager.Tick(60.0f);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(9));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f));
            Assert.That(_timeManager.TotalElapsedInGameMinutes, Is.EqualTo(60.0f));
        }

        [Test]
        public void F4_T1_02_HourTickEventFiresSequentially()
        {
            var ticksRecorded = new List<(int hour, int day)>();
            _timeManager.OnTimeHourTick += (hour, day) => ticksRecorded.Add((hour, day));

            // Advance 180 real-time seconds (3 in-game hours: 09:00, 10:00, 11:00)
            _timeManager.Tick(180.0f);

            Assert.That(ticksRecorded.Count, Is.EqualTo(3));
            Assert.That(ticksRecorded[0], Is.EqualTo((9, 1)));
            Assert.That(ticksRecorded[1], Is.EqualTo((10, 1)));
            Assert.That(ticksRecorded[2], Is.EqualTo((11, 1)));
        }

        [Test]
        public void F4_T1_03_DayTickEventFiresOn24HourRollover()
        {
            int dayFired = 0;
            _timeManager.OnTimeDayTick += day => dayFired = day;

            // Start at Hour 8; advancing 16 in-game hours brings clock to midnight (24:00 -> Hour 0, Day 2)
            // 16 hours = 16 * 60 = 960 real seconds
            _timeManager.Tick(960.0f);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(0));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));
            Assert.That(dayFired, Is.EqualTo(2));
        }

        [Test]
        public void F4_T1_04_CampsiteRestAdvances8Hours()
        {
            int hourTicksCount = 0;
            _timeManager.OnTimeHourTick += (h, d) => hourTicksCount++;

            // Overnight sleep: 8 hours
            _timeManager.AdvanceTime(8.0f);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(16)); // 8 + 8 = 16:00
            Assert.That(hourTicksCount, Is.EqualTo(8), "8-hour rest should fire 8 sequential hour ticks.");
        }

        [Test]
        public void F4_T1_05_BiomeWeatherTransitions()
        {
            _weatherSystem.SetBiome("biome_desert");
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Heat));
            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.5f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.5f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.1f));

            _weatherSystem.SetBiome("biome_mountains");
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Freezing));
            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(0.2f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(2.0f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.25f));
        }

        [Test]
        public void F4_T1_06_SpireCalibrationCalmsHazards()
        {
            bool spireEventFired = false;
            _weatherSystem.OnSpireCalibrated += (biome, spire) => spireEventFired = true;

            _weatherSystem.SetBiome("biome_mountains"); // Freezing blizzard active
            Assert.That(_weatherSystem.SpireCalibrated, Is.False);
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.25f));

            // Complete Spire Calibration sequence
            _weatherSystem.CalibrateSpire();

            Assert.That(_weatherSystem.SpireCalibrated, Is.True);
            Assert.That(spireEventFired, Is.True);
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Clear));
            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.0f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.0f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.0f));
        }

        #endregion

        #region Tier 2: Boundary & Corner Cases

        [Test]
        public void F4_T2_01_TickTimeWithZeroDeltaDoesNotAdvance()
        {
            _timeManager.Tick(0f);
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
        }

        [Test]
        public void F4_T2_02_TickAt59SecondsDoesNotTriggerHourTick()
        {
            bool hourTicked = false;
            _timeManager.OnTimeHourTick += (h, d) => hourTicked = true;

            _timeManager.Tick(59.9f);

            Assert.That(hourTicked, Is.False);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(59.9f).Within(0.01f));
        }

        [Test]
        public void F4_T2_03_PausedClockFreezesTimeProgression()
        {
            _timeManager.IsPaused = true;
            _timeManager.Tick(120.0f); // 2 in-game hours simulated

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8), "Clock must not advance while paused.");
            Assert.That(_timeManager.TotalElapsedInGameMinutes, Is.EqualTo(0f));
        }

        [Test]
        public void F4_T2_04_CampsiteRestCrossingMidnightRollover()
        {
            // Start rest at 20:00 (8:00 PM) Day 1
            _timeManager.AdvanceTime(12.0f); // Advance to 20:00
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(20));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(1));

            // Sleep for 8 hours (20:00 -> 04:00 Day 2)
            _timeManager.AdvanceTime(8.0f);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(4));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));
        }

        [Test]
        public void F4_T2_05_ExtremeWeatherMultipliersApplied()
        {
            _weatherSystem.SetWeather(WeatherType.Freezing);
            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(0.2f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(2.0f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.25f));
        }

        [Test]
        public void F4_T2_06_NegativeDeltaIgnoredSafely()
        {
            _timeManager.Tick(-60.0f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f));
        }

        #endregion

        #region Tier 3: Cross-Feature Combinations

        [Test]
        public void F4_T3_01_TimeManagerDrivesCaravanInventoryDecay()
        {
            var caravan = new CaravanInventory(StorageTier.FootPacks);
            caravan.AddItem("item_meat", "Meat", 2.0f, quantity: 1, durability: 100f, isPerishable: true);

            // Hook time manager hour tick to drive caravan decay
            _timeManager.OnTimeHourTick += (hour, day) =>
            {
                // Each in-game hour = 1.0 real-time minute of decay
                caravan.TickDecay(1.0f, _weatherSystem.SpoilageMultiplier);
            };

            // Advance 5 in-game hours
            _timeManager.AdvanceTime(5.0f);

            var meat = caravan.Items[0];
            // 5 hours * 1% per hour = 5% decay => 95% remaining
            Assert.That(meat.Durability, Is.EqualTo(95.0f).Within(0.0001f));
        }

        [Test]
        public void F4_T3_02_WeatherSystemModulatesTimeTickedDecay()
        {
            var caravan = new CaravanInventory(StorageTier.FootPacks);
            caravan.AddItem("item_fish", "Fish", 1.5f, quantity: 1, durability: 100f, isPerishable: true);

            _weatherSystem.SetBiome("biome_desert"); // Spoilage = 1.5x

            _timeManager.OnTimeHourTick += (hour, day) =>
            {
                caravan.TickDecay(1.0f, _weatherSystem.SpoilageMultiplier);
            };

            // Advance 10 in-game hours in Desert Heat
            _timeManager.AdvanceTime(10.0f);

            var fish = caravan.Items[0];
            // 10 hours * 1.0% * 1.5 = 15% decay => 85% remaining
            Assert.That(fish.Durability, Is.EqualTo(85.0f).Within(0.0001f));
        }

        [Test]
        public void F4_T3_03_CampsiteRestCausesPerishableItemToSpoil()
        {
            var caravan = new CaravanInventory(StorageTier.FootPacks);
            caravan.AddItem("fragile_ration", "Ration", 1.0f, quantity: 1, durability: 6.0f, isPerishable: true);

            bool spoiled = false;
            caravan.OnItemDecayed += (id, wasSpoiled) => spoiled = wasSpoiled;

            _timeManager.OnTimeHourTick += (h, d) => caravan.TickDecay(1.0f, 1.0f);

            // 8-hour sleep applies 8% decay on 6% durability item -> item spoils overnight
            _timeManager.AdvanceTime(8.0f);

            Assert.That(spoiled, Is.True);
            Assert.That(caravan.Items[0].IsSpoiled, Is.True);
        }

        [Test]
        public void F4_T3_04_SpireCalibrationResetsCombatDelayMultiplier()
        {
            _weatherSystem.SetBiome("biome_mountains"); // Blizzard delay 1.25x
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.25f));

            _weatherSystem.CalibrateSpire();
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.0f));
        }

        #endregion

        #region Tier 4: Real-World Scenarios

        [Test]
        public void F4_T4_01_FullDayNightExplorationAndRestCycle()
        {
            int totalHoursTicked = 0;
            int totalDaysTicked = 0;

            _timeManager.OnTimeHourTick += (h, d) => totalHoursTicked++;
            _timeManager.OnTimeDayTick += d => totalDaysTicked++;

            // Step 1: Morning exploration (08:00 to 14:00 = 6 hours)
            _weatherSystem.SetBiome("biome_grasslands");
            _timeManager.AdvanceTime(6.0f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(14));

            // Step 2: Afternoon travel into rainy swamp (14:00 to 20:00 = 6 hours)
            _weatherSystem.SetBiome("biome_swamp");
            _timeManager.AdvanceTime(6.0f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(20));
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Rain));

            // Step 3: Establish evening camp and sleep for 8 hours (20:00 to 04:00 Day 2)
            _timeManager.AdvanceTime(8.0f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(4));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));
            Assert.That(totalDaysTicked, Is.EqualTo(1));

            // Step 4: Early dawn preparation (04:00 to 08:00 = 4 hours)
            _timeManager.AdvanceTime(4.0f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));

            // Total hours elapsed: 6 + 6 + 8 + 4 = 24 in-game hours (exactly 1 full cycle)
            Assert.That(totalHoursTicked, Is.EqualTo(24));
        }

        #endregion
    }
}
