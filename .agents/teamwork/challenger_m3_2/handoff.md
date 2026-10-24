# Milestone 3 Challenger 2 Empirical Test & Hardening Report

**Agent**: teamwork_preview_challenger (Challenger 2, Milestone 3)  
**Parent**: fc826616-6237-4548-b6ac-f0d5352bcd07 (Project Orchestrator)  
**Date**: 2026-10-24T20:57:30Z  
**Verdict**: **REQUEST_CHANGES**

---

## 1. Observation

### 1.1 Empirical Test Execution Command and Results
Executed:
```powershell
dotnet test standalone-tests/InkAndAsh.StandaloneTests.csproj --filter "FullyQualifiedName~ChallengerM3EmpiricalEdgeCaseTests" --verbosity normal
```

Verbatim console output:
```
Test run for D:\Unity Projects\Ink and Ash\standalone-tests\bin\Debug\net8.0\InkAndAsh.StandaloneTests.dll (.NETCoreApp,Version=v8.0)
NUnit Adapter 4.6.0.0: Test execution started
Running selected tests in D:\Unity Projects\Ink and Ash\standalone-tests\bin\Debug\net8.0\InkAndAsh.StandaloneTests.dll
   NUnit3TestExecutor discovered 17 of 17 NUnit test cases using Current Discovery mode, Non-Explicit run
  Passed Agility_ExtremeNegative_EffectiveAgilityClampsToOne [7 ms]
  Passed Agility_Negative_Instance_ClampsToOne_NoNegativeDelay [1 ms]
  Passed Agility_Negative_StaticOverload_ClampsToOne [< 1 ms]
  Passed Agility_Zero_Instance_ClampsToOne_NoDivideByZero [< 1 ms]
  Passed Agility_Zero_StaticOverload_ClampsToOne [< 1 ms]
  Passed Agility_ZeroWithInjuryDebuff_ClampsToOne [< 1 ms]
  Passed MultiStack_Deduction_DoesNotAffectOtherItemTypes [7 ms]
  Passed MultiStack_Deduction_ExactTotalQuantity_RemovesAllMatchingStacks [< 1 ms]
  Passed MultiStack_Deduction_InsufficientQuantity_DoesNotMutateAnyStack [< 1 ms]
Remaining stack name: 'Golden Barley Stack 1', Quantity: 3, Durability: 90

  Failed MultiStack_Deduction_TwoStacksOf5_Removing7_VerifiesDeductionOrder [23 ms]
  Error Message:
     DEFECT FOUND: RemoveItem iterates backwards (_items.Count - 1 down to 0), deducting 5 from stack 2 and 2 from stack 1 (LIFO), instead of deducting 5 from stack 1 and 2 from stack 2 (FIFO).
Assert.That(remainingStack.Name, Is.EqualTo("Golden Barley Stack 2"))
  String lengths are both 21. Strings differ at index 20.
  Expected: "Golden Barley Stack 2"
  But was:  "Golden Barley Stack 1"
  -------------------------------^

  Stack Trace:
     at InkAndAsh.StandaloneTests.ChallengerM3EmpiricalEdgeCaseTests.MultiStack_Deduction_TwoStacksOf5_Removing7_VerifiesDeductionOrder() in D:\Unity Projects\Ink and Ash\standalone-tests\ChallengerM3EmpiricalEdgeCaseTests.cs:line 66

  Passed MultiStack_Deduction_WeightUpdatesAccuratelyUnderHumidWeather [< 1 ms]
  Passed NonPositiveQuantity_EmptyInventory_ReturnsFalse [< 1 ms]
  Passed NonPositiveQuantity_NullOrEmptyItemId_ReturnsFalse [< 1 ms]
  Passed NonPositiveQuantity_RemoveItem_NegativeFive_ReturnsFalse [< 1 ms]
  Passed NonPositiveQuantity_RemoveItem_Zero_ReturnsFalse [< 1 ms]
  Passed TestIsolation_CaravanInventory_InstancesDoNotShareItemsList [< 1 ms]
  Passed TestIsolation_CtbCombatEngine_InstancesDoNotShareTimelineQueue [< 1 ms]

Test Run Failed.
Total tests: 17
     Passed: 16
     Failed: 1
 Total time: 0.5763 Seconds
```

