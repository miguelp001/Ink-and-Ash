#nullable enable
using System;

namespace InkAndAsh.StandaloneTests.E2ETests.Contracts
{
    public enum WeatherType
    {
        Clear,
        Rain,
        Heat,
        Freezing,
        Ashfall
    }

    public class WeatherSystem
    {
        public string CurrentBiomeId { get; private set; } = "biome_grasslands";
        public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;
        public bool SpireCalibrated { get; private set; }

        public float SpoilageMultiplier { get; private set; } = 1.0f;
        public float StaminaMultiplier { get; private set; } = 1.0f;
        public float CombatDelayMultiplier { get; private set; } = 1.0f;

        public event Action<string, string>? OnSpireCalibrated;

        public WeatherSystem()
        {
            UpdateMultipliers();
        }

        public void SetBiome(string biomeId)
        {
            CurrentBiomeId = biomeId;
            SpireCalibrated = false;

            switch (biomeId.ToLowerInvariant())
            {
                case "biome_mountains":
                    SetWeather(WeatherType.Freezing);
                    break;
                case "biome_desert":
                    SetWeather(WeatherType.Heat);
                    break;
                case "biome_swamp":
                case "biome_coast":
                    SetWeather(WeatherType.Rain);
                    break;
                default:
                    SetWeather(WeatherType.Clear);
                    break;
            }
        }

        public void SetWeather(WeatherType weather)
        {
            CurrentWeather = weather;
            UpdateMultipliers();
        }

        public void CalibrateSpire()
        {
            SpireCalibrated = true;
            CurrentWeather = WeatherType.Clear;
            UpdateMultipliers();
            OnSpireCalibrated?.Invoke(CurrentBiomeId, $"{CurrentBiomeId}_spire");
        }

        private void UpdateMultipliers()
        {
            if (SpireCalibrated)
            {
                SpoilageMultiplier = 1.0f;
                StaminaMultiplier = 1.0f;
                CombatDelayMultiplier = 1.0f;
                return;
            }

            switch (CurrentWeather)
            {
                case WeatherType.Heat:
                    SpoilageMultiplier = 1.5f;
                    StaminaMultiplier = 1.5f;
                    CombatDelayMultiplier = 1.1f;
                    break;
                case WeatherType.Freezing:
                    SpoilageMultiplier = 0.2f;
                    StaminaMultiplier = 2.0f;
                    CombatDelayMultiplier = 1.25f;
                    break;
                case WeatherType.Rain:
                    SpoilageMultiplier = 1.5f;
                    StaminaMultiplier = 1.25f;
                    CombatDelayMultiplier = 1.0f;
                    break;
                case WeatherType.Ashfall:
                    SpoilageMultiplier = 1.2f;
                    StaminaMultiplier = 1.4f;
                    CombatDelayMultiplier = 1.15f;
                    break;
                case WeatherType.Clear:
                default:
                    SpoilageMultiplier = 1.0f;
                    StaminaMultiplier = 1.0f;
                    CombatDelayMultiplier = 1.0f;
                    break;
            }
        }
    }

    public class TimeManager
    {
        public int CurrentDay { get; private set; } = 1;
        public int CurrentHour { get; private set; } = 8;
        public float CurrentMinute { get; private set; }
        public float TotalElapsedInGameMinutes { get; private set; }
        public bool IsPaused { get; set; }

        public event Action<int, int>? OnTimeHourTick; // (hour, day)
        public event Action<int>? OnTimeDayTick;       // (day)

        public void Tick(float realDeltaTimeSeconds)
        {
            if (IsPaused || realDeltaTimeSeconds <= 0f) return;

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

            // Advance hour by hour to fire ticks sequentially
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
    }
}
