using System;
using NUnit.Framework;
using InkAndAsh.World;

namespace InkAndAsh.Tests.EditMode
{
    [TestFixture]
    public class WeatherSystemTests
    {
        private WeatherSystem _weatherSystem;

        [SetUp]
        public void SetUp()
        {
            _weatherSystem = new WeatherSystem();
        }

        #region 1. Biome Climate Defaults

        [TestCase("biome_grasslands", WeatherType.Clear)]
        [TestCase("biome_coast", WeatherType.Rain)]
        [TestCase("biome_forest", WeatherType.Clear)]
        [TestCase("biome_mountains", WeatherType.Freezing)]
        [TestCase("biome_swamp", WeatherType.Rain)]
        [TestCase("biome_desert", WeatherType.Heat)]
        [TestCase("biome_jungle", WeatherType.Rain)]
        public void BiomeSelection_SetsCorrectDefaultWeather(string biomeId, WeatherType expectedWeather)
        {
            _weatherSystem.SetBiome(biomeId);

            Assert.That(_weatherSystem.CurrentBiomeId, Is.EqualTo(biomeId));
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(expectedWeather));
            Assert.That(_weatherSystem.SpireCalibrated, Is.False);
        }

        [Test]
        public void BiomeSelection_NullOrUnknownBiome_DefaultsToClearGrasslands()
        {
            _weatherSystem.SetBiome(null);
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Clear));

            _weatherSystem.SetBiome("unknown_biome_xyz");
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Clear));
        }

        #endregion

        #region 2. Multipliers by Weather Type

        [Test]
        public void WeatherMultipliers_ClearWeather_NormalMultipliers()
        {
            _weatherSystem.SetWeather(WeatherType.Clear);

            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.0f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.0f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.0f));
        }

        [Test]
        public void WeatherMultipliers_HeatWeather_HighSpoilageAndStamina()
        {
            _weatherSystem.SetWeather(WeatherType.Heat);

            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.5f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.5f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.1f));
        }

        [Test]
        public void WeatherMultipliers_FreezingWeather_LowSpoilageExtremeStaminaAndDelay()
        {
            _weatherSystem.SetWeather(WeatherType.Freezing);

            // Freezing preserves perishables (0.2x spoilage) but exhausts travelers (2.0x stamina) and slows actions (1.25x delay)
            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(0.2f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(2.0f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.25f));
        }

        [Test]
        public void WeatherMultipliers_RainWeather_HighSpoilageElevatedStamina()
        {
            _weatherSystem.SetWeather(WeatherType.Rain);

            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.5f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.25f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.0f));
        }

        [Test]
        public void WeatherMultipliers_AshfallWeather_ModerateSpoilageAndHazard()
        {
            _weatherSystem.SetWeather(WeatherType.Ashfall);

            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.2f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.4f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.15f));
        }

        #endregion

        #region 3. Dynamic Weather Transitions & Events

        [Test]
        public void DynamicWeatherTransitions_DispatchesEventAndUpdatesMultipliers()
        {
            WeatherType recordedWeather = WeatherType.Clear;
            int eventCallCount = 0;

            _weatherSystem.OnWeatherChanged += (weather) =>
            {
                recordedWeather = weather;
                eventCallCount++;
            };

            _weatherSystem.SetWeather(WeatherType.Rain);
            Assert.That(eventCallCount, Is.EqualTo(1));
            Assert.That(recordedWeather, Is.EqualTo(WeatherType.Rain));
            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.5f));

            _weatherSystem.SetWeather(WeatherType.Heat);
            Assert.That(eventCallCount, Is.EqualTo(2));
            Assert.That(recordedWeather, Is.EqualTo(WeatherType.Heat));
            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.5f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.1f));
        }

        #endregion

        #region 4. Spire Calibration

        [Test]
        public void SpireCalibration_NormalizesAllMultipliersAndResetsWeatherToClear()
        {
            _weatherSystem.SetBiome("biome_mountains");
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Freezing));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.25f));

            bool eventFired = false;
            string calibratedBiome = null;
            string calibratedSpire = null;

            _weatherSystem.OnSpireCalibrated += (biome, spire) =>
            {
                eventFired = true;
                calibratedBiome = biome;
                calibratedSpire = spire;
            };

            _weatherSystem.CalibrateSpire();

            Assert.That(_weatherSystem.SpireCalibrated, Is.True);
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Clear));
            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.0f));
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.0f));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.0f));

            Assert.That(eventFired, Is.True);
            Assert.That(calibratedBiome, Is.EqualTo("biome_mountains"));
            Assert.That(calibratedSpire, Is.EqualTo("biome_mountains_spire"));
        }

        [Test]
        public void SpireCalibration_LocksMultipliersAgainstSubsequentWeatherChanges()
        {
            _weatherSystem.SetBiome("biome_desert");
            _weatherSystem.CalibrateSpire();

            Assert.That(_weatherSystem.SpireCalibrated, Is.True);

            // Attempt to force weather back to Heat while Spire is calibrated
            _weatherSystem.SetWeather(WeatherType.Heat);

            Assert.That(_weatherSystem.SpoilageMultiplier, Is.EqualTo(1.0f), "Calibrated Spire must keep spoilage locked at 1.0x.");
            Assert.That(_weatherSystem.StaminaMultiplier, Is.EqualTo(1.0f), "Calibrated Spire must keep stamina locked at 1.0x.");
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.0f), "Calibrated Spire must keep delay locked at 1.0x.");
        }

        [Test]
        public void SetBiome_TransitionsToNewBiomeAndResetsSpireCalibration()
        {
            _weatherSystem.SetBiome("biome_desert");
            _weatherSystem.CalibrateSpire();
            Assert.That(_weatherSystem.SpireCalibrated, Is.True);

            // Travel to new uncalibrated biome
            _weatherSystem.SetBiome("biome_mountains");

            Assert.That(_weatherSystem.CurrentBiomeId, Is.EqualTo("biome_mountains"));
            Assert.That(_weatherSystem.SpireCalibrated, Is.False, "New biome must start uncalibrated.");
            Assert.That(_weatherSystem.CurrentWeather, Is.EqualTo(WeatherType.Freezing));
            Assert.That(_weatherSystem.CombatDelayMultiplier, Is.EqualTo(1.25f));
        }

        #endregion
    }
}
