using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;


namespace BOTF3D.Core
{
    class ProjectionHits
    {
        public float Max { get; private set; }
        public float Min { get; private set; }

        public ProjectionHits(float max, float min)
        {
            Min = min;
            Max = max;
        }

        public ProjectionHits AddPadding(float paddingToMax, float paddingToMin)
        {
            return new ProjectionHits(Max + paddingToMax, Min - paddingToMin);
        }
    }
}
