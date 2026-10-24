using System;
using System.Collections.Generic;
using System.Linq;
using InkAndAsh.Data;

namespace InkAndAsh.Economy
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
        private readonly List<CaravanItemEntry> _items = new List<CaravanItemEntry>();
        public StorageTier Tier { get; }
        public bool IsHumidWeather { get; private set; }

        public event Action<string, bool> OnItemDecayed;

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

        public void SetHumidWeather(bool isHumid)
        {
            SetHumidityWeather(isHumid);
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
                Name = name ?? itemId,
                UnitBaseWeightKg = unitWeightKg,
                Quantity = quantity,
                Durability = Math.Clamp(durability, 0f, 100f),
                IsPerishable = isPerishable,
                IsMoistureAbsorbent = isAbsorbent
            });

            return true;
        }

        public bool AddItem(string itemId, int quantity = 1, float durability = 100f)
        {
            if (string.IsNullOrEmpty(itemId)) return false;

            var dataItem = DataManager.Instance.GetItem(itemId);
            if (dataItem != null)
            {
                bool isAbsorbent = string.Equals(itemId, "item_golden_barley", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(itemId, "item_coarse_fiber", StringComparison.OrdinalIgnoreCase);

                return AddItem(dataItem.itemId, dataItem.name, dataItem.baseWeightKg, quantity, durability, dataItem.isPerishable, isAbsorbent);
            }

            // Fallback default
            return AddItem(itemId, itemId, 1.0f, quantity, durability, true, false);
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;

            int totalAvailable = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    totalAvailable += _items[i].Quantity;
                }
            }

            if (totalAvailable < quantity) return false;

            int remainingToDeduct = quantity;
            for (int i = 0; i < _items.Count && remainingToDeduct > 0; )
            {
                var item = _items[i];
                if (string.Equals(item.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    int deduct = Math.Min(item.Quantity, remainingToDeduct);
                    item.Quantity -= deduct;
                    remainingToDeduct -= deduct;
                    if (item.Quantity <= 0)
                    {
                        _items.RemoveAt(i);
                        continue;
                    }
                }
                i++;
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
                for (int i = 0; i < _items.Count && remainingToDeduct > 0; )
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
                            continue;
                        }
                    }
                    i++;
                }
            }

            buffId = "Buff_Expedition_WarmthAndResolve";
            buffDurationHours = 24.0f; // Exactly 24 in-game hours
            return true;
        }
    }
}

#if STANDALONE_TEST
namespace InkAndAsh.StandaloneTests
{
    using NUnit.Framework;
    using InkAndAsh.Economy;

    [TestFixture]
    public class CaravanInventoryProductionTests
    {
        private CaravanInventory _footPacks;
        private CaravanInventory _carriage;

        [SetUp]
        public void Setup()
        {
            _footPacks = new CaravanInventory(StorageTier.FootPacks);
            _carriage = new CaravanInventory(StorageTier.HorseCarriage);
        }

        [Test]
        public void Caravan_CapacityLimits_MatchTiers()
        {
            Assert.That(_footPacks.MaxSlots, Is.EqualTo(30));
            Assert.That(_footPacks.MaxWeightKg, Is.EqualTo(50.0f));
            Assert.That(_footPacks.DecayReductionMultiplier, Is.EqualTo(1.0f));

            Assert.That(_carriage.MaxSlots, Is.EqualTo(120));
            Assert.That(_carriage.MaxWeightKg, Is.EqualTo(300.0f));
            Assert.That(_carriage.DecayReductionMultiplier, Is.EqualTo(0.40f));
        }

        [Test]
        public void Caravan_DurabilityDecay_InClearWeather()
        {
            _footPacks.AddItem("item_meat", "Game Meat", 1.0f, 1, durability: 100f, isPerishable: true);

            // 10 real minutes elapsed
            _footPacks.TickDecay(10.0f, 1.0f);

            var item = _footPacks.Items[0];
            Assert.That(item.Durability, Is.EqualTo(90.0f));
        }

        [Test]
        public void Caravan_Carriage_ReducesDecayBy60Percent()
        {
            _carriage.AddItem("item_meat", "Game Meat", 1.0f, 1, durability: 100f, isPerishable: true);

            // 10 real minutes elapsed in clear weather
            // Decay = 10 * 1.0% * 1.0 * 0.40 = 4.0% lost
            _carriage.TickDecay(10.0f, 1.0f);

            var item = _carriage.Items[0];
            Assert.That(item.Durability, Is.EqualTo(96.0f));
        }

        [Test]
        public void Caravan_MoistureWeight_Applies50PercentPenalty()
        {
            _footPacks.AddItem("item_golden_barley", "Golden Barley", 1.0f, 10, isAbsorbent: true);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(10.0f));

            _footPacks.SetHumidityWeather(true);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(15.0f));

            _footPacks.SetHumidityWeather(false);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(10.0f));
        }

        [Test]
        public void Caravan_Spoilage_FiresOnItemDecayedEvent()
        {
            string spoiledId = null;
            bool wasSpoiled = false;
            _footPacks.OnItemDecayed += (id, s) =>
            {
                spoiledId = id;
                wasSpoiled = s;
            };

            _footPacks.AddItem("item_canopy_bloom", "Canopy Bloom", 0.5f, 1, durability: 5f, isPerishable: true);

            // 10 mins elapsed in 1.0x decay -> 10% lost -> clamps to 0%
            _footPacks.TickDecay(10.0f, 1.0f);

            Assert.That(_footPacks.Items[0].Durability, Is.EqualTo(0f));
            Assert.That(_footPacks.Items[0].IsSpoiled, Is.True);
            Assert.That(spoiledId, Is.EqualTo("item_canopy_bloom"));
            Assert.That(wasSpoiled, Is.True);
        }
    }
}
#endif
