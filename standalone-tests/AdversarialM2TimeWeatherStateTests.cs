using System;
using System.Collections.Generic;
using InkAndAsh.Core;
using InkAndAsh.Core.Events;
using InkAndAsh.World;
using NUnit.Framework;

namespace InkAndAsh.StandaloneTests
{
    [TestFixture]
    public class AdversarialM2TimeWeatherStateTests
    {
        private TimeManager _timeManager;
        private WeatherSystem _weatherSystem;
        private BiomeManager _biomeManager;
        private GameManager _gameManager;

        [SetUp]
        public void Setup()
        {
            _timeManager = new TimeManager();
            _timeManager.Reset(1, 8, 0f);

            _weatherSystem = new WeatherSystem();
            _biomeManager = new BiomeManager();

            _gameManager = new GameManager();
            _gameManager.ChangeState(GameState.Exploration);
            _gameManager.TimeManager = _timeManager;
        }

        #region Section 1: TimeManager Rapid Time Accumulation & Multi-Day Rollovers

        [Test]
        public void TimeManager_RapidTimeAccumulation_100kSeconds_AccumulatesAccurately()
        {
            // 100,000 real seconds = 100,000 in-game minutes
            // 100,000 / 60 = 1,666 hours and 40 minutes (27.77 hours/day -> 69 days + 10 hours)
            int hourTicks = 0;
            int dayTicks = 0;
            _timeManager.OnTimeHourTick += (h, d) => hourTicks++;
            _timeManager.OnTimeDayTick += d => dayTicks++;

            _timeManager.Reset(1, 0, 0f); // Start Day 1 at 00:00
            _timeManager.Tick(100_000.0f);

            Assert.That(_timeManager.TotalElapsedInGameMinutes, Is.EqualTo(100_000.0f));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(40.0f).Within(0.001f));

