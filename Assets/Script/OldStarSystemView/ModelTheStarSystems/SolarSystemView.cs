using UnityEngine;

namespace BOTF3D.Core
{
    public class SolarSystemView : MonoBehaviour
    {
        //private GameManager gameManager;
        ////public GameObject emptyGO;
        //public OrbitalSO orbitalSO;
        //public SolarSystem solarSystem;
        //public Sprite[] starSprites;
        //public Sprite[] planetMoonSprites;
        //public Sprite[] solSprites;
        //public Sprite earthMoonSprite;
        //private string[] systemDataArray;
        //public ulong zoomLevels = 150000000000; // times 1 billion zoom
        //float planetMoonScale = 0.25f;
        ////Galaxy ourGalaxy;
        //public float galacticTime = 0;
        //private int deltaTime = 1000;
        //private int earthSpriteCounter = 0;
        //Dictionary<OrbitalGalactic, OrbitalSO> orbitalGameObjectMap; // put in the orbital sprit and get the game object/orbitalSO
        //public static Dictionary<int, string[]> systemDataDictionary = new Dictionary<int, string[]>();

        //void Start()
        //{
        //    gameManager = GameManager.current;

        //    //systemDataDictionary = GalaxyView.SystemDataDictionary; // this really works
        //}
        //void Update()
        //{
        //    galacticTime = galacticTime + deltaTime;
        //    if (solarSystem == null)
        //        return;
        //    else
        //    {
        //        OrbitalGalactic sunOrbitalGalactic = solarSystem.Children[0];

        //        for (int i = 0; i < sunOrbitalGalactic.Children.Count; i++)
        //        {
        //            sunOrbitalGalactic.Children[i].Update(galacticTime); // update offset angle for orbital
        //            UpdateSprites(sunOrbitalGalactic.Children[i]); // UpdateSprites looks a moon in children of the orbital sent to it
        //        }
        //    }

        //}

        //public void TurnOffSolarSystemview(Galaxy Galaxy, int solarSystemID)
        //{
        //    //ourGalaxy = Galaxy;
        //    while (transform.childCount > 0) // delelt old systems from prior update
        //    {
        //        Transform child = transform.GetChild(0);
        //        child.SetParent(null); // decreases number of children in while loop
        //        Destroy(child.gameObject);
        //    }
        //    solarSystem = null;
        //}

        public void ShowNextSolarSystemView(int buttonSystemID)
        {
            //while (transform.childCount > 0) // transform is the SSView child of the solar system button, delelt old systems from prior update
            //{
            //    Transform child = transform.GetChild(0);
            //    child.SetParent(null); // decreases number of children in while loop
            //    Destroy(child.gameObject);
            //}
            //gameManager.ChangeSystemClicked(buttonSystemID, this);
            //gameManager.SwitchtState(GameManager.State.GALACTIC_MAP_INIT, 0);
            //systemDataArray = systemDataDictionary[buttonSystemID];
            //var mySolarSystem = new SolarSystem();
            //mySolarSystem.LoadSystem(systemDataArray);
            //solarSystem = mySolarSystem;

            //for (int i = 0; i < solarSystem.Children.Count; i++)
            //{
            //    this.LoadSpritesForOrbital(this.transform, solarSystem.Children[i]);
            //}

        }
        //private void LoadSpritesForOrbital(Transform transformParent, OrbitalGalactic orbitalG)
        //{

        //    if (orbitalGameObjectMap == null)
        //        orbitalGameObjectMap = new Dictionary<OrbitalGalactic, OrbitalSO>() { { orbitalG, orbitalSO } };
        //    else
        //        orbitalGameObjectMap.Add(orbitalG, orbitalSO);
        //    GameObject emptyGO = orbitalSO.GetComponent<GameObject>();
        //    emptyGO.transform.SetParent(transformParent, false);
        //    emptyGO.transform.position = orbitalG.position / zoomLevels;
        //    emptyGO.layer = 3; // Star System layer
        //    emptyGO.name = "Orbital";
        //    SpriteRenderer renderer = emptyGO.AddComponent<SpriteRenderer>();
        //    renderer.transform.localScale = new Vector3(planetMoonScale, planetMoonScale, planetMoonScale);
        //    if (int.Parse(systemDataArray[0]) == 0)
        //        this.LoadEarthSprites(transformParent, orbitalG, renderer);
        //    else
        //    {
        //        switch (orbitalG.GraphicID)
        //        {
        //            case 0:
        //                string starColor = systemDataArray[7];
        //                gameObject.name = "Star";
        //                switch (starColor)
        //                {
        //                    case "BlueStar":
        //                        renderer.sprite = starSprites[0];
        //                        break;
        //                    case "OrangeStar":
        //                        renderer.sprite = starSprites[1];
        //                        break;
        //                    case "RedStar":
        //                        renderer.sprite = starSprites[2];
        //                        break;
        //                    case "WhiteStar":
        //                        renderer.sprite = starSprites[3];
        //                        break;
        //                    case "YellowStar":
        //                        renderer.sprite = starSprites[4];
        //                        break;
        //                    default:
        //                        break;
        //                }
        //                break;
        //            case 1 + (int)PlanetType.H_uninhabitable:
        //                renderer.sprite = planetMoonSprites[UnityEngine.Random.Range(0, 4)];
        //                break;
        //            case 1 + (int)PlanetType.J_gasGiant:
        //                renderer.sprite = planetMoonSprites[UnityEngine.Random.Range(5, 10)];
        //                break;
        //            case 1 + (int)PlanetType.M_habitable:
        //                renderer.sprite = planetMoonSprites[UnityEngine.Random.Range(11, 16)];
        //                break;
        //            case 1 + (int)PlanetType.L_marginalForLife:
        //                renderer.sprite = planetMoonSprites[UnityEngine.Random.Range(17, 22)];
        //                break;
        //            case 1 + (int)PlanetType.K_marsLike:
        //                renderer.sprite = planetMoonSprites[UnityEngine.Random.Range(23, 28)];
        //                break;
        //            case 1 + (int)PlanetType.Moon: // Our moons are only defined as orbitalgalactic and not as planet so do not really have a planet type
        //                renderer.sprite = planetMoonSprites[UnityEngine.Random.Range(29, 34)];
        //                break;
        //            default:
        //                break;
        //        }
        //    }

