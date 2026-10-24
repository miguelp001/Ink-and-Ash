using System;
using System.Collections.Generic;

namespace InkAndAsh.Data
{
    public enum NpcTier
    {
        Core = 1,
        Anchor = 2,
        Background = 3
    }

    public static class FactionNames
    {
        public const string Traditionalists = "Traditionalists";
        public const string Pragmatists = "Pragmatists";
        public const string YoungRadicals = "YoungRadicals";
        public const string TheBurdened = "TheBurdened";
        public const string Outsiders = "Outsiders";
        public const string ImperialBureaucracy = "ImperialBureaucracy";

        private static readonly HashSet<string> AllFactions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Traditionalists,
            Pragmatists,
            YoungRadicals,
            TheBurdened,
            Outsiders,
            ImperialBureaucracy
        };

        public static bool IsValidFaction(string faction)
        {
            return !string.IsNullOrEmpty(faction) && AllFactions.Contains(faction);
        }
    }

    [Serializable]
    public class PADState
    {
        public float pleasure;
        public float arousal;
        public float dominance;

        public PADState() { }

        public PADState(float p, float a, float d)
        {
            pleasure = p;
            arousal = a;
            dominance = d;
        }

        public bool IsValid()
        {
            return pleasure >= -100f && pleasure <= 100f &&
                   arousal >= -100f && arousal <= 100f &&
                   dominance >= -100f && dominance <= 100f;
        }

        public void Clamp()
        {
            pleasure = Math.Clamp(pleasure, -100f, 100f);
            arousal = Math.Clamp(arousal, -100f, 100f);
            dominance = Math.Clamp(dominance, -100f, 100f);
        }

        public void ApplyDelta(PADState delta)
        {
            if (delta == null) return;
            pleasure += delta.pleasure;
            arousal += delta.arousal;
            dominance += delta.dominance;
            Clamp();
        }
    }

    [Serializable]
    public class RelationshipVectors
    {
        public float imperialFear;
        public float factionTrust;

        public RelationshipVectors() { }

        public RelationshipVectors(float fear, float trust)
        {
            imperialFear = fear;
            factionTrust = trust;
        }

        public bool IsValid()
        {
            return imperialFear >= 0f && imperialFear <= 100f &&
                   factionTrust >= 0f && factionTrust <= 100f;
        }

        public void Clamp()
        {
            imperialFear = Math.Clamp(imperialFear, 0f, 100f);
            factionTrust = Math.Clamp(factionTrust, 0f, 100f);
        }
    }

    [Serializable]
    public class DialogueResponse
    {
        public string responseId;
        public string text;
        public PADState targetPadDelta;
        public string nextQuestState;
        public string nextBranchId;
    }

    [Serializable]
    public class DialogueBranch
    {
        public string branchId;
        public string triggerCondition;
        public string dialogueText;
        public List<DialogueResponse> availableResponses;
    }

    [Serializable]
    public class NpcData
    {
        public string npcId;
        public int tier;
        public string name;
        public string homeBiome;
        public string factionAffiliation;
        public PADState padEmotionalState;
        public RelationshipVectors relationshipVectors;
        public List<DialogueBranch> dialogueTreeBranches;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (tier < 1 || tier > 3)
            {
                return false;
            }

            if (padEmotionalState == null)
            {
                return false;
            }

            return true;
        }

        public void ValidateAndClamp()
        {
            if (padEmotionalState != null)
            {
                padEmotionalState.Clamp();
            }

            if (relationshipVectors != null)
            {
                relationshipVectors.Clamp();
            }

            if (!FactionNames.IsValidFaction(factionAffiliation))
            {
                factionAffiliation = FactionNames.Outsiders;
            }
        }
    }
}