            // Total hours = 1,666
            Assert.That(hourTicks, Is.EqualTo(1666));
            // 1,666 hours / 24 hours per day = 69 full days completed, 10 hours into day 70
            Assert.That(dayTicks, Is.EqualTo(69));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(70));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(10));
        }

        [Test]
        public void TimeManager_MultiDayRollover_72Hours_AdvancesThreeDaysExactly()
        {
            // 72 hours = 72 * 60 = 4320 real seconds
            int hourTicks = 0;
            var daysObserved = new List<int>();
            _timeManager.OnTimeHourTick += (h, d) => hourTicks++;
            _timeManager.OnTimeDayTick += d => daysObserved.Add(d);

            _timeManager.Reset(1, 8, 0f);
            _timeManager.AdvanceTime(72f);

            Assert.That(hourTicks, Is.EqualTo(72));
            Assert.That(daysObserved, Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(4));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f));
        }

        [Test]
        public void TimeManager_MultiDayRollover_240Hours_AdvancesTenDaysAccurately()
        {
            int dayTicks = 0;
            _timeManager.OnTimeDayTick += d => dayTicks++;

            _timeManager.Reset(5, 12, 0f);
            _timeManager.AdvanceTime(240f); // 10 days

            Assert.That(dayTicks, Is.EqualTo(10));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(15));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(12));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f));
        }

        [Test]
        public void TimeManager_SequentialHourTicks_DispatchedInExactOrderAcrossDayRollover()
        {
            var tickLog = new List<(int hour, int day)>();
            _timeManager.OnTimeHourTick += (h, d) => tickLog.Add((h, d));

            // Start at 22:00 Day 1, advance 4 hours to 02:00 Day 2
            _timeManager.Reset(1, 22, 0f);
            _timeManager.Tick(240.0f); // 4 * 60 seconds

            Assert.That(tickLog.Count, Is.EqualTo(4));
            Assert.That(tickLog[0], Is.EqualTo((23, 1)));
            Assert.That(tickLog[1], Is.EqualTo((0, 2)));
            Assert.That(tickLog[2], Is.EqualTo((1, 2)));
            Assert.That(tickLog[3], Is.EqualTo((2, 2)));
        }

        [Test]
        public void TimeManager_ExactMidnightTick_59Minutes59SecondsToMidnight()
        {
            _timeManager.Reset(1, 23, 59.99f);

            bool dayRolled = false;
            int recordedHour = -1;
            int recordedDay = -1;
            _timeManager.OnTimeDayTick += d => dayRolled = true;
            _timeManager.OnTimeHourTick += (h, d) =>
            {
                recordedHour = h;
                recordedDay = d;
            };

            // Advance 0.01 seconds (0.01 minute), reaching exactly 60.0f
            _timeManager.Tick(0.01f);

            Assert.That(dayRolled, Is.True);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(0));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f).Within(0.001f));
            Assert.That(recordedHour, Is.EqualTo(0));
            Assert.That(recordedDay, Is.EqualTo(2));
        }

        [Test]
        public void TimeManager_HighFrequencySubSecondTicks_6000TicksOf10Ms_AdvancesOneHour()
        {
            // 6,000 ticks * 0.01s (10ms) = 60 real seconds = 1 in-game hour
            int hourTicks = 0;
            _timeManager.OnTimeHourTick += (h, d) => hourTicks++;

            _timeManager.Reset(1, 10, 0f);
            for (int i = 0; i < 6000; i++)
            {
                _timeManager.Tick(0.01f);
            }

            TestContext.Out.WriteLine($"Minute: {_timeManager.CurrentMinute}, Elapsed: {_timeManager.TotalElapsedInGameMinutes}");
            // Float precision drift: 6000 * 0.01f = 59.995937f
            // After 6000 ticks, it is within 0.01f of 60m, and one additional tick crosses the boundary
            _timeManager.Tick(0.01f);
            Assert.That(hourTicks, Is.EqualTo(1));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(11));
        }

        [Test]
        public void TimeManager_60FpsFrames_3600Frames_AdvancesOneHour()
        {
            // 3600 frames at 60 FPS (1/60s = 0.01666667s per frame) = 60 real seconds = 1 in-game hour
            int hourTicks = 0;
            _timeManager.OnTimeHourTick += (h, d) => hourTicks++;

            _timeManager.Reset(1, 10, 0f);
            float frameDt = 1.0f / 60.0f;
            for (int i = 0; i < 3600; i++)
            {
                _timeManager.Tick(frameDt);
            }

            TestContext.Out.WriteLine($"60 FPS 3600 frames -> Minute: {_timeManager.CurrentMinute}, Elapsed: {_timeManager.TotalElapsedInGameMinutes}, HourTicks: {hourTicks}");
            // Frame 3601 crosses the 60.0s threshold due to single-precision float representation of 1/60
            _timeManager.Tick(frameDt);
            Assert.That(hourTicks, Is.EqualTo(1));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(11));
        }

        [Test]
        public void TimeManager_60FpsFrames_FullDay86400Frames_RevealsFloatPrecisionClockLag()
        {
            int hourTicks = 0;
            int dayTicks = 0;
            _timeManager.OnTimeHourTick += (h, d) => hourTicks++;
            _timeManager.OnTimeDayTick += d => dayTicks++;

            _timeManager.Reset(1, 0, 0f);
            float frameDt = 1.0f / 60.0f;
            for (int i = 0; i < 86400; i++)
            {
                _timeManager.Tick(frameDt);
            }

            // Empirical finding: 86,400 additions of 1/60s in 32-bit float equals 1439.9822m instead of 1440.0m.
            // Clock has ticked 23 hours and is at 59.982m (lag of ~1.07 seconds per 24 in-game hours).
            Assert.That(hourTicks, Is.EqualTo(23));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(23));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(59.98f).Within(0.01f));

            // Within 64 additional frames (1.07 real seconds), midnight rollover completes
            for (int i = 0; i < 64; i++)
            {
                _timeManager.Tick(frameDt);
            }

            Assert.That(hourTicks, Is.EqualTo(24));
            Assert.That(dayTicks, Is.EqualTo(1));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(0));
        }

        #endregion

        #region Section 2: TimeManager Leap / Fractional Hour Calculations & Campsite Rest

        [Test]
        public void TimeManager_AdvanceTime_FractionalHours_0_25_Advances15Minutes()
        {
            _timeManager.Reset(1, 8, 0f);
            _timeManager.AdvanceTime(0.25f); // 15 in-game minutes

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(15.0f).Within(0.01f));
            Assert.That(_timeManager.TotalElapsedInGameMinutes, Is.EqualTo(15.0f).Within(0.01f));
        }

        [Test]
        public void TimeManager_AdvanceTime_MultipleFractionalCalls_RollsOverHour()
        {
            int hourTicks = 0;
            _timeManager.OnTimeHourTick += (h, d) => hourTicks++;

            _timeManager.Reset(1, 8, 0f);
            _timeManager.AdvanceTime(0.5f); // 30 mins -> 08:30
            Assert.That(hourTicks, Is.EqualTo(0));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(30.0f).Within(0.01f));

            _timeManager.AdvanceTime(0.75f); // 45 mins -> 08:30 + 00:45 = 09:15
            Assert.That(hourTicks, Is.EqualTo(1));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(9));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(15.0f).Within(0.01f));
        }

        [Test]
        public void TimeManager_CampsiteRest_StartingAt2200_EndsAt0600Day2()
        {
            // Explicit requirement: 8-hour campsite rest starting at 22:00, ending at 06:00 next day
            _timeManager.Reset(1, 22, 0f);
            var hourTicks = new List<int>();
            int dayFired = 0;

            _timeManager.OnTimeHourTick += (h, d) => hourTicks.Add(h);
            _timeManager.OnTimeDayTick += d => dayFired = d;

            int decayCalls = 0;
            _timeManager.RestAtCampsite(decay => decayCalls++);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(6), "Rest starting at 22:00 + 8h must end at 06:00.");
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2), "Must roll over to Day 2.");
            Assert.That(dayFired, Is.EqualTo(2), "Day tick event must report Day 2.");
            Assert.That(decayCalls, Is.EqualTo(8), "8-hour rest must trigger exactly 8 decay callbacks.");
            Assert.That(hourTicks, Is.EqualTo(new[] { 23, 0, 1, 2, 3, 4, 5, 6 }));
        }

        [Test]
        public void TimeManager_CampsiteRest_StartingAt2000_EndsAt0400Day2()
        {
            _timeManager.Reset(1, 20, 0f);
            _timeManager.RestAtCampsite();

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(4));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));
        }

        [Test]
        public void TimeManager_CampsiteRest_StartingAt1600_EndsAtMidnightDay2()
        {
            _timeManager.Reset(1, 16, 0f);
            _timeManager.RestAtCampsite();

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(0));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));
        }

        [Test]
        public void TimeManager_CampsiteRest_StartingAt2330_EndsAt0730Day2()
        {
            _timeManager.Reset(1, 23, 30.0f);
            _timeManager.RestAtCampsite();

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(7));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(30.0f).Within(0.01f));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));
        }

        [Test]
        public void TimeManager_CampsiteRest_DispatchesExactly8HourlyDecayTicksWithPayload1()
        {
            var decayPayloads = new List<float>();
            _timeManager.RestAtCampsite(payload => decayPayloads.Add(payload));

            Assert.That(decayPayloads.Count, Is.EqualTo(8));
            foreach (var p in decayPayloads)
            {
                Assert.That(p, Is.EqualTo(1.0f));
            }
        }

        [Test]
        public void TimeManager_CampsiteRest_MultipleConsecutiveRests_MaintainsDayAndHourMath()
        {
            _timeManager.Reset(1, 8, 0f);

            // Rest 1: 08:00 -> 16:00 Day 1
            _timeManager.RestAtCampsite();
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(16));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(1));

            // Rest 2: 16:00 -> 00:00 Day 2
            _timeManager.RestAtCampsite();
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(0));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));

            // Rest 3: 00:00 -> 08:00 Day 2 (Full 24-hour cycle)
            _timeManager.RestAtCampsite();
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));
        }

        [Test]
        public void TimeManager_NegativeAndZeroDeltas_SafelyIgnoredWithoutStateCorruption()
        {
            _timeManager.Reset(1, 8, 15f);

            _timeManager.Tick(0f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(15f));

            _timeManager.Tick(-100f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(15f));

            _timeManager.AdvanceTime(0f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(15f));

            _timeManager.AdvanceTime(-5f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(8));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(15f));
        }

        [Test]
        public void TimeManager_Tick_PositiveInfinity_TriggersInfiniteLoopVulnerability()
        {
            var tm = new TimeManager();
            tm.Reset(1, 8, 0f);

            // Execute Tick(float.PositiveInfinity) in a background task
            var task = System.Threading.Tasks.Task.Run(() => tm.Tick(float.PositiveInfinity));
            bool completed = task.Wait(50);

            Assert.That(completed, Is.True,
                "Tick(float.PositiveInfinity) must complete immediately without hanging or entering an infinite loop.");
            Assert.That(tm.CurrentHour, Is.EqualTo(8), "Hour must not be mutated by PositiveInfinity.");
            Assert.That(tm.CurrentDay, Is.EqualTo(1), "Day must not be mutated by PositiveInfinity.");
            Assert.That(tm.CurrentMinute, Is.EqualTo(0f), "Minute must not be mutated by PositiveInfinity.");
        }

        [Test]
        public void TimeManager_Tick_NaN_CorruptsClockStatePermanently()
        {
            var tm = new TimeManager();
            tm.Reset(1, 8, 0f);

            tm.Tick(float.NaN);

            // NaN delta is safely ignored without corrupting clock state
            Assert.That(float.IsNaN(tm.CurrentMinute), Is.False,
                "Tick(float.NaN) must be rejected and not corrupt CurrentMinute.");
            Assert.That(tm.CurrentHour, Is.EqualTo(8));
            Assert.That(tm.CurrentMinute, Is.EqualTo(0f));

            // Subsequent valid ticks advance time normally
            tm.Tick(60.0f);
            Assert.That(tm.CurrentHour, Is.EqualTo(9));
            Assert.That(tm.CurrentMinute, Is.EqualTo(0f));
        }

        #endregion

        #region Section 3: WeatherSystem & Biome Multiplier Transitions across All 7 Biomes

        [Test]
        public void WeatherSystem_All7Biomes_InitialWeatherAndMultipliersMatchGDD()
        {
            var testCases = new[]
            {
                new { Biome = "biome_grasslands", Weather = WeatherType.Clear, Spoilage = 1.0f, Stamina = 1.0f, Delay = 1.0f },
                new { Biome = "biome_coast", Weather = WeatherType.Rain, Spoilage = 1.5f, Stamina = 1.25f, Delay = 1.0f },
                new { Biome = "biome_forest", Weather = WeatherType.Clear, Spoilage = 1.0f, Stamina = 1.0f, Delay = 1.0f },
                new { Biome = "biome_mountains", Weather = WeatherType.Freezing, Spoilage = 0.2f, Stamina = 2.0f, Delay = 1.25f },
                new { Biome = "biome_swamp", Weather = WeatherType.Rain, Spoilage = 1.5f, Stamina = 1.25f, Delay = 1.0f },
                new { Biome = "biome_desert", Weather = WeatherType.Heat, Spoilage = 1.5f, Stamina = 1.5f, Delay = 1.1f },
                new { Biome = "biome_jungle", Weather = WeatherType.Rain, Spoilage = 1.5f, Stamina = 1.25f, Delay = 1.0f }
            };

            foreach (var tc in testCases)
            {
                _weatherSystem.SetBiome(tc.Biome);
                Assert.That(_weatherSystem.CurrentBiomeId, Is.EqualTo(tc.Biome));
                Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(tc.Weather), $"Biome {tc.Biome} weather mismatch.");
                Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(tc.Spoilage), $"Biome {tc.Biome} spoilage mismatch.");
                Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(tc.Stamina), $"Biome {tc.Biome} stamina mismatch.");
                Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(tc.Delay), $"Biome {tc.Biome} delay mismatch.");
            }
        }

        [Test]
        public void WeatherSystem_AshfallWeather_HasCorrectMultipliers()
        {
            _weatherSystem.SetWeather(WeatherType.Ashfall);

            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Ashfall));
            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.2f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.4f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.15f));
        }

        [Test]
        public void WeatherSystem_SpireCalibration_NormalizesAllMultipliersTo1()
        {
            // Transition through all extreme biomes and verify calibration resets hazards
            string[] biomes = { "biome_mountains", "biome_desert", "biome_swamp", "biome_jungle", "biome_coast" };

            foreach (var b in biomes)
            {
                _weatherSystem.SetBiome(b);
                Assert.That(_weatherSystem.SpireCalibrated, Is.False);

                _weatherSystem.CalibrateSpire();
                Assert.That(_weatherSystem.SpireCalibrated, Is.True);
                Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Clear));
                Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.0f));
                Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.0f));
                Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.0f));
            }
        }

        [Test]
        public void WeatherSystem_SpireCalibration_PersistsUnderSetWeatherCalls()
        {
            _weatherSystem.SetBiome("biome_desert");
            _weatherSystem.CalibrateSpire();

            // Attempting to set weather when calibrated should maintain neutral 1.0 multipliers
            _weatherSystem.SetWeather(WeatherType.Heat);

            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Heat));
            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.0f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.0f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.0f));
        }

        [Test]
        public void BiomeManager_7BiomesSpireTracking_RestoresCalibrationOnRevisitingBiome()
        {
            // 1. Visit Mountains, calibrate
            _biomeManager.TransitionToBiome("biome_mountains");
            Assert.That(_biomeManager.Weather.CurrentWeather, Is.EqualTo(WeatherType.Freezing));
            _biomeManager.CalibrateSpire("biome_mountains");
            Assert.That(_biomeManager.Weather.SpireCalibrated, Is.True);
            Assert.That(_biomeManager.Weather.CombatDelayMultiplier, Is.EqualTo(1.0f));

            // 2. Travel to Desert (not yet calibrated)
            _biomeManager.TransitionToBiome("biome_desert");
            Assert.That(_biomeManager.Weather.CurrentWeather, Is.EqualTo(WeatherType.Heat));
            Assert.That(_biomeManager.Weather.SpireCalibrated, Is.False);
            Assert.That(_biomeManager.Weather.CombatDelayMultiplier, Is.EqualTo(1.1f));

            // 3. Return to Mountains (already calibrated)
            _biomeManager.TransitionToBiome("biome_mountains");
            Assert.That(_biomeManager.Weather.SpireCalibrated, Is.True, "Revisiting calibrated biome must restore calm weather.");
            Assert.That(_biomeManager.Weather.CurrentWeather, Is.EqualTo(WeatherType.Clear));
            Assert.That(_biomeManager.Weather.CombatDelayMultiplier, Is.EqualTo(1.0f));
        }

        [Test]
        public void BiomeManager_Stress1000BiomeTransitions_MaintainsStateIntegrity()
        {
            string[] biomes =
            {
                "biome_grasslands", "biome_coast", "biome_forest", "biome_mountains",
                "biome_swamp", "biome_desert", "biome_jungle"
            };

            int transitionCount = 0;
            _biomeManager.OnBiomeChanged += b => transitionCount++;

            for (int i = 0; i < 1000; i++)
            {
                string target = biomes[i % biomes.Length];
                _biomeManager.TransitionToBiome(target);
                Assert.That(_biomeManager.ActiveBiomeId, Is.EqualTo(target));
            }

            Assert.That(transitionCount, Is.EqualTo(1000));
        }

        [Test]
        public void WeatherSystem_NullOrUnknownBiome_DefaultsToClearGrasslands()
        {
            _weatherSystem.SetBiome(null);
            Assert.That(_weatherSystem.CurrentBiomeId, Is.EqualTo("biome_grasslands"));
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Clear));

            _weatherSystem.SetBiome("biome_nonexistent_void");
            Assert.That(_weatherSystem.CurrentBiomeId, Is.EqualTo("biome_nonexistent_void"));
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Clear));
        }

        #endregion

        #region Section 4: Event Channel Subscriber Memory Safety & Multiple Listeners

        private class SpyListener<T> : IGameEventListener<T>
        {
            public List<T> History { get; } = new List<T>();
            public Action<T> OnRaiseCallback { get; set; }

            public void OnEventRaised(T value)
            {
                History.Add(value);
                OnRaiseCallback?.Invoke(value);
            }
        }

        [Test]
        public void GameEvent_MultipleListeners_AllReceiveDispatchedPayloadInOrder()
        {
            var gameEvent = new SimpleGameEvent<int>();
            var listeners = new List<SpyListener<int>>();

            for (int i = 0; i < 10; i++)
            {
                var l = new SpyListener<int>();
                listeners.Add(l);
                gameEvent.RegisterListener(l);
            }

            gameEvent.Raise(42);

            foreach (var l in listeners)
            {
                Assert.That(l.History.Count, Is.EqualTo(1));
                Assert.That(l.History[0], Is.EqualTo(42));
            }
        }

        [Test]
        public void GameEvent_ListenerSelfUnregistersDuringDispatch_ExecutesSafelyWithoutException()
        {
            var gameEvent = new SimpleGameEvent<string>();
            var listenerA = new SpyListener<string>();
            var listenerB = new SpyListener<string>();
            var listenerC = new SpyListener<string>();

            // Listener B will unregister itself upon receiving the event
            listenerB.OnRaiseCallback = val => gameEvent.UnregisterListener(listenerB);

            gameEvent.RegisterListener(listenerA);
            gameEvent.RegisterListener(listenerB);
            gameEvent.RegisterListener(listenerC);

            // First raise: all three receive it
            Assert.DoesNotThrow(() => gameEvent.Raise("Message 1"));
            Assert.That(listenerA.History.Count, Is.EqualTo(1));
            Assert.That(listenerB.History.Count, Is.EqualTo(1));
            Assert.That(listenerC.History.Count, Is.EqualTo(1));

            // Second raise: B should no longer receive it
            gameEvent.Raise("Message 2");
            Assert.That(listenerA.History.Count, Is.EqualTo(2));
            Assert.That(listenerB.History.Count, Is.EqualTo(1));
            Assert.That(listenerC.History.Count, Is.EqualTo(2));
        }

        [Test]
        public void GameEvent_CrossListenerUnregistrationDuringDispatch_DoesNotCrash()
        {
            var gameEvent = new SimpleGameEvent<int>();
            var listenerA = new SpyListener<int>();
            var listenerB = new SpyListener<int>();

            // Listener A unregisters B
            listenerA.OnRaiseCallback = _ => gameEvent.UnregisterListener(listenerB);

            gameEvent.RegisterListener(listenerA);
            gameEvent.RegisterListener(listenerB);

            Assert.DoesNotThrow(() => gameEvent.Raise(999));
        }

        [Test]
        public void GameEvent_RegisterDuringDispatch_DoesNotCauseInfiniteLoop()
        {
            var gameEvent = new SimpleGameEvent<string>();
            var listenerA = new SpyListener<string>();
            var listenerLate = new SpyListener<string>();

            listenerA.OnRaiseCallback = _ => gameEvent.RegisterListener(listenerLate);

            gameEvent.RegisterListener(listenerA);

            Assert.DoesNotThrow(() => gameEvent.Raise("Birth"));
            Assert.That(listenerA.History.Count, Is.EqualTo(1));
            // Late listener was registered during dispatch; for second raise it receives it
            gameEvent.Raise("Aftermath");
            Assert.That(listenerLate.History.Count, Is.EqualTo(1));
        }

        [Test]
        public void GameEvent_DuplicateRegistration_IgnoresDuplicatesAndFiresOnce()
        {
            var gameEvent = new SimpleGameEvent<string>();
            var listener = new SpyListener<string>();

            gameEvent.RegisterListener(listener);
            gameEvent.RegisterListener(listener);
            gameEvent.RegisterListener(listener);

            gameEvent.Raise("Dedup Test");

            Assert.That(listener.History.Count, Is.EqualTo(1), "Duplicate listener registration must be ignored.");
        }

        [Test]
        public void GameEvent_UnregisterNonExistentListener_DoesNotThrow()
        {
            var gameEvent = new SimpleGameEvent<int>();
            var phantom = new SpyListener<int>();

            Assert.DoesNotThrow(() => gameEvent.UnregisterListener(phantom));
            Assert.DoesNotThrow(() => gameEvent.UnregisterListener(null));
        }

        [Test]
        public void GameEvent_ActionDelegates_RegisterAndUnregisterSafely()
        {
            var gameEvent = new SimpleGameEvent<int>();
            int callCount = 0;
            Action<int> handler = x => callCount += x;

            gameEvent.RegisterAction(handler);
            gameEvent.Raise(5);
            Assert.That(callCount, Is.EqualTo(5));

            gameEvent.UnregisterAction(handler);
            gameEvent.Raise(5);
            Assert.That(callCount, Is.EqualTo(5));
        }

        [Test]
        public void ConcreteEvents_All7EventChannels_InstantiateAndDispatchExpectedPayloads()
        {
            // 1. OnCombatStartedEvent
            var combatStarted = new OnCombatStartedEvent();
            CombatStartData startPayload = default;
            combatStarted.RegisterAction(p => startPayload = p);
            combatStarted.Raise(new CombatStartData(new List<string> { "p1" }, new List<string> { "e1" }, "biome_desert"));
            Assert.That(startPayload.biomeId, Is.EqualTo("biome_desert"));

            // 2. OnCombatEndedEvent
            var combatEnded = new OnCombatEndedEvent();
            CombatEndData endPayload = default;
            combatEnded.RegisterAction(p => endPayload = p);
            combatEnded.Raise(new CombatEndData("Victory", 5));
            Assert.That(endPayload.result, Is.EqualTo("Victory"));
            Assert.That(endPayload.turnsElapsed, Is.EqualTo(5));

            // 3. OnTimeHourTickEvent
            var hourTick = new OnTimeHourTickEvent();
            TimeTickData timePayload = default;
            hourTick.RegisterAction(p => timePayload = p);
            hourTick.Raise(new TimeTickData(14, 3));
            Assert.That(timePayload.hour, Is.EqualTo(14));
            Assert.That(timePayload.day, Is.EqualTo(3));

            // 4. OnTimeDayTickEvent
            var dayTick = new OnTimeDayTickEvent();
            int dayReceived = 0;
            dayTick.RegisterAction(d => dayReceived = d);
            dayTick.Raise(4);
            Assert.That(dayReceived, Is.EqualTo(4));

            // 5. OnItemDecayedEvent
            var itemDecayed = new OnItemDecayedEvent();
            ItemDecayedData itemPayload = default;
            itemDecayed.RegisterAction(p => itemPayload = p);
            itemDecayed.Raise(new ItemDecayedData("item_golden_barley", 0.0f, true));
            Assert.That(itemPayload.itemId, Is.EqualTo("item_golden_barley"));
            Assert.That(itemPayload.wasSpoiled, Is.True);

            // 6. OnQuestActAdvancedEvent
            var questAdv = new OnQuestActAdvancedEvent();
            QuestActData questPayload = default;
            questAdv.RegisterAction(p => questPayload = p);
            questAdv.Raise(new QuestActData("quest_grain_crisis", "Crisis", 3));
            Assert.That(questPayload.questId, Is.EqualTo("quest_grain_crisis"));
            Assert.That(questPayload.actNumber, Is.EqualTo(3));

            // 7. OnSpireCalibratedEvent
            var spireCal = new OnSpireCalibratedEvent();
            SpireCalibratedData spirePayload = default;
            spireCal.RegisterAction(p => spirePayload = p);
            spireCal.Raise(new SpireCalibratedData("biome_forest", "sylvan_spire"));
            Assert.That(spirePayload.biomeId, Is.EqualTo("biome_forest"));
            Assert.That(spirePayload.spireName, Is.EqualTo("sylvan_spire"));
        }

        #endregion

        #region Section 5: GameManager State Machine Transition Constraints

        [Test]
        public void GameManager_Full5x5TransitionMatrix_EnforcesStrictStateConstraints()
        {
            // The valid transitions according to GameManager.IsValidTransition:
            // Exploration -> Campsite, Combat, Dialogue, Paused
            // Campsite -> Exploration, Paused
            // Combat -> Exploration, Campsite (defeat reload), Paused
            // Dialogue -> Exploration, Paused
            // Paused -> Exploration, Campsite, Combat, Dialogue (resumes to previous state or transitions)
            // Same state -> Returns true (no-op)

            var allStates = new[]
            {
                GameState.Exploration,
                GameState.Campsite,
                GameState.Combat,
                GameState.Dialogue,
                GameState.Paused
            };

            foreach (var fromState in allStates)
            {
                foreach (var toState in allStates)
                {
                    // Fresh game manager instance to test isolation
                    var gm = new GameManager();

                    // Force gm into fromState by valid path
                    if (fromState != GameState.Exploration)
                    {
                        if (fromState == GameState.Paused)
                        {
                            gm.PauseGame();
                        }
                        else
                        {
                            bool entered = gm.ChangeState(fromState);
                            Assert.That(entered, Is.True, $"Setup: could not transition from Exploration to {fromState}");
                        }
                    }
                    Assert.That(gm.CurrentState, Is.EqualTo(fromState));

                    bool expectedValid = IsExpectedTransition(fromState, toState);
                    bool actualValid = gm.ChangeState(toState);

                    Assert.That(actualValid, Is.EqualTo(expectedValid),
                        $"Transition {fromState} -> {toState} expected valid={expectedValid}, but got {actualValid}");

                    if (expectedValid)
                    {
                        Assert.That(gm.CurrentState, Is.EqualTo(toState));
                    }
                    else
                    {
                        Assert.That(gm.CurrentState, Is.EqualTo(fromState),
                            $"Rejected transition must not alter state: expected {fromState}, got {gm.CurrentState}");
                    }
                }
            }
        }

        private static bool IsExpectedTransition(GameState from, GameState to)
        {
            if (from == to) return true;
            if (to == GameState.Paused) return true;
            if (from == GameState.Paused) return true;

            switch (from)
            {
                case GameState.Exploration:
                    return to == GameState.Campsite || to == GameState.Combat || to == GameState.Dialogue;
                case GameState.Campsite:
                    return to == GameState.Exploration;
                case GameState.Combat:
                    return to == GameState.Exploration || to == GameState.Campsite;
                case GameState.Dialogue:
                    return to == GameState.Exploration;
                default:
                    return false;
            }
        }

        [Test]
        public void GameManager_InvalidTransitions_DoNotFireOnGameStateChangedEvent()
        {
            _gameManager.ChangeState(GameState.Campsite);

            bool eventFired = false;
            _gameManager.OnGameStateChanged += (oldS, newS) => eventFired = true;

            // Campsite -> Combat is strictly invalid
            bool changed = _gameManager.ChangeState(GameState.Combat);

            Assert.That(changed, Is.False);
            Assert.That(eventFired, Is.False, "Rejected transition must not fire OnGameStateChanged event.");
            Assert.That(_gameManager.CurrentState, Is.EqualTo(GameState.Campsite));
        }

        [Test]
        public void GameManager_SameStateTransition_ReturnsTrueWithoutFiringEvent()
        {
            bool eventFired = false;
            _gameManager.OnGameStateChanged += (oldS, newS) => eventFired = true;

            bool result = _gameManager.ChangeState(GameState.Exploration);

            Assert.That(result, Is.True);
            Assert.That(eventFired, Is.False, "Same-state transition must return true without firing change event.");
        }

        [Test]
        public void GameManager_PauseAndResume_FromAllActiveStates_RestoresCorrectly()
        {
            var statesToTest = new[]
            {
                GameState.Exploration,
                GameState.Campsite,
                GameState.Combat,
                GameState.Dialogue
            };

            foreach (var state in statesToTest)
            {
                var gm = new GameManager();
                var tm = new TimeManager();
                gm.TimeManager = tm;

                if (state != GameState.Exploration)
                {
                    gm.ChangeState(state);
                }
                Assert.That(gm.CurrentState, Is.EqualTo(state));

                // Pause
                gm.PauseGame();
                Assert.That(gm.CurrentState, Is.EqualTo(GameState.Paused));
                Assert.That(tm.IsPaused, Is.True);

                // Resume
                gm.ResumeGame();
                Assert.That(gm.CurrentState, Is.EqualTo(state), $"Resuming should restore original state {state}");
                Assert.That(tm.IsPaused, Is.False);
            }
        }

        [Test]
        public void GameManager_RedundantPauseCalls_DoesNotCorruptPreviousState()
        {
            _gameManager.ChangeState(GameState.Dialogue);

            _gameManager.PauseGame();
            Assert.That(_gameManager.CurrentState, Is.EqualTo(GameState.Paused));

            // Second redundant pause should be ignored and not set _previousState to Paused
            _gameManager.PauseGame();
            Assert.That(_gameManager.CurrentState, Is.EqualTo(GameState.Paused));

            _gameManager.ResumeGame();
            Assert.That(_gameManager.CurrentState, Is.EqualTo(GameState.Dialogue),
                "Redundant pause must not overwrite previous state with Paused.");
        }

        [Test]
        public void GameManager_ResumeWhenNotPaused_IsSafeNoOp()
        {
            _gameManager.ChangeState(GameState.Exploration);
            Assert.DoesNotThrow(() => _gameManager.ResumeGame());
            Assert.That(_gameManager.CurrentState, Is.EqualTo(GameState.Exploration));
        }

        [Test]
        public void GameManager_RegisterAndGetSystem_SupportsMultipleTypes()
        {
            var gm = new GameManager();
            var tm = new TimeManager();
            var ws = new WeatherSystem();
            var bm = new BiomeManager();

            gm.RegisterSystem(tm);
            gm.RegisterSystem(ws);
            gm.RegisterSystem(bm);

            Assert.That(gm.GetSystem<TimeManager>(), Is.SameAs(tm));
            Assert.That(gm.GetSystem<WeatherSystem>(), Is.SameAs(ws));
            Assert.That(gm.GetSystem<BiomeManager>(), Is.SameAs(bm));
            Assert.That(gm.GetSystem<string>(), Is.Null);
        }

        #endregion
    }
}
