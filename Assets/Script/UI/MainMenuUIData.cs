using System.Collections.Generic;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    public class MainMenuData
    {
        public GalaxyMapType SelectedGalaxyType;// { get; private set; }
        public GalaxySize SelectedGalaxySize; //{ get; private set; }
        public TechLevel SelectedTechLevel; //{ get; private set; }
        public List<CivEnum> InGamePlayableCivList = new List<CivEnum>();
    }
}

