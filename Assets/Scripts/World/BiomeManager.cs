using System;
using System.Collections.Generic;
using InkAndAsh.Data;

namespace InkAndAsh.World
{
    public class BiomeManager
    {
        private static BiomeManager _instance;
        public static BiomeManager Instance => _instance ??= new BiomeManager();

        private readonly Dictionary<string, bool> _calibratedSpires = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public WeatherSystem Weather { get; } = new WeatherSystem();
        public string ActiveBiomeId => Weather.CurrentBiomeId;

        public event Action<string> OnBiomeChanged;
        public event Action<string, string> OnSpireCalibrated;

        public BiomeManager()
        {
            InitializeBiomes();
        }

        private void InitializeBiomes()
        {
            string[] biomes = new[]
            {
                "biome_grasslands", "biome_coast", "biome_forest", "biome_mountains",
                "biome_swamp", "biome_desert", "biome_jungle"
            };

            foreach (var b in biomes)
            {
                _calibratedSpires[b] = false;
            }
        }

        public void TransitionToBiome(string biomeId)
        {
            if (string.IsNullOrEmpty(biomeId)) return;

            Weather.SetBiome(biomeId);

            // Restore calibration state if already calibrated
            if (IsSpireCalibrated(biomeId))
            {
                Weather.CalibrateSpire();
            }

            OnBiomeChanged?.Invoke(biomeId);
        }

        public bool IsSpireCalibrated(string biomeId)
        {
            if (string.IsNullOrEmpty(biomeId)) return false;
            _calibratedSpires.TryGetValue(biomeId, out bool calibrated);
            return calibrated;
        }

        public void CalibrateSpire(string biomeId)
        {
            if (string.IsNullOrEmpty(biomeId)) return;

            _calibratedSpires[biomeId] = true;
            if (string.Equals(ActiveBiomeId, biomeId, StringComparison.OrdinalIgnoreCase))
            {
                Weather.CalibrateSpire();
            }

            var biomeData = DataManager.Instance.GetBiome(biomeId);
            string spireName = biomeData?.spireName ?? $"{biomeId}_spire";

            OnSpireCalibrated?.Invoke(biomeId, spireName);
        }
    }
}

#if STANDALONE_TEST
namespace InkAndAsh.StandaloneTests
{
    using NUnit.Framework;
    using InkAndAsh.World;

    [TestFixture]
    public class BiomeManagerProductionTests
    {
        private BiomeManager _biomeManager;

        [SetUp]
        public void Setup()
        {
            _biomeManager = new BiomeManager();
        }

        [Test]
        public void BiomeManager_Tracks7Biomes()
        {
            Assert.That(_biomeManager.IsSpireCalibrated("biome_grasslands"), Is.False);
            Assert.That(_biomeManager.IsSpireCalibrated("biome_mountains"), Is.False);
            Assert.That(_biomeManager.IsSpireCalibrated("biome_desert"), Is.False);
        }

        [Test]
        public void BiomeManager_TransitionAndCalibrate_UpdatesState()
        {
            _biomeManager.TransitionToBiome("biome_mountains");
            Assert.That(_biomeManager.ActiveBiomeId, Is.EqualTo("biome_mountains"));
            Assert.That(_biomeManager.Weather.CurrentWeather, Is.EqualTo(WeatherType.Freezing));

            _biomeManager.CalibrateSpire("biome_mountains");
            Assert.That(_biomeManager.IsSpireCalibrated("biome_mountains"), Is.True);
            Assert.That(_biomeManager.Weather.SpireCalibrated, Is.True);
            Assert.That(_biomeManager.Weather.CurrentWeather, Is.EqualTo(WeatherType.Clear));
        }
    }
}
#endif
