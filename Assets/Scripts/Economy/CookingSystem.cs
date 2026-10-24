using System;
using System.Collections.Generic;
using System.Linq;

namespace InkAndAsh.Economy
{
    [Serializable]
    public class CookingRecipe
    {
        public string RecipeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> RequiredIngredientIds { get; set; } = new List<string>();
        public string BuffId { get; set; } = string.Empty;
        public float BuffDurationHours { get; set; } = 24.0f;
        public string Description { get; set; } = string.Empty;

        public CookingRecipe() { }

        public CookingRecipe(string id, string name, List<string> ingredients, string buffId, string description = "")
        {
            RecipeId = id ?? string.Empty;
            Name = name ?? string.Empty;
            RequiredIngredientIds = ingredients ?? new List<string>();
            BuffId = buffId ?? string.Empty;
            BuffDurationHours = 24.0f;
            Description = description ?? string.Empty;
        }
    }

    public class ActiveBuff
    {
        public string BuffId { get; set; } = string.Empty;
        public float RemainingHours { get; set; }

        public bool IsExpired => RemainingHours <= 0f;
    }

    public class CookingSystem
    {
        private static CookingSystem _instance;
        public static CookingSystem Instance => _instance ??= new CookingSystem();

        private readonly Dictionary<string, CookingRecipe> _recipes = new Dictionary<string, CookingRecipe>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ActiveBuff> _activeBuffs = new List<ActiveBuff>();

        public IReadOnlyList<ActiveBuff> ActiveBuffs => _activeBuffs;

        public CookingSystem()
        {
            RegisterDefaultRecipes();
        }

        private void RegisterDefaultRecipes()
        {
            // Built-in recipes utilizing the 16 unified goods
            RegisterRecipe(new CookingRecipe(
                "recipe_barley_warmth",
                "Warm Barley Porridge",
                new List<string> { "item_golden_barley", "item_spring_water", "item_glacier_salt" },
                "Buff_Expedition_WarmthAndResolve",
                "Restores stamina and clears lingering fatigue."
            ));

            RegisterRecipe(new CookingRecipe(
                "recipe_hunter_stew",
                "Hearty Mountain Stew",
                new List<string> { "item_wild_game_meat", "item_golden_barley", "item_glacier_salt" },
                "Buff_Expedition_WarmthAndResolve",
                "Robust meal providing cold resistance and restorative vitality."
            ));

            RegisterRecipe(new CookingRecipe(
                "recipe_river_roast",
                "Savory River Fish",
                new List<string> { "item_fresh_river_fish", "item_aromatic_herbs", "item_glacier_salt" },
                "Buff_CTB_Speed_Bonus",
                "A fragrant roasted meal that heightens mental focus and reflexes."
            ));

            RegisterRecipe(new CookingRecipe(
                "recipe_kelp_broth",
                "Tidal Kelp Broth",
                new List<string> { "item_sun_dried_kelp", "item_spring_water", "item_glacier_salt" },
                "Buff_Cold_Resistance",
                "Mineral-rich broth providing deep endurance against freezing mountain winds."
            ));
        }

