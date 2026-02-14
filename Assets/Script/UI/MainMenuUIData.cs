using System.Collections.Generic;

namespace BOTF3D.Core
{
    public class MainMenuData
    {
        public GalaxyMapType SelectedGalaxyType;// { get; private set; }
        public GalaxySize SelectedGalaxySize; //{ get; private set; }
        public TechLevel SelectedTechLevel; //{ get; private set; }
        public List<CivEnum> InGamePlayableCivList = new List<CivEnum>();
    }
}

