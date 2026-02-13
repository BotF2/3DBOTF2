/**
 * Created :    Winter 2022
 * Author :     SeungGeon Kim (keithrek@hanmail.net)
 * Project :    FogWar
 * Filename :   csHomebrewFogWar.cs (non-static monobehaviour module)
 * 
 * All Content (C) 2022 Unlimited Fischl Works, all rights reserved.
 */

using System;                       // Convert
using System.Collections.Generic;   // List
using System.IO;                    // Directory
using System.Linq;                  // Enumerable
#if UNITY_EDITOR
using UnityEditor;                  // Handles (editor-only)
#endif
using UnityEngine;                  // Monobehaviour



namespace FischlWorks_FogWar
{

    /// The non-static high-level monobehaviour interface of the AOS Fog of War module.

    /// This class holds serialized data for various configuration properties,\n
    /// and is responsible for scanning / saving / loading the LevelData object.\n
    /// The class handles the update frequency of the fog, plus some shader businesses.\n
    /// Various public interfaces related to FogRevealer's FOV are also available.
    public class csFogWar : MonoBehaviour
    {
        public static csFogWar Instance { get; private set; }
        bool fogReady = false;
        [SerializeField]
        GameObject galacticCamHolder;
        GameObject fogPlaneParent;

        //public LayerMask interactableLayers;
        /// A class for storing the base level data.
        /// 
        /// This class is later serialized into Json format.\n
        /// Empty spaces are stored as 0, while the obstacles are stored as 1.\n
        /// If a level is loaded instead of being scanned, 
        /// the level dimension properties of csFogWar will be replaced by the level data.

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }

            // DON'T find fogPlaneParent here - it might be in a scene that gets unloaded
            // fogPlaneParent = GameObject.FindWithTag("FogPlaneParent");
        }

        [System.Serializable]
        public class LevelData
        {
            public void AddColumn(LevelColumn levelColumn)
            {
                levelRow.Add(levelColumn);
            }

            // Indexer definition
            public LevelColumn this[int index]
            {
                get
                {
                    if (index >= 0 && index < levelRow.Count)
                    {
                        return levelRow[index];
                    }
                    else
                    {
                        Debug.LogErrorFormat("index given in x axis is out of range");

                        return null;
                    }
                }
                set
                {
                    if (index >= 0 && index < levelRow.Count)
                    {
                        levelRow[index] = value;
                    }
                    else
                    {
                        Debug.LogErrorFormat("index given in x axis is out of range");

                        return;
                    }
                }
            }

            // Adding private getter / setters are not allowed for serialization
            public int levelDimensionX = 0;
            public int levelDimensionY = 0;
            public float unitScale = 0;
            public float scanSpacingPerUnit = 0;

            [SerializeField]
            private List<LevelColumn> levelRow = new List<LevelColumn>();
        }



        [System.Serializable]
        public class LevelColumn
        {
            public LevelColumn(IEnumerable<ETileState> stateTiles)
            {
                levelColumn = new List<ETileState>(stateTiles);
            }

            // If I create a separate Tile class, it will impact the size of the save file (but enums will be saved as int)
            public enum ETileState
            {
                Empty,
                Obstacle
            }

            // Indexer definition
            public ETileState this[int index]
            {
                get
                {
                    if (index >= 0 && index < levelColumn.Count)
                    {
                        return levelColumn[index];
                    }
                    else
                    {
                        Debug.LogErrorFormat("index given in y axis is out of range");

                        return ETileState.Empty;
                    }
                }
                set
                {
                    if (index >= 0 && index < levelColumn.Count)
                    {
                        levelColumn[index] = value;
                    }
                    else
                    {
                        Debug.LogErrorFormat("index given in y axis is out of range");

                        return;
                    }
                }
            }

            [SerializeField]
            private List<ETileState> levelColumn = new List<ETileState>();
        }



        [System.Serializable]
        public class FogRevealer
        {
            // DON'T initialize here - it causes the error!
            // Transform camTransform = csFogWar.Instance.galacticCamHolder.transform;

            // Lazy-initialized property instead
            private Transform camTransform;
            private Transform CamTransform
            {
                get
                {
                    if (camTransform == null && csFogWar.Instance != null && csFogWar.Instance.galacticCamHolder != null)
                    {
                        camTransform = csFogWar.Instance.galacticCamHolder.transform;
                    }
                    return camTransform;
                }
            }

            public FogRevealer(Transform revealerTransform, int sightRange, bool updateOnlyOnMove)
            {
                this.revealerTransform = revealerTransform;
                this.sightRange = sightRange;
                this.updateOnlyOnMove = updateOnlyOnMove;
            }

            public Vector2Int GetCurrentLevelCoordinates(csFogWar fogWar)
            {
                // SAFETY CHECK: Ensure transforms still exist
                if (CamTransform == null || revealerTransform == null)
                {
                    Debug.LogWarning("FogRevealer.GetCurrentLevelCoordinates: Transform is null or destroyed");
                    return currentLevelCoordinates; // Return last known position
                }

                float xCam = CamTransform.position.x;
                float zCam = CamTransform.position.z;

                currentLevelCoordinates = new Vector2Int(
                    fogWar.GetUnitX(revealerTransform.position.x),
                    fogWar.GetUnitY(revealerTransform.position.z));

                return currentLevelCoordinates;
            }

            // To be assigned manually by the user
            [SerializeField]
            private Transform revealerTransform = null;
            public Transform _RevealerTransform => revealerTransform;

            [SerializeField]
            private int sightRange = 200;
            public int _SightRange => sightRange;

            [SerializeField]
            private bool updateOnlyOnMove = true;
            public bool _UpdateOnlyOnMove => updateOnlyOnMove;

            private Vector2Int currentLevelCoordinates = new Vector2Int();
            public Vector2Int _CurrentLevelCoordinates
            {
                get
                {
                    lastSeenAt = currentLevelCoordinates;
                    return currentLevelCoordinates;
                }
            }

            [Header("Debug")]
            [SerializeField]
            private Vector2Int lastSeenAt = new Vector2Int(Int32.MaxValue, Int32.MaxValue);
            public Vector2Int _LastSeenAt => lastSeenAt;
        }


        [BigHeader("Basic Properties")]
        [SerializeField]
        private List<FogRevealer> fogRevealers = null;
        public List<FogRevealer> _FogRevealers => fogRevealers;
        [SerializeField]
        private Transform levelMidPoint = null;
        public Transform _LevelMidPoint => levelMidPoint;
        [SerializeField]
        [Range(1, 30)]
        private float FogRefreshRate = 10;

        [BigHeader("Fog Properties")]
        [SerializeField]
        [Range(0, 140)]
        private float fogPlaneHeight = -55;// put it over the background image at -60, 0 is world space of the galaxy stars in 3D space. Other ships are not seen in the shadow, not directly line of sight in the 3D camera view
        [SerializeField]
        private Material fogPlaneMaterial = null;

        [SerializeField]
        private Color fogColor = new Color32(5, 15, 25, 255);
        [SerializeField]
        [Range(0, 1)]
        private float fogPlaneAlpha = 0.8f; // opaque
        [SerializeField]
        [Range(0, 5)]
        private float fogLerpSpeed = 2.5f;
        [Header("Debug")]
        [SerializeField]
        private Texture2D fogPlaneTextureLerpTarget = null;
        [SerializeField]
        private Texture2D fogPlaneTextureLerpBuffer = null;

        [BigHeader("Level Data")]
        [SerializeField]
        private TextAsset LevelDataToLoad = null;
        [SerializeField]
        private bool saveDataOnScan = true;
        [ShowIf("saveDataOnScan")]
        [SerializeField]
        private string levelNameToSave = "SuperCoolFogLevel";

        [BigHeader("Scan Properties")]
        [SerializeField]
        [Range(1, 300)]
        private int levelDimensionX = 130;
        [SerializeField]
        [Range(1, 300)]
        private int levelDimensionY = 180;
        [SerializeField]
        private float unitScale = 10f;  // This 10f, along with scanSpacingPerUnit = 5f and level DimensionX 130 and Y 180, gives a scan (draw gizmos) that is lined up
                                        // with the background galaxy map image. See Unity csFogWar in the Hierarchy, scene.
        public float _UnitScale => unitScale;
        [SerializeField]
        private float scanSpacingPerUnit = 5f;
        [SerializeField]
        private float rayStartHeight = 60;
        [SerializeField]
        private float rayMaxDistance = 110;
        [SerializeField]
        private LayerMask obstacleLayers = new LayerMask();
        [SerializeField]
        private bool ignoreTriggers = true;

        [BigHeader("Debug Options")]
        [SerializeField]
        private bool drawGizmos = false;
        [SerializeField]
        private bool LogOutOfRange = false;

        // External shadowcaster module
        public Shadowcaster shadowcaster { get; private set; } = new Shadowcaster();

        public LevelData levelData { get; private set; } = new LevelData();

        // The primitive plane which will act as a mesh for rendering the fog with
        private GameObject fogPlane = null;

        private float FogRefreshRateTimer = 0;

        private const string levelScanDataPath = "/LevelData";

        //private void Start()
        public void RunFogOfWar()
        {
            // CRITICAL: Find fogPlaneParent NOW (when GalaxyScene is loaded)
            if (fogPlaneParent == null)
            {
                fogPlaneParent = GameObject.FindWithTag("FogPlaneParent");

                if (fogPlaneParent == null)
                {
                    Debug.LogError("csFogWar.RunFogOfWar: FogPlaneParent not found! Cannot initialize fog.");
                    return;
                }

                Debug.Log($"csFogWar: Found FogPlaneParent: {fogPlaneParent.name}");
            }

            // CRITICAL: Find galacticCamHolder NOW if not assigned
            if (galacticCamHolder == null)
            {
                galacticCamHolder = GameObject.Find("GalaxyCenter");

                if (galacticCamHolder == null)
                {
                    // Try finding MainCamera
                    var mainCam = GameObject.FindGameObjectWithTag("MainCamera");
                    if (mainCam != null)
                    {
                        galacticCamHolder = mainCam;
                    }
                }

                Debug.Log($"csFogWar: Found galacticCamHolder: {galacticCamHolder != null}");
            }

            csFogWar.Instance.CheckProperties();

            InitializeVariables();

            if (LevelDataToLoad == null)
            {
                ScanLevel();

                if (saveDataOnScan == true)
                {
#if UNITY_EDITOR
                    SaveScanAsLevelData();
#endif
                }
            }
            else
            {
                LoadLevelData();
            }

            InitializeFog();

            shadowcaster.Initialize(this);

            ForceUpdateFog();
            fogReady = true;

            Debug.Log("csFogWar: Fog of War initialized successfully");
        }

        private void Update()
        {
            if (fogPlane != null && fogReady)
                UpdateFog();
        }

        // --- --- ---

        private void CheckProperties()
        {
            foreach (FogRevealer fogRevealer in fogRevealers)
            {
                if (fogRevealer._RevealerTransform == null)
                {
                    Debug.LogErrorFormat("Please assign a Transform component to each Fog Revealer!");
                }
            }

            if (unitScale <= 0)
            {
                Debug.LogErrorFormat("Unit Scale must be bigger than 0!");
            }

            if (scanSpacingPerUnit <= 0)
            {
                Debug.LogErrorFormat("Scan Spacing Per Unit must be bigger than 0!");
            }

            if (levelMidPoint == null)
            {
                Debug.LogErrorFormat("Please assign the Level Mid Point property!");
            }

            if (fogPlaneMaterial == null)
            {
                Debug.LogErrorFormat("Please assign the \"FogPlane\" material to the Fog Plane Material property!");
            }
        }

        private void InitializeVariables()
        {
            // This is for faster development iteration purposes
            if (obstacleLayers.value == 0)
            {
                obstacleLayers = LayerMask.GetMask("Default");
            }

            // This is also for faster development iteration purposes
            if (levelNameToSave == String.Empty)
            {
                levelNameToSave = "Default";
            }
        }

        private void InitializeFog()
        {
            // SAFETY: Ensure fogPlaneParent still exists
            if (fogPlaneParent == null)
            {
                Debug.LogError("csFogWar.InitializeFog: fogPlaneParent is NULL!");
                return;
            }

            fogPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fogPlane.layer = 8; // Fog of War Plane for ray hits
            fogPlane.name = "[RUNTIME] Fog_Plane";
            fogPlane.transform.SetParent(fogPlaneParent.transform, false); // LINE 414 - now safe

            // Position fog plane at Y = -55 (same level as fog obstacles)
            // This reduces perspective distortion between shadows and star systems
            fogPlane.transform.position = new Vector3(
                levelMidPoint.position.x,
                -55f,  // CHANGED: Was levelMidPoint.position.y + fogPlaneHeight
                levelMidPoint.position.z);

            fogPlane.transform.localScale = new Vector3(
                (levelDimensionX * unitScale) / 10f,
                1,
                (levelDimensionY * unitScale) / 10f);

            fogPlaneTextureLerpTarget = new Texture2D(levelDimensionX, levelDimensionY);
            fogPlaneTextureLerpBuffer = new Texture2D(levelDimensionX, levelDimensionY);

            fogPlaneTextureLerpBuffer.wrapMode = TextureWrapMode.Clamp;
            fogPlaneTextureLerpBuffer.filterMode = FilterMode.Bilinear;

            fogPlane.GetComponent<MeshRenderer>().material = new Material(fogPlaneMaterial);
            fogPlane.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", fogPlaneTextureLerpBuffer);

            MeshCollider mCollider = fogPlane.GetComponent<MeshCollider>();
            mCollider.enabled = false;
        }



        private void ForceUpdateFog()
        {
            UpdateFogField();

            Graphics.CopyTexture(fogPlaneTextureLerpTarget, fogPlaneTextureLerpBuffer);
        }

        private void UpdateFog()
        {
            if (fogRevealers == null || fogRevealers.Count == 0)
                return;

            // Clean up any destroyed revealers BEFORE iterating
            int removedCount = fogRevealers.RemoveAll(r => r == null || r._RevealerTransform == null);
            if (removedCount > 0)
            {
                Debug.LogWarning($"csFogWar.UpdateFog: Removed {removedCount} destroyed revealer(s)");
            }

            // CRITICAL: Track if ANY revealer moved
            bool anyRevealerMoved = false;

            // Now iterate safely
            for (int i = 0; i < fogRevealers.Count; i++)
            {
                var revealer = fogRevealers[i];

                // Double-check safety
                if (revealer == null || revealer._RevealerTransform == null)
                {
                    continue;
                }

                // Get coordinates and update fog
                try
                {
                    Vector2Int currentCoords = revealer.GetCurrentLevelCoordinates(this);

                    // CRITICAL: Check if revealer moved to a different grid cell
                    if (revealer._UpdateOnlyOnMove)
                    {
                        // Only update if moved to different grid cell
                        if (currentCoords != revealer._LastSeenAt)
                        {
                            anyRevealerMoved = true;
                        }
                    }
                    else
                    {
                        // Always update if updateOnlyOnMove is false
                        anyRevealerMoved = true;
                    }
                }
                catch (MissingReferenceException ex)
                {
                    Debug.LogWarning($"csFogWar.UpdateFog: Revealer transform destroyed mid-update: {ex.Message}");
                    fogRevealers.RemoveAt(i);
                    i--; // Adjust index after removal
                }
            }

            // CRITICAL: Only recalculate fog if a revealer moved OR updateOnlyOnMove is false
            if (anyRevealerMoved)
            {
                UpdateFogField();
                UpdateFogPlaneTextureBuffer();
            }
        }

        private void UpdateFogField()
        {
            shadowcaster.ResetTileVisibility();

            foreach (FogRevealer fogRevealer in fogRevealers)
            {
                fogRevealer.GetCurrentLevelCoordinates(this);

                shadowcaster.ProcessLevelData(
                    fogRevealer._CurrentLevelCoordinates,
                    Mathf.RoundToInt(fogRevealer._SightRange / unitScale));
            }

            UpdateFogPlaneTextureTarget();
        }



        // Doing shader business on the script, if we pull this out as a shader pass, same operations must be repeated
        private void UpdateFogPlaneTextureBuffer()
        {
            for (int xIterator = 0; xIterator < levelDimensionX; xIterator++)
            {
                for (int yIterator = 0; yIterator < levelDimensionY; yIterator++)
                {
                    Color bufferPixel = fogPlaneTextureLerpBuffer.GetPixel(xIterator, yIterator);
                    Color targetPixel = fogPlaneTextureLerpTarget.GetPixel(xIterator, yIterator);

                    fogPlaneTextureLerpBuffer.SetPixel(xIterator, yIterator, Color.Lerp(
                        bufferPixel,
                        targetPixel,
                        fogLerpSpeed * Time.deltaTime));
                }
            }

            fogPlaneTextureLerpBuffer.Apply();
        }

        private void UpdateFogPlaneTextureTarget()
        {
            fogPlane.GetComponent<MeshRenderer>().material.SetColor("_Color", fogColor);

            fogPlaneTextureLerpTarget.SetPixels(shadowcaster.fogField.GetColors(fogPlaneAlpha));

            fogPlaneTextureLerpTarget.Apply();
        }



        private void ScanLevel()
        {
            // Debug.LogFormat("There is no level data file assigned, scanning level...");

            // These operations have no real computational meaning, but it will bring consistency to the data
            levelData.levelDimensionX = levelDimensionX;
            levelData.levelDimensionY = levelDimensionY;
            levelData.unitScale = unitScale;
            levelData.scanSpacingPerUnit = scanSpacingPerUnit;

            for (int xIterator = 0; xIterator < levelDimensionX; xIterator++)
            {
                // Adding a new list for column (y axis) for each unit in row (x axis)
                levelData.AddColumn(new LevelColumn(Enumerable.Repeat(LevelColumn.ETileState.Empty, levelDimensionY)));

                for (int yIterator = 0; yIterator < levelDimensionY; yIterator++)
                {
                    bool isObstacleHit = Physics.BoxCast(
                        new Vector3(
                            GetWorldX(xIterator),
                            levelMidPoint.position.y + rayStartHeight,
                            GetWorldY(yIterator)),
                        new Vector3(
                            (unitScale - scanSpacingPerUnit) / 2.0f,
                            unitScale / 2.0f,
                            (unitScale - scanSpacingPerUnit) / 2.0f),
                        Vector3.down,
                        Quaternion.identity,
                        rayMaxDistance,
                        obstacleLayers,
                        (QueryTriggerInteraction)(2 - Convert.ToInt32(ignoreTriggers)));

                    if (isObstacleHit == true)
                    {
                        levelData[xIterator][yIterator] = LevelColumn.ETileState.Obstacle;
                    }
                }
            }

            Debug.LogFormat("Successfully scanned level with a scale of {0} x {1}", levelDimensionX, levelDimensionY);
        }



        // We intend to use Application.dataPath only for accessing project files directory (only in unity editor)