        public void RegisterRecipe(CookingRecipe recipe)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.RecipeId)) return;
            _recipes[recipe.RecipeId] = recipe;
        }

        public CookingRecipe GetRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;
            _recipes.TryGetValue(recipeId, out var recipe);
            return recipe;
        }

        public bool CanCook(CaravanInventory inventory, string recipeId)
        {
            if (inventory == null || string.IsNullOrEmpty(recipeId)) return false;
            if (!_recipes.TryGetValue(recipeId, out var recipe)) return false;

            var countsNeeded = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var ingId in recipe.RequiredIngredientIds)
            {
                countsNeeded[ingId] = countsNeeded.GetValueOrDefault(ingId, 0) + 1;
            }

            foreach (var kvp in countsNeeded)
            {
                int available = inventory.Items
                    .Where(i => string.Equals(i.ItemId, kvp.Key, StringComparison.OrdinalIgnoreCase) && !i.IsSpoiled)
                    .Sum(i => i.Quantity);

                if (available < kvp.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Cook(CaravanInventory inventory, string recipeId, out string buffId, out float buffDurationHours)
        {
            buffId = string.Empty;
            buffDurationHours = 0f;

            if (!CanCook(inventory, recipeId)) return false;

            var recipe = _recipes[recipeId];
            bool success = inventory.CookRecipe(recipe.RequiredIngredientIds, out buffId, out buffDurationHours);
            if (success)
            {
                buffId = recipe.BuffId;
                buffDurationHours = recipe.BuffDurationHours;
                ApplyBuff(buffId, buffDurationHours);
            }

            return success;
        }

        public void ApplyBuff(string buffId, float durationHours = 24.0f)
        {
            if (string.IsNullOrEmpty(buffId)) return;

            var existing = _activeBuffs.FirstOrDefault(b => string.Equals(b.BuffId, buffId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                // Refresh duration
                existing.RemainingHours = durationHours;
            }
            else
            {
                _activeBuffs.Add(new ActiveBuff
                {
                    BuffId = buffId,
                    RemainingHours = durationHours
                });
            }
        }

        public void TickBuffs(float inGameHoursElapsed)
        {
            if (inGameHoursElapsed <= 0f) return;

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                _activeBuffs[i].RemainingHours -= inGameHoursElapsed;
                if (_activeBuffs[i].IsExpired)
                {
                    _activeBuffs.RemoveAt(i);
                }
            }
        }

        public bool HasActiveBuff(string buffId)
        {
            if (string.IsNullOrEmpty(buffId)) return false;
            return _activeBuffs.Any(b => string.Equals(b.BuffId, buffId, StringComparison.OrdinalIgnoreCase) && !b.IsExpired);
        }

        public void ClearBuffs()
        {
            _activeBuffs.Clear();
        }
    }
}

#if STANDALONE_TEST
namespace InkAndAsh.StandaloneTests
{
    using NUnit.Framework;
    using InkAndAsh.Economy;

    [TestFixture]
    public class CookingSystemProductionTests
    {
        private CookingSystem _cookingSystem;
        private CaravanInventory _inventory;

        [SetUp]
        public void Setup()
        {
            _cookingSystem = new CookingSystem();
            _inventory = new CaravanInventory(StorageTier.FootPacks);
        }

        [Test]
        public void Cooking_CanCook_ReturnsFalseWhenMissingIngredients()
        {
            // Warm barley porridge needs: item_golden_barley, item_spring_water, item_glacier_salt
            _inventory.AddItem("item_golden_barley", "Barley", 1.0f, 1);
            Assert.That(_cookingSystem.CanCook(_inventory, "recipe_barley_warmth"), Is.False);
        }

        [Test]
        public void Cooking_CookRecipe_ConsumesIngredientsAndGrantsBuff()
        {
            _inventory.AddItem("item_golden_barley", "Barley", 1.0f, 1);
            _inventory.AddItem("item_spring_water", "Water", 1.0f, 1);
            _inventory.AddItem("item_glacier_salt", "Salt", 1.0f, 1);

            Assert.That(_cookingSystem.CanCook(_inventory, "recipe_barley_warmth"), Is.True);

            bool cooked = _cookingSystem.Cook(_inventory, "recipe_barley_warmth", out var buffId, out var duration);
            Assert.That(cooked, Is.True);
            Assert.That(buffId, Is.EqualTo("Buff_Expedition_WarmthAndResolve"));
            Assert.That(duration, Is.EqualTo(24.0f));
            Assert.That(_cookingSystem.HasActiveBuff("Buff_Expedition_WarmthAndResolve"), Is.True);

            // Verify ingredients were consumed
            Assert.That(_inventory.UsedSlots, Is.EqualTo(0));
        }

        [Test]
        public void Cooking_BuffExpires_After24InGameHours()
        {
            _cookingSystem.ApplyBuff("Buff_Test", 24.0f);
            Assert.That(_cookingSystem.HasActiveBuff("Buff_Test"), Is.True);

            _cookingSystem.TickBuffs(23.9f);
            Assert.That(_cookingSystem.HasActiveBuff("Buff_Test"), Is.True);

            _cookingSystem.TickBuffs(0.2f);
            Assert.That(_cookingSystem.HasActiveBuff("Buff_Test"), Is.False);
        }
    }
}
#endif