### 1.2 Full Test Suite Execution Command and Results
Executed:
```powershell
dotnet test standalone-tests/InkAndAsh.StandaloneTests.csproj --verbosity minimal
```

Verbatim console output:
```
Failed!  - Failed:     1, Passed:   305, Skipped:     0, Total:   306, Duration: 660 ms - InkAndAsh.StandaloneTests.dll (net8.0)
```

### 1.3 Code Inspection of Identified Defect
File: `Assets/Scripts/Economy/CaravanInventory.cs`  
Lines 135-150:
```csharp
135:            int remainingToDeduct = quantity;
136:            for (int i = _items.Count - 1; i >= 0 && remainingToDeduct > 0; i--)
137:            {
138:                var item = _items[i];
139:                if (string.Equals(item.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
140:                {
141:                    int deduct = Math.Min(item.Quantity, remainingToDeduct);
142:                    item.Quantity -= deduct;
143:                    remainingToDeduct -= deduct;
144:                    if (item.Quantity <= 0)
145:                    {
146:                        _items.RemoveAt(i);
147:                    }
148:                }
149:            }
150:
151:            return true;
```
Lines 200-216 (`CookRecipe` has the identical issue):
```csharp
200:            foreach (var kvp in countsNeeded)
201:            {
202:                int remainingToDeduct = kvp.Value;
203:                for (int i = _items.Count - 1; i >= 0 && remainingToDeduct > 0; i--)
204:                {
205:                    var item = _items[i];
206:                    if (string.Equals(item.ItemId, kvp.Key, StringComparison.OrdinalIgnoreCase) && !item.IsSpoiled)
207:                    {
208:                        int deduct = Math.Min(item.Quantity, remainingToDeduct);
209:                        item.Quantity -= deduct;
210:                        remainingToDeduct -= deduct;
211:                        if (item.Quantity <= 0)
212:                        {
213:                            _items.RemoveAt(i);
214:                        }
215:                    }
216:                }
217:            }
```

---

## 2. Logic Chain

1. **Requirement 1 Verification (Multi-Stack Deduction Order)**:
   - The dispatch prompt explicitly mandated:
     > "1. Test multi-stack inventory deduction in `CaravanInventory`: if player has two stacks of 5 Golden Barley, removing 7 items should deduct 5 from stack 1 and 2 from stack 2."
   - Under First-In-First-Out (FIFO) deduction, Stack 1 (index 0, added first, older/decayed) must be consumed first (5 deducted, remaining quantity = 0, slot removed), and then 2 items must be deducted from Stack 2 (index 1, added second, newer/fresher), leaving 3 items in Stack 2 (durability 100%).
   - In `Assets/Scripts/Economy/CaravanInventory.cs` lines 136-149, the deduction loop was implemented as:
     `for (int i = _items.Count - 1; i >= 0 && remainingToDeduct > 0; i--)`
   - This reverse loop accesses index 1 (Stack 2) first. It deducts `Math.Min(5, 7) = 5` from Stack 2, depleting and deleting Stack 2. It then accesses index 0 (Stack 1) and deducts the remaining 2 items, leaving 3 items in Stack 1 (durability 90%).
   - As directly recorded in `ChallengerM3EmpiricalEdgeCaseTests.cs`:
     `Expected: "Golden Barley Stack 2"`
     `But was: "Golden Barley Stack 1"`
   - Result: The code implemented LIFO (Last-In-First-Out) instead of FIFO (First-In-First-Out). In a perishable economy where older items decay toward 0%, consuming fresh items from the newest stack while leaving older items to spoil directly corrupts the game's survival and trade logistics.
   - The identical reverse iteration defect exists in `CaravanInventory.CookRecipe` (line 203), which consumes the newest/freshest ingredients rather than the oldest available ingredients.

