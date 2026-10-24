using System;
using System.Collections.Generic;

namespace InkAndAsh.Data
{
    [Serializable]
    public class WeatherHazard
    {
        public string hazardName;
        public string weatherState;
        public float probability;
        public float spoilageModifier;
        public float staminaDrainModifier;
    }

    [Serializable]
    public class BiomeData
    {
        public string biomeId;
        public string name;
        public string spireName;
        public float spoilageMultiplier;
        public float staminaDrainMultiplier;
        public float travelSpeedModifier;
        public int hazardRating;
        public bool spireCalibrated;
        public List<WeatherHazard> weatherHazards;
        public string[] primaryResources;

        public BiomeData()
        {
            weatherHazards = new List<WeatherHazard>();
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(biomeId) || string.IsNullOrEmpty(spireName))
            {
                return false;
            }

            if (spoilageMultiplier < 0f || staminaDrainMultiplier <= 0f)
            {
                return false;
            }

            return true;
        }

        public void CalibrateSpire()
        {
            spireCalibrated = true;
        }
    }
}
