using BOTF3D.Core;

namespace BOTF3D.Civilization
{
    public class IntelProject
    {
        public SecretActionsEnum ActionType;
        public CivEnum InitiatorCiv;
        public CivEnum TargetCiv;
        public int TurnsTotal;
        public int TurnsRemaining;
        public float SuccessChance;   // 0–1
        public float DiscoveryChance; // 0–1; chance of diplomatic penalty on failure
        public bool IsComplete;

        public IntelProject(SecretActionsEnum actionType, CivEnum initiatorCiv, CivEnum targetCiv,
            int turnsTotal, float successChance, float discoveryChance)
        {
            ActionType = actionType;
            InitiatorCiv = initiatorCiv;
            TargetCiv = targetCiv;
            TurnsTotal = turnsTotal;
            TurnsRemaining = turnsTotal;
            SuccessChance = successChance;
            DiscoveryChance = discoveryChance;
        }
    }
}