        //    for (int i = 0; i < orbitalG.Children.Count; i++)
        //    {
        //        LoadSpritesForOrbital(gameObject.transform, orbitalG.Children[i]);
        //    }

        //}
        //private void LoadEarthSprites(Transform transformParent, OrbitalGalactic orbitalG, SpriteRenderer renderer)
        //{
        //    if (orbitalG.GraphicID == 1 + (int)PlanetType.Moon)
        //    {
        //        if (orbitalG.Parent.GraphicID == 7)
        //        {
        //            renderer.sprite = earthMoonSprite;
        //        }

        //        else
        //            renderer.sprite = planetMoonSprites[UnityEngine.Random.Range(29, 34)];
        //    }
        //    else
        //    {
        //        switch (earthSpriteCounter)
        //        {
        //            case 0:
        //                renderer.sprite = starSprites[4];
        //                //renderer.transform.localPosition = Vector3.zero;
        //                earthSpriteCounter++;
        //                break;
        //            case 1:
        //                renderer.sprite = solSprites[0];
        //                earthSpriteCounter++;
        //                break;
        //            case 2:
        //                renderer.sprite = solSprites[1];
        //                earthSpriteCounter++;
        //                break;
        //            case 3:
        //                renderer.sprite = solSprites[2];
        //                orbitalG.GraphicID = 7;
        //                earthSpriteCounter++;
        //                break;
        //            case 4:
        //                renderer.sprite = solSprites[3];
        //                earthSpriteCounter++;
        //                break;
        //            case 5:
        //                renderer.sprite = solSprites[4];
        //                earthSpriteCounter++;
        //                break;
        //            case 6:
        //                renderer.sprite = solSprites[5];
        //                earthSpriteCounter++;
        //                break;
        //            case 7:
        //                renderer.sprite = solSprites[6];
        //                earthSpriteCounter++;
        //                break;
        //            case 8:
        //                renderer.sprite = solSprites[7];
        //                earthSpriteCounter = 0;
        //                break;
        //            case 9:
        //                earthSpriteCounter = 0;
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //}
        //private void MakeSpritesForOrbital(Transform transformParent, OrbitalGalactic orbitalG)
        //{
        //    //CameraManagerGalactica cameraManagerGalactic = new CameraManagerGalactica();
        //    GameObject gameObject = orbitalSO.GetComponent<GameObject>();
        //    //GameObject gameObject = (GameObject)Instantiate(emptyGO, new Vector3(0, 0, 0), Quaternion.identity);
        //    //orbitalGameObjectMap[orbitalG] = gameObject; // update map
        //    //gameObject.layer = 30; // galactic
        //    gameObject.transform.SetParent(transformParent, false);
        //    // set position in 3D
        //    gameObject.transform.position = orbitalG.position / zoomLevels; // cut down scale of system to view
        //    SpriteRenderer spritView = gameObject.AddComponent<SpriteRenderer>();
        //    spritView.transform.localScale = new Vector3(planetMoonScale, planetMoonScale, planetMoonScale);
        //    spritView.sprite = planetMoonSprites[orbitalG.GraphicID];
        //    orbitalGameObjectMap.Add(orbitalG, orbitalSO);

        //    for (int i = 0; i < orbitalG.Children.Count; i++)
        //    {
        //        MakeSpritesForOrbital(gameObject.transform, orbitalG.Children[i]);
        //        //spritView.transform.LookAt();
        //    }
        //}
        //void UpdateSprites(OrbitalGalactic orbital) //, float time)
        //{
        //    OrbitalSO _orbitalSO = orbitalGameObjectMap[orbital];
        //    _orbitalSO.GetComponent<GameObject>().transform.position = orbital.position / zoomLevels;
        //    for (int i = 0; i < orbital.Children.Count; i++)
        //    {
        //        UpdateSprites(orbital.Children[i]);
        //    }
        //}
        //public void SetZoomLevel(ulong zl)
        //{
        //    zoomLevels = zl;
        //    //Update planet postions and scale graphics to still see planet sprites as a few pix
        //}
    }
}