2. **Requirement 2 Verification (Non-Positive Quantity Removals)**:
   - The dispatch prompt mandated:
     > "2. Test non-positive quantity removals (`RemoveItem("barley", 0)` and `RemoveItem("barley", -5)`). Both must return false."
   - Tested in `NonPositiveQuantity_RemoveItem_Zero_ReturnsFalse` and `NonPositiveQuantity_RemoveItem_NegativeFive_ReturnsFalse`.
   - In `CaravanInventory.cs` line 122:
     `if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;`
   - Both returned `false`. Inventory quantity, slots, and weights were unchanged. **PASSED**.

3. **Requirement 3 Verification (Agility Clamping in CTB)**:
   - The dispatch prompt mandated:
     > "3. Test agility <= 0 in `CtbCombatEngine.CalculateTurnDelay`: verify it clamps to 1.0f and does not produce DivideByZero or negative delays."
   - Tested with BaseAgility = 0f, -50f, -999999f, and with static overload `CtbCombatEngine.CalculateTurnDelay(0f, 100)` and `(-25f, 100)`.
   - Also tested with `InjuryDebuff`: BaseAgility = 0f with debuff yields delay 115.0f (cost 115 / clamped agility 1.0).
   - In all cases, delay clamped to 1.0f denominator, producing positive non-infinite numbers (100.0f, 115.0f) without `DivideByZeroException`, `NaN`, or `Infinity`. **PASSED**.

4. **Requirement 4 Verification (Standalone Test Execution)**:
   - Executed `dotnet test standalone-tests/InkAndAsh.StandaloneTests.csproj`.
   - 305 tests passed, 1 test failed (`MultiStack_Deduction_TwoStacksOf5_Removing7_VerifiesDeductionOrder`). **FAILED**.

---

## 3. Caveats

- No caveats. The behavior was empirically proven with an executable NUnit test fixture compiled directly into the project's standalone test runner.

---

## 4. Conclusion

A definitive verdict of **REQUEST_CHANGES** is issued.

While defensive hardening for non-positive removal quantities (`quantity <= 0`) and CTB turn delay agility clamping (`Math.Max(1.0f, agility)`) were correctly implemented and verified, `CaravanInventory.RemoveItem` (and `CaravanInventory.CookRecipe`) currently iterates backwards through `_items` (`_items.Count - 1` down to `0`), causing LIFO deduction (deducting 5 from stack 2 and 2 from stack 1) rather than the required FIFO deduction (deducting 5 from stack 1 and 2 from stack 2).

### Actionable Fix Required:
In `Assets/Scripts/Economy/CaravanInventory.cs`:
1. In `RemoveItem` (lines 135-150), replace the reverse loop with forward index traversal:
```csharp
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
```
2. In `CookRecipe` (lines 201-216), apply the same forward index traversal so cooking consumes the oldest available ingredients first:
```csharp
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
```

---

## 5. Verification Method

To independently reproduce the empirical failure and verify the subsequent fix:

```powershell
dotnet test "d:\Unity Projects\Ink and Ash\standalone-tests\InkAndAsh.StandaloneTests.csproj" --filter "FullyQualifiedName~ChallengerM3EmpiricalEdgeCaseTests" --verbosity normal
```

Expected output prior to fix:
`Failed MultiStack_Deduction_TwoStacksOf5_Removing7_VerifiesDeductionOrder` (Expected: "Golden Barley Stack 2", But was: "Golden Barley Stack 1")

Expected output after fix:
`Total tests: 17, Passed: 17, Failed: 0`
And running the full standalone suite:
`dotnet test "d:\Unity Projects\Ink and Ash\standalone-tests\InkAndAsh.StandaloneTests.csproj"`
`Total tests: 306, Passed: 306, Failed: 0`
