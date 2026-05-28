namespace BOTF3D.Core
{
    internal class Planet : OrbitalGalactic
    {
        public PlanetType planteType;
        private float priorAngle;

        public void LoadPlanet(Planet myPlanet, string[] systemData, int i)
        {
            // this.OrbitalDistance = (ulong)(100 +(i*100) * 1000000 * 1000);
            myPlanet.OrbitalDistance = (myPlanet.OrbitalDistance) + ((ulong)i * (myPlanet.OrbitalDistance * (ulong)2));//80000000000)/8);
            OrbitalGalactic myOrbital = new OrbitalGalactic();
            this.TimeToOrbit = myOrbital.OrbitTime() + ((ulong)i * myOrbital.OrbitTime());// 365 * 24 * 60 * 60/5);
            string pType = systemData[8 + (i * 2)];
            switch (pType)
            {
                case "H":
                    this.planteType = PlanetType.H_uninhabitable;
                    this.GraphicID = (int)PlanetType.H_uninhabitable + 1;
                    break;
                case "J":
                    this.planteType = PlanetType.J_gasGiant;
                    this.GraphicID = (int)PlanetType.J_gasGiant + 1;
                    break;
                case "M":
                    this.planteType = PlanetType.M_habitable;
                    this.GraphicID = (int)PlanetType.M_habitable + 1;
                    break;
                case "L":
                    this.planteType = PlanetType.L_marginalForLife;
                    this.GraphicID = (int)PlanetType.L_marginalForLife + 1;
                    break;
                case "K":
                    this.planteType = PlanetType.K_marsLike;
                    this.GraphicID = (int)PlanetType.K_marsLike + 1;
                    break;
                default:
                    this.planteType = PlanetType.K_marsLike;
                    this.GraphicID = (int)PlanetType.K_marsLike + 1;
                    break;
            }

        }
        public Planet()
        {

        }
        public void LoadMoons(OrbitalGalactic planet, int moons)
        {
            for (int i = 0; i < moons; i++)
            {
                OrbitalGalactic moon = new OrbitalGalactic();
                planet.AddChild(moon);
                moon.OrbitalDistance = 5000000000 + (ulong)(5000000000 * i / 5);
                moon.TimeToOrbit = (moon.OrbitTime() + ((ulong)i * moon.OrbitTime() / 4));
                moon.GraphicID = (int)PlanetType.Moon + 1;
            }
        }
        //public void Generate(int maxMoons)
        //{
        //    this.OrbitalDistance = (ulong)UnityEngine.Random.Range(100, 800) * 1000000 * 1000;
        //    TimeToOrbit = this.OrbitTimeForDistance(); // ToDo: use real physics 
        //    GraphicID = UnityEngine.Random.Range(1, 12);
        //    int moons = UnityEngine.Random.Range(1, maxMoons + 1);
        //    for (int i= 0; i < moons; i++)
        //    {
        //        OrbitalGalactic moon = new OrbitalGalactic();
        //        this.AddChild(moon);
        //        moon.OrbitalDistance = 100000000000; // fix me
        //        moon.TimeToOrbit = moon.OrbitTimeForDistance()/10; // fix with real physics (orbital version)
        //    }
        //}

        ulong OrbitTimeForDistance() // for planet
        {
            // Fix this with real orbital math
            return 365 * 24 * 60 * 60;
        }
        public void MakeEarth()
        {
            OffsetAngle = 0; // North of star
            OrbitalDistance = 150000000000; // 150 million KM
            TimeToOrbit = 365 * 24 * 60 * 60; // for Earth, days * hours * min * sec (in sec)
        }
    }
}
