using BOTF3D.Core;

namespace BOTF3D.Civilization
{
    public enum EspionageProjectType
    {
        StealTechnology  // Phase 3. Phase 4 will add targeted tech steal, sabotage, etc.
    }

    public enum EspionageProjectState
    {
        Active,
        Succeeded,
        Failed
    }

    public class EspionageProject
    {
        public CivEnum TargetCivEnum;
        public EspionageProjectType ProjectType;
        public EspionageProjectState State;
        public int TurnsRemaining;
        public int TechPointsStolen;  // filled on success

        // Base intel cost and duration for a StealTechnology project
        public const int IntelCost     = 50;
        public const int DurationTurns = 8;

        public EspionageProject(CivEnum target, EspionageProjectType type)
        {
            TargetCivEnum  = target;
            ProjectType    = type;
            TurnsRemaining = DurationTurns;
            State          = EspionageProjectState.Active;
        }

        public bool IsComplete => State != EspionageProjectState.Active;
    }
}
