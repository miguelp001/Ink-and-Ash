using System;

namespace InkAndAsh.Core
{
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    public class TimeManager : UnityEngine.MonoBehaviour
#else
    public class TimeManager
#endif
    {
        private static TimeManager _instance;
        private static readonly object _lock = new object();

        public static TimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TimeManager();
                        }
                    }
                }
                return _instance;
            }
            protected set => _instance = value;
        }

        public int CurrentDay { get; private set; } = 1;
        public int CurrentHour { get; private set; } = 8; // Starts at 08:00 (Morning)
        public float CurrentMinute { get; private set; } = 0f;
        public float TotalElapsedInGameMinutes { get; private set; } = 0f;
        public bool IsPaused { get; set; } = false;

        public event Action<int, int> OnTimeHourTick; // (hour, day)
        public event Action<int> OnTimeDayTick;       // (day)

        public TimeManager()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                UnityEngine.Destroy(gameObject);
                return;
            }
            _instance = this;
            UnityEngine.DontDestroyOnLoad(gameObject);
        }

        protected virtual void Update()
        {
            Tick(UnityEngine.Time.deltaTime);
        }
#endif

        public void Reset(int day = 1, int hour = 8, float minute = 0f)
        {
            CurrentDay = Math.Max(1, day);
            CurrentHour = Math.Clamp(hour, 0, 23);
            CurrentMinute = Math.Clamp(minute, 0f, 59.99f);
            TotalElapsedInGameMinutes = 0f;
            IsPaused = false;
        }

        public void Tick(float realDeltaTimeSeconds)
        {
            if (IsPaused || float.IsNaN(realDeltaTimeSeconds) || float.IsInfinity(realDeltaTimeSeconds) || realDeltaTimeSeconds <= 0f) return;

            // 1 real second = 1 in-game minute (1 real minute = 1 in-game hour)
            float inGameMinutesToAdd = realDeltaTimeSeconds;
            TotalElapsedInGameMinutes += inGameMinutesToAdd;
            CurrentMinute += inGameMinutesToAdd;

            while (CurrentMinute >= 60.0f)
            {
                CurrentMinute -= 60.0f;
                CurrentHour++;

                if (CurrentHour >= 24)
                {
                    CurrentHour = 0;
                    CurrentDay++;
                    OnTimeDayTick?.Invoke(CurrentDay);
                }

                OnTimeHourTick?.Invoke(CurrentHour, CurrentDay);
            }
        }

        public void AdvanceTime(float inGameHours)
        {
            if (inGameHours <= 0f) return;

            int wholeHours = (int)Math.Floor(inGameHours);
            float remainingFraction = inGameHours - wholeHours;

            for (int i = 0; i < wholeHours; i++)
            {
                Tick(60.0f); // 60 real seconds = 1 in-game hour
            }

            if (remainingFraction > 0.001f)
            {
                Tick(remainingFraction * 60.0f);
            }
        }

        public void RestAtCampsite(Action<float> hourlyDecayCallback = null)
        {
            // Campsite Rest: advances time by 8 in-game hours
            for (int i = 0; i < 8; i++)
            {
                Tick(60.0f); // 1 in-game hour = 1 real minute of elapsed time
                hourlyDecayCallback?.Invoke(1.0f);
            }
        }
    }
}

#if STANDALONE_TEST
namespace InkAndAsh.StandaloneTests
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using InkAndAsh.Core;

    [TestFixture]
    public class TimeManagerProductionTests
    {
        private TimeManager _timeManager;

        [SetUp]
        public void Setup()
        {
            _timeManager = new TimeManager();
            _timeManager.Reset(1, 8, 0f);
        }

        [Test]
        public void TimeManager_SixtySecondsEqualsOneInGameHour()
        {
            _timeManager.Tick(60.0f);
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(9));
            Assert.That(_timeManager.CurrentMinute, Is.EqualTo(0f));
            Assert.That(_timeManager.TotalElapsedInGameMinutes, Is.EqualTo(60.0f));
        }

        [Test]
        public void TimeManager_EmitsSequentialHourTicks()
        {
            var ticks = new List<(int, int)>();
            _timeManager.OnTimeHourTick += (h, d) => ticks.Add((h, d));

            _timeManager.Tick(180.0f); // 3 hours

            Assert.That(ticks.Count, Is.EqualTo(3));
            Assert.That(ticks[0], Is.EqualTo((9, 1)));
            Assert.That(ticks[1], Is.EqualTo((10, 1)));
            Assert.That(ticks[2], Is.EqualTo((11, 1)));
            Assert.That(_timeManager.CurrentHour, Is.EqualTo(11));
        }

        [Test]
        public void TimeManager_MidnightRollover_AdvancesDay()
        {
            _timeManager.Reset(1, 23, 0f);
            int dayFired = 0;
            _timeManager.OnTimeDayTick += d => dayFired = d;

            _timeManager.Tick(60.0f); // Advance 1 hr from 23:00 to 00:00

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(0));
            Assert.That(_timeManager.CurrentDay, Is.EqualTo(2));
            Assert.That(dayFired, Is.EqualTo(2));
        }

        [Test]
        public void TimeManager_CampsiteRest_AdvancesEightHours_AndCallsDecay()
        {
            int decayCalls = 0;
            _timeManager.RestAtCampsite(mins => decayCalls++);

            Assert.That(_timeManager.CurrentHour, Is.EqualTo(16)); // 8 + 8 = 16:00
            Assert.That(decayCalls, Is.EqualTo(8));
        }
    }
}
#endif
