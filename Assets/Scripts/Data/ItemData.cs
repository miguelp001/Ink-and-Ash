using System;
using System.Collections.Generic;

namespace InkAndAsh.Data
{
    [Serializable]
    public class ItemData
    {
        public string itemId;
        public string name;
        public string primaryBiome;
        public float baseWeightKg;
        public float decayRatePerMinute;
        public bool isPerishable;
        public string[] usageCategories;
        public int basePrice;
        public Dictionary<string, float> biomePriceModifiers;
        public string description;
        public string storageNotes;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (baseWeightKg <= 0f)
            {
                return false;
            }

            if (decayRatePerMinute < 0f)
            {
                return false;
            }

            if (usageCategories == null || usageCategories.Length == 0)
            {
                return false;
            }

            return true;
        }

        public float GetPriceInBiome(string biomeName)
        {
            if (biomePriceModifiers != null && biomePriceModifiers.TryGetValue(biomeName, out float modifier))
            {
                return basePrice * modifier;
            }

            return basePrice;
        }
    }
}