#if UNITY_EDITOR
        private void SaveScanAsLevelData()
        {
            string fullPath = Application.dataPath + levelScanDataPath + "/" + levelNameToSave + ".json";

            if (Directory.Exists(Application.dataPath + levelScanDataPath) == false)
            {
                Directory.CreateDirectory(Application.dataPath + levelScanDataPath);

                Debug.LogFormat("level scan data folder at \"{0}\" is missing, creating...", levelScanDataPath);
            }

            if (File.Exists(fullPath) == true)
            {
                Debug.LogFormat("level scan data already exists, overwriting...");
            }

            string levelJson = JsonUtility.ToJson(levelData);

            File.WriteAllText(fullPath, levelJson);

            Debug.LogFormat("Successfully saved level scan data at \"{0}\"", fullPath);
        }
#endif



        private void LoadLevelData()
        {
            Debug.LogFormat("Level scan data with a name of \"{0}\" is assigned, loading...", LevelDataToLoad.name);

            // Exception check is indirectly performed through branching on the upper part of the code
            string levelJson = LevelDataToLoad.ToString();

            levelData = JsonUtility.FromJson<LevelData>(levelJson);

            levelDimensionX = levelData.levelDimensionX;
            levelDimensionY = levelData.levelDimensionY;
            unitScale = levelData.unitScale;
            scanSpacingPerUnit = levelData.scanSpacingPerUnit;

            Debug.LogFormat("Successfully loaded level scan data with the name of \"{0}\"", LevelDataToLoad.name);
        }



        /// Adds a new FogRevealer Instance to the list and returns its index
        public int AddFogRevealer(FogRevealer fogRevealer)
        {
            fogRevealers.Add(fogRevealer);

            return fogRevealers.Count - 1;
        }
        public int RemoveFogRevealer(FogRevealer fogRevealer)
        {
            int index = fogRevealers.IndexOf(fogRevealer);
            if (index != -1)
            {
                fogRevealers.RemoveAt(index);
            }
            else
            {
                Debug.LogFormat("Given FogRevealer instance not found in the revealers' container");
            }
            return index;
        }


        /// Removes a FogRevealer Instance from the list with index
        public void RemoveFogRevealerByIndex(int revealerIndex)
        {
            if (fogRevealers.Count > revealerIndex && revealerIndex > -1)
            {
                fogRevealers.RemoveAt(revealerIndex);
            }
            else
            {
                Debug.LogFormat("Given index of {0} exceeds the revealers' container range", revealerIndex);
            }
        }



        /// Replaces the FogRevealer list with the given one
        public void ReplaceFogRevealerList(List<FogRevealer> fogRevealers)
        {
            this.fogRevealers = fogRevealers;
        }



        /// Checks if the given level coordinates are within level dimension range.
        public bool CheckLevelGridRange(Vector2Int levelCoordinates)
        {
            bool result =
                levelCoordinates.x >= 0 &&
                levelCoordinates.x < levelData.levelDimensionX &&
                levelCoordinates.y >= 0 &&
                levelCoordinates.y < levelData.levelDimensionY;

            if (result == false && LogOutOfRange == true)
            {
                Debug.LogFormat("Level coordinates \"{0}\" is out of grid range", levelCoordinates);
            }

            return result;
        }

        /// Checks if the given world coordinates are within level dimension range.
        public bool CheckWorldGridRange(Vector3 worldCoordinates)
        {
            Vector2Int levelCoordinates = WorldToLevel(worldCoordinates);

            return CheckLevelGridRange(levelCoordinates);
        }



        /// Checks if the given pair of world coordinates and additionalRadius is visible by FogRevealers.
        public bool CheckVisibility(Vector3 worldCoordinates, int additionalRadius)
        {
            Vector2Int levelCoordinates = WorldToLevel(worldCoordinates);

            if (additionalRadius == 0)
            {
                return shadowcaster.fogField[levelCoordinates.x][levelCoordinates.y] ==
                    Shadowcaster.LevelColumn.ETileVisibility.Revealed;
            }

            int scanResult = 0;

            for (int xIterator = -1; xIterator < additionalRadius + 1; xIterator++)
            {
                for (int yIterator = -1; yIterator < additionalRadius + 1; yIterator++)
                {
                    if (CheckLevelGridRange(new Vector2Int(
                        levelCoordinates.x + xIterator,
                        levelCoordinates.y + yIterator)) == false)
                    {
                        scanResult = 0;

                        break;
                    }

                    scanResult += Convert.ToInt32(
                        shadowcaster.fogField[levelCoordinates.x + xIterator][levelCoordinates.y + yIterator] ==
                        Shadowcaster.LevelColumn.ETileVisibility.Revealed);
                }
            }

            if (scanResult > 0)
            {
                return true;
            }

            return false;
        }



        /// Converts unit (divided by unitScale, then rounded) world coordinates to level coordinates.
        public Vector2Int WorldToLevel(Vector3 worldCoordinates)
        {
            Vector2Int unitWorldCoordinates = GetUnitVector(worldCoordinates);

            return new Vector2Int(
                unitWorldCoordinates.x + (levelDimensionX / 2),
                unitWorldCoordinates.y + (levelDimensionY / 2));
        }



        /// Converts level coordinates into world coordinates.
        public Vector3 GetWorldVector(Vector2Int worldCoordinates)
        {
            return new Vector3(
                GetWorldX(worldCoordinates.x + (levelDimensionX / 2)),
                0,
                GetWorldY(worldCoordinates.y + (levelDimensionY / 2)));
        }



        /// Converts "pure" world coordinates into unit world coordinates.
        public Vector2Int GetUnitVector(Vector3 worldCoordinates)
        {
            return new Vector2Int(GetUnitX(worldCoordinates.x), GetUnitY(worldCoordinates.z));
        }



        /// Converts level coordinate to corresponding unit world coordinates.
        public float GetWorldX(int xValue)
        {
            if (levelData.levelDimensionX % 2 == 0)
            {
                return (levelMidPoint.position.x - ((levelDimensionX / 2.0f) - xValue) * unitScale);
            }

            return (levelMidPoint.position.x - ((levelDimensionX / 2.0f) - (xValue + 0.5f)) * unitScale);
        }



        /// Converts world coordinate to unit world coordinates.
        public int GetUnitX(float xValue)
        {
            return Mathf.RoundToInt((xValue - levelMidPoint.position.x) / unitScale);
        }



        /// Converts level coordinate to corresponding unit world coordinates.
        public float GetWorldY(int yValue)
        {
            if (levelData.levelDimensionY % 2 == 0)
            {
                return (levelMidPoint.position.z - ((levelDimensionY / 2.0f) - yValue) * unitScale);
            }

            return (levelMidPoint.position.z - ((levelDimensionY / 2.0f) - (yValue + 0.5f)) * unitScale);
        }



        /// Converts world coordinate to unit world coordinates.
        public int GetUnitY(float yValue)
        {
            return Mathf.RoundToInt((yValue - levelMidPoint.position.z) / unitScale);
        }



