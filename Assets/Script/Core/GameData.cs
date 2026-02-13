using System.Collections.Generic;


namespace Assets.Core
{
    public class GameData
    {
        // For now we need something to give a "LocalPlayer" selection before CivManager is instantiated
        // this GameDate exists when main menu boots up but CivManger is not running yet.
        // This will be done in NetCode network check of GameObjects for local player
        // in CivManager but do we need it in a Data file for save game ?
        public CivEnum LocalPlayerCivEnum; // temp set to fed for now
        public TechLevel StartingTechLevel;
        public GalaxySize GalaxySize;
        public GalaxyMapType GalaxyMapType;
        public GameMode GameMode;
        public List<CivEnum> MajorCivsInGameList = new List<CivEnum>();
    }
}
