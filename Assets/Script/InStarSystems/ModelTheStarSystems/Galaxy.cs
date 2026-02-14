using System;
using System.Collections.Generic;
using UnityEngine;


namespace BOTF3D.Core
{
    /// <summary>
    /// Part of the solar system view code
    /// ToDo: determin if any code is still need for the solarsystem view
    /// and what code can be removed.
    /// </summary>
    public class Galaxy
    {
        public Galaxy theGalaxy;
        public List<SolarSystem> SolarSystems;
        public bool GalaxyNotNull = false;
        public int NumberOfStars;
        public SolarSystem galaxyCenter = new SolarSystem();
        public Dictionary<Vector3, SolarSystem> SolarSystemsMap;
        public void GalaxyInit()
        {
            Galaxy galaxy = new Galaxy();
            theGalaxy = this;
            // GameManager.current.Galaxy = galaxy;

            // For now, we set a SEED for the random number generator, so that it
            // starts from the same Galaxy every time, see planet.cs random is now not so random
            //UnityEngine.Random.InitState(123);
            //NumberOfStars = numberOfStars;
            //SolarSystems = GenerateSystems(numberOfStars); // for solar system view
            //this.AddChild(myStar);

        }
        public void Awake()
        {
            // On awake there is a Galaxy with the galalctic center 'system' but no button for it
            var galaxyCenterSystem = new SolarSystem();
            galaxyCenter = galaxyCenterSystem.GenerateGalaxyCenter();
            Vector3 galacticCenterVector = new Vector3(0, 0, 0);
            SolarSystemsMap.Add(galacticCenterVector, galaxyCenter);
            SolarSystems.Add(galaxyCenterSystem);
        }

        public void Update(UInt64 timeSinceStart)
        {
            // ToDo: Consider only updating the systems player are looking at
            foreach (SolarSystem ss in SolarSystems)
            {
                ss.Update(timeSinceStart); // solarsystem inherits from orbital with this Update()
            }
        }

        public SolarSystem LoadThisSystem(int systemButtonID) // Do we need this for just a single system to be shown by solarSystemView??
        {

            SolarSystem ss = new SolarSystem();
            ss.Generate();
            //result.Add(ss);

            return ss;
        }
        public List<SolarSystem> GenerateSystems(int numberOfStars) // ToDo: load SystemDate.txt instead of generate
        {
            List<SolarSystem> result = new List<SolarSystem>();
            for (int i = 0; i < numberOfStars; i++)
            {
                if (i == 0)
                {
                    // ToDo: use GalaxyView (like a SolarSystem.cs but no moons, just solar systems as buttons in place of stars of a solcar system
                    // a GenerateGalaxyMap() that give buttons for solar systems form GalaxyMap class

                }
                SolarSystem ss = new SolarSystem();
                ss.Generate();
                result.Add(ss);
            }

            return result;
        }
        public bool DoWeHaveAGalaxy()
        {
            if (SolarSystems.Count != 0)
            {
                GalaxyNotNull = true;
            }
            return GalaxyNotNull;
        }
        public void Generate(int numStars) // non canon map generated
        {
            if (SolarSystems.Count == 0)
            {
                NumberOfStars = numStars;
                for (int i = 0; i < numStars; i++)
                {
                    SolarSystem ss = new SolarSystem();
                    ss.Generate();
                    SolarSystems.Add(ss);
                    // Galaxy.AddChild(ss);
                }
                //gameManager.Galaxy = Galaxy;

                // ToDo: use numStars and GalaxyMapType
            }
        }

        //public void AddChildSystem(SolarSystem child)
        //{
        //    child.Parent = this;
        //    SolarSystems.Add(child);
        //}
        //public void RemoveChildSystem(SolarSystem child)
        //{
        //    child.Parent = null;
        //    SolarSystems.Remove(child);
        //}
    }
}
