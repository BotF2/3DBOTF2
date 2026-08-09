using BOTF3D.Core;

namespace BOTF3D.Civilization
{
    /// <summary>
    /// A multi-turn diplomatic proposal awaiting the target civ's response - the Diplomacy-side
    /// counterpart to IntelProject (see IntelProject.cs). Unlike an IntelProject there is no
    /// discovery/secrecy concept here: the target already knows a proposal was made, the only open
    /// question is whether they accept it. Ticked and resolved by
    /// DiplomacyManager.TickDiplomacyProjects / ResolveDiplomacyProject.
    /// </summary>
    public class DiplomacyProject
    {
        public NegotiationPloysEnum ProposalType;
        public CivEnum ProposerCiv;
        public CivEnum TargetCiv;
        public int TurnsTotal;
        public int TurnsRemaining;
        public float AcceptanceChance; // 0-1
        public bool IsComplete;

        public DiplomacyProject(NegotiationPloysEnum proposalType, CivEnum proposerCiv, CivEnum targetCiv,
            int turnsTotal, float acceptanceChance)
        {
            ProposalType = proposalType;
            ProposerCiv = proposerCiv;
            TargetCiv = targetCiv;
            TurnsTotal = turnsTotal;
            TurnsRemaining = turnsTotal;
            AcceptanceChance = acceptanceChance;
        }
    }
}