#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying == false)
            {
                return;
            }

            if (drawGizmos == false)
            {
                return;
            }

            Handles.color = Color.yellow;

            for (int xIterator = 0; xIterator < levelDimensionX; xIterator++)
            {
                for (int yIterator = 0; yIterator < levelDimensionY; yIterator++)
                {
                    if (levelData[xIterator][yIterator] == LevelColumn.ETileState.Obstacle)
                    {
                        if (shadowcaster.fogField[xIterator][yIterator] == Shadowcaster.LevelColumn.ETileVisibility.Revealed)
                        {
                            Handles.color = Color.green;
                        }
                        else
                        {
                            Handles.color = Color.red;
                        }

                        Handles.DrawWireCube(
                            new Vector3(
                                GetWorldX(xIterator),
                                levelMidPoint.position.y,
                                GetWorldY(yIterator)),
                            new Vector3(
                                unitScale - scanSpacingPerUnit,
                                unitScale,
                                unitScale - scanSpacingPerUnit));
                    }
                    else
                    {
                        Gizmos.color = Color.yellow;

                        Gizmos.DrawSphere(
                            new Vector3(
                                GetWorldX(xIterator),
                                levelMidPoint.position.y,
                                GetWorldY(yIterator)),
                            unitScale / 5.0f);
                    }

                    if (shadowcaster.fogField[xIterator][yIterator] == Shadowcaster.LevelColumn.ETileVisibility.Revealed)
                    {
                        Gizmos.color = Color.green;

                        Gizmos.DrawSphere(
                            new Vector3(
                                GetWorldX(xIterator),
                                levelMidPoint.position.y,
                                GetWorldY(yIterator)),
                            unitScale / 3.0f);
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Clear all fog revealers (call when transitioning scenes)
        /// </summary>
        public void ClearAllRevealers()
        {
            if (fogRevealers != null)
            {
                fogRevealers.Clear();
                Debug.Log("csFogWar: Cleared all fog revealers");
            }
        }

        /// <summary>
        /// Remove a specific fog revealer by transform
        /// </summary>
        public void RemoveRevealer(Transform revealerTransform)
        {
            if (fogRevealers == null || revealerTransform == null) return;

            int removed = fogRevealers.RemoveAll(r => r == null || r._RevealerTransform == null || r._RevealerTransform == revealerTransform);

            if (removed > 0)
            {
                Debug.Log($"csFogWar: Removed {removed} revealer(s) for {revealerTransform.name}");
            }
        }
    }



    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string _BaseCondition
        {
            get { return mBaseCondition; }
        }

        private string mBaseCondition = String.Empty;

        public ShowIfAttribute(string baseCondition)
        {
            mBaseCondition = baseCondition;
        }
    }



    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class BigHeaderAttribute : PropertyAttribute
    {
        public string _Text
        {
            get { return mText; }
        }

        private string mText = String.Empty;

        public BigHeaderAttribute(string text)
        {
            mText = text;
        }
    }



}