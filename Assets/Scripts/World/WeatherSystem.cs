using System;

namespace InkAndAsh.World
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
        private static WeatherSystem _instance;
        public static WeatherSystem Instance => _instance ??= new WeatherSystem();

        public string CurrentBiomeId { get; private set; } = "biome_grasslands";
        public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;
        public bool SpireCalibrated { get; private set; }

        public float SpoilageMultiplier { get; private set; } = 1.0f;
        public float StaminaMultiplier { get; private set; } = 1.0f;
        public float CombatDelayMultiplier { get; private set; } = 1.0f;

        public event Action<string, string> OnSpireCalibrated;
        public event Action<WeatherType> OnWeatherChanged;

        public WeatherSystem()
        {
            UpdateMultipliers();
        }

        public void SetBiome(string biomeId)
        {
            CurrentBiomeId = biomeId ?? "biome_grasslands";
            SpireCalibrated = false;

            switch (CurrentBiomeId.ToLowerInvariant())
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
                case "biome_jungle":
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
            OnWeatherChanged?.Invoke(CurrentWeather);
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
}

#if STANDALONE_TEST
namespace InkAndAsh.StandaloneTests
{
    using NUnit.Framework;
    using InkAndAsh.World;

    [TestFixture]
    public class WeatherSystemProductionTests
    {
        private WeatherSystem _weather;

        [SetUp]
        public void Setup()
        {
            _weather = new WeatherSystem();
        }

        [Test]
        public void Weather_Mountains_FreezingMultipliers()
        {
            _weather.SetBiome("biome_mountains");

            Assert.That(_weather.CurrentWeather, Is.EqualTo(WeatherType.Freezing));
            Assert.That(_weather.SpoilageMultiplier, Is.EqualTo(0.2f));
            Assert.That(_weather.StaminaMultiplier, Is.EqualTo(2.0f));
            Assert.That(_weather.CombatDelayMultiplier, Is.EqualTo(1.25f));
        }

        [Test]
        public void Weather_Desert_HeatMultipliers()
        {
            _weather.SetBiome("biome_desert");

            Assert.That(_weather.CurrentWeather, Is.EqualTo(WeatherType.Heat));
            Assert.That(_weather.SpoilageMultiplier, Is.EqualTo(1.5f));
            Assert.That(_weather.StaminaMultiplier, Is.EqualTo(1.5f));
            Assert.That(_weather.CombatDelayMultiplier, Is.EqualTo(1.1f));
        }

        [Test]
        public void Weather_CalibrateSpire_NormalizesAllMultipliers()
        {
            _weather.SetBiome("biome_mountains");
            Assert.That(_weather.StaminaMultiplier, Is.EqualTo(2.0f));

            string calibratedBiome = null;
            string calibratedSpire = null;
            _weather.OnSpireCalibrated += (b, s) =>
            {
                calibratedBiome = b;
                calibratedSpire = s;
            };

            _weather.CalibrateSpire();

            Assert.That(_weather.SpireCalibrated, Is.True);
            Assert.That(_weather.CurrentWeather, Is.EqualTo(WeatherType.Clear));
            Assert.That(_weather.SpoilageMultiplier, Is.EqualTo(1.0f));
            Assert.That(_weather.StaminaMultiplier, Is.EqualTo(1.0f));
            Assert.That(_weather.CombatDelayMultiplier, Is.EqualTo(1.0f));
            Assert.That(calibratedBiome, Is.EqualTo("biome_mountains"));
            Assert.That(calibratedSpire, Is.EqualTo("biome_mountains_spire"));
        }
    }
}
#endif
