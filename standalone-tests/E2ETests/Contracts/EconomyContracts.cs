#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace InkAndAsh.StandaloneTests.E2ETests.Contracts
{
    public enum StorageTier
    {
        FootPacks,
        HorseCarriage
    }

    public class CaravanItemEntry
    {
        public string ItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public float UnitBaseWeightKg { get; set; }
        public int Quantity { get; set; }
        public float Durability { get; set; } = 100f; // 0.0 to 100.0%
        public bool IsPerishable { get; set; }
        public bool IsMoistureAbsorbent { get; set; }
        public bool IsSpoiled => Durability <= 0f && IsPerishable;

        public float GetTotalWeight(bool isHumidWeather)
        {
            float unitWeight = UnitBaseWeightKg;
            if (isHumidWeather && IsMoistureAbsorbent)
            {
                unitWeight *= 1.5f; // +50% moisture weight penalty
            }
            return unitWeight * Quantity;
        }
    }

    public class CaravanInventory
    {
        private readonly List<CaravanItemEntry> _items = new();
        public StorageTier Tier { get; }
        public bool IsHumidWeather { get; private set; }

        public event Action<string, bool>? OnItemDecayed;

        public int MaxSlots => Tier == StorageTier.FootPacks ? 30 : 120;
        public float MaxWeightKg => Tier == StorageTier.FootPacks ? 50f : 300f;
        public float DecayReductionMultiplier => Tier == StorageTier.HorseCarriage ? 0.40f : 1.0f; // 60% reduction

        public int UsedSlots => _items.Count;
        public float CurrentWeightKg => _items.Sum(i => i.GetTotalWeight(IsHumidWeather));
        public IReadOnlyList<CaravanItemEntry> Items => _items;

        public CaravanInventory(StorageTier tier = StorageTier.FootPacks)
        {
            Tier = tier;
        }

        public void SetHumidityWeather(bool isHumid)
        {
            IsHumidWeather = isHumid;
        }

        public bool AddItem(string itemId, string name, float unitWeightKg, int quantity = 1, float durability = 100f, bool isPerishable = true, bool isAbsorbent = false)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;

            float addedWeight = unitWeightKg * quantity * (IsHumidWeather && isAbsorbent ? 1.5f : 1.0f);
            if (CurrentWeightKg + addedWeight > MaxWeightKg + 0.0001f)
            {
                return false; // Weight cap exceeded
            }

            var existing = _items.FirstOrDefault(i => i.ItemId == itemId && !i.IsPerishable);
            if (existing != null)
            {
                existing.Quantity += quantity;
                return true;
            }

            if (_items.Count >= MaxSlots)
            {
                return false; // Slot cap exceeded
            }

            _items.Add(new CaravanItemEntry
            {
                ItemId = itemId,
                Name = name,
                UnitBaseWeightKg = unitWeightKg,
                Quantity = quantity,
                Durability = Math.Clamp(durability, 0f, 100f),
                IsPerishable = isPerishable,
                IsMoistureAbsorbent = isAbsorbent
            });

            return true;
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var item = _items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null || item.Quantity < quantity) return false;

            item.Quantity -= quantity;
            if (item.Quantity <= 0)
            {
                _items.Remove(item);
            }
            return true;
        }

        public void TickDecay(float realMinutesElapsed, float biomeSpoilageMultiplier = 1.0f)
        {
            if (realMinutesElapsed <= 0f) return;

            float decayPercentage = realMinutesElapsed * 1.0f * biomeSpoilageMultiplier * DecayReductionMultiplier;

            foreach (var item in _items)
            {
                if (!item.IsPerishable) continue;

                bool wasSpoiledBefore = item.IsSpoiled;
                item.Durability = Math.Max(0f, item.Durability - decayPercentage);

                if (!wasSpoiledBefore && item.IsSpoiled)
                {
                    OnItemDecayed?.Invoke(item.ItemId, true);
                }
            }
        }

        public bool CookRecipe(List<string> requiredItemIds, out string buffId, out float buffDurationHours)
        {
            buffId = string.Empty;
            buffDurationHours = 0f;

            if (requiredItemIds == null || requiredItemIds.Count == 0) return false;

            // Check availability and non-spoiled status
            var countsNeeded = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in requiredItemIds)
            {
                countsNeeded[id] = countsNeeded.GetValueOrDefault(id, 0) + 1;
            }

            foreach (var kvp in countsNeeded)
            {
                var available = _items.Where(i => string.Equals(i.ItemId, kvp.Key, StringComparison.OrdinalIgnoreCase) && !i.IsSpoiled)
                                      .Sum(i => i.Quantity);
                if (available < kvp.Value)
                {
                    return false; // Missing or spoiled ingredient
                }
            }

            // Consume ingredients
            foreach (var kvp in countsNeeded)
            {
                int remainingToDeduct = kvp.Value;
                for (int i = _items.Count - 1; i >= 0 && remainingToDeduct > 0; i--)
                {
                    var item = _items[i];
                    if (string.Equals(item.ItemId, kvp.Key, StringComparison.OrdinalIgnoreCase) && !item.IsSpoiled)
                    {
                        int deduct = Math.Min(item.Quantity, remainingToDeduct);
                        item.Quantity -= deduct;
                        remainingToDeduct -= deduct;
                        if (item.Quantity <= 0)
                        {
                            _items.RemoveAt(i);
                        }
                    }
                }
            }

            buffId = "Buff_Expedition_WarmthAndResolve";
            buffDurationHours = 24.0f; // Exactly 24 in-game hours
            return true;
        }
    }
}
