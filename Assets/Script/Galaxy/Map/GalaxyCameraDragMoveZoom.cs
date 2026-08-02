using BOTF3D.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;
using BOTF3D.Galaxy;

namespace BOTF3D.Galaxy
{
    public class GalaxyCameraDragMoveZoom : MonoBehaviour
    {
public static GalaxyCameraDragMoveZoom Instance;
    [SerializeField]
    private Camera galaxyCam;
    [SerializeField]
    private float panSpeed = 400f;
    [SerializeField]
    private float zoomSpeed = 300f;
    [SerializeField]
    private float minY = 123f;
    [SerializeField]
    private float maxY = 800f;
    [SerializeField]
    private float mouseSpeed = 2f;
    [SerializeField]
    private float minX = -600f;
    [SerializeField]
    private float maxX = 650f;
    [SerializeField]
    private float minZ = -1140f;
    [SerializeField]
    private float maxZ = 1150f;
    [SerializeField]
    private Vector3 lastMousePosition;
    [SerializeField]
    private bool playerTargetDrag = false;
    [SerializeField]
    private Vector3 homePosition;
    [SerializeField]
    private Vector3 lastCameraPosition;
    [SerializeField]
    private bool foundHomePosition = false;
    [SerializeField]
    private bool atHomePosition = true;
    [SerializeField]
    private float homeXRotation = 31f;
    public float galaxyXRotation = 21f;
    private float homeYRotation = 0f;
    private bool initialOrientationApplied = false;

    private UIControls uiControls;
    private bool isActive = false; // Track if this camera controller is active

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // NO DontDestroyOnLoad - this is specific to GalaxyScene
        }

        // Create InputAction asset instance early but DON'T enable yet
        if (uiControls == null)
        {
            uiControls = new UIControls();
        }

        // Get the camera component from THIS GameObject
        if (galaxyCam == null)
        {
            galaxyCam = GetComponent<Camera>();
        }

        // Start disabled - will be enabled when transitioning to gameplay
        isActive = false;
        this.enabled = false;

        Debug.Log($"GalaxyCameraDragMoveZoom: Initialized on {gameObject.name} (disabled)");
    }

    // Call this when switching to galaxy gameplay
    public void EnableCameraControl()
    {
        isActive = true;
        this.enabled = true;
        if (uiControls != null)
        {
            uiControls.Enable();
        }
        ApplyInitialCivOrientation();
        Debug.Log("GalaxyCameraDragMoveZoom: Camera control enabled");
    }

    // Called once on the first genuine entry into the galaxy scene (EnableCameraControl is
    // also called every time combat ends and control returns to the galaxy, so this must not
    // re-fire then). Borg/Dominion homeworlds sit in the map's far corner (see the matching
    // flip in SetCameraToLocalPlayerHome), so the default starting view - authored looking
    // toward the opposite corner - needs the same 180 degree flip around the galactic center
    // (GalaxyCenter sits at world origin) so the initial view and the Home-button view agree.
    private void ApplyInitialCivOrientation()
    {
        if (initialOrientationApplied) return;
        if (GameController.Instance == null) return;
        initialOrientationApplied = true;

        var localCivEneum = GameController.Instance.GameData.LocalPlayerCivEnum;
        if (localCivEneum != CivEnum.BORG && localCivEneum != CivEnum.DOM) return;

        transform.position = new Vector3(-transform.position.x, transform.position.y, -transform.position.z);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + 180f, transform.eulerAngles.z);
    }

    // Call this when switching back to main menu
    public void DisableCameraControl()
    {
        isActive = false;
        this.enabled = false;
        if (uiControls != null)
        {
            uiControls.Disable();
        }
        Debug.Log("GalaxyCameraDragMoveZoom: Camera control disabled");
    }

    private void OnEnable()
    {
        if (isActive)
        {
            uiControls ??= new UIControls();
            uiControls.Enable();
        }
    }

    private void OnDisable()
    {
        uiControls?.Disable();
    }

    private void OnDestroy()
    {
        // Clean up singleton when scene unloads
        if (Instance == this)
        {
            Instance = null;
        }

        // Proper cleanup to avoid disposed errors
        if (uiControls != null)
        {
            uiControls.Disable();
            uiControls.Dispose();
            uiControls = null;
        }
    }

    void Update()
    {
        if (!isActive) return; // Don't process input if not active

        DoZoom();
        KeyboardInputs();
        if (!playerTargetDrag)
            DrageCameraWithLeftMouse();
        RotateCamerWithRightMouse();
        CameraMoveLimits();
    }

    public void SetPlayerTargetDrag(bool value)
    {
        playerTargetDrag = value;
    }

    private void MoveCamera(float xInput, float zInput)
    {
        float zMove = Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180) * zInput + Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180) * xInput;
        float xMove = Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180) * zInput - Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180) * xInput;
        transform.position = transform.position + new Vector3(xMove, 0, zMove);
    }

    private void DoZoom()
    {
        if (IMGUIBlocker.IsMouseOver()) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        float scrollValue = 0f;
        if (uiControls != null)
        {
            Vector2 v = uiControls.UI.ScrollWheel.ReadValue<Vector2>();
            scrollValue = v.y;
        }
        else if (Mouse.current != null)
        {
            scrollValue = Mouse.current.scroll.ReadValue().y;
        }
        else
        {
            scrollValue = Input.GetAxis("Mouse ScrollWheel");
        }

        // Normalize scroll value (typically ranges from -120 to 120)
        float normalizedScroll = scrollValue / 120f; // Normalize to ~-1 to 1 range
        galaxyCam.fieldOfView -= normalizedScroll * (zoomSpeed * 1f);

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.qKey.isPressed)
            {
                galaxyCam.fieldOfView += 0.5f;
            }
            if (kb.eKey.isPressed)
            {
                galaxyCam.fieldOfView -= 0.5f;
            }
        }
        else
        {
            if (Input.GetKey("q"))
            {
                galaxyCam.fieldOfView += 0.5f;
            }
            if (Input.GetKey("e"))
            {
                galaxyCam.fieldOfView -= 0.5f;
            }
        }

        galaxyCam.fieldOfView = Mathf.Clamp(galaxyCam.fieldOfView, 25f, 90f);
    }

    void DrageCameraWithLeftMouse()
    {
        if (IMGUIBlocker.IsMouseOver()) return;
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                var pos = mouse.position.ReadValue();
                lastMousePosition = new Vector3(pos.x, pos.y, 0f);
            }
            else if (mouse.leftButton.isPressed)
            {
                if (EventSystem.current != null)
                {
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        var pos = mouse.position.ReadValue();
                        Vector3 currentPos = new Vector3(pos.x, pos.y, 0f);
                        Vector3 delta = (currentPos - lastMousePosition) / mouseSpeed;
                        MoveCamera(delta.x, -delta.y);
                        lastMousePosition = currentPos;
                    }
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0))
            {
                if (EventSystem.current != null)
                {
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        Vector3 delta = (Input.mousePosition - lastMousePosition) / mouseSpeed;
                        MoveCamera(delta.x, -delta.y);
                        lastMousePosition = Input.mousePosition;
                    }
                }
            }
        }
    }

    void RotateCamerWithRightMouse()
    {
        if (IMGUIBlocker.IsMouseOver()) return;
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        bool spacePressed = kb != null ? kb.spaceKey.isPressed : Input.GetKey(KeyCode.Space);

        if (mouse != null)
        {
            if (mouse.rightButton.wasPressedThisFrame && !spacePressed)
            {
                var pos = mouse.position.ReadValue();
                lastMousePosition = new Vector3(pos.x, pos.y, 0f);
            }
            if (mouse.rightButton.isPressed && !spacePressed)
            {
                var pos = mouse.position.ReadValue();
                float pitchDelta = transform.eulerAngles.x;
                if ((pos.y - lastMousePosition.y) != 0f)
                {
                    pitchDelta += (pos.y - lastMousePosition.y) / (mouseSpeed * 10f);
                }
                float yawDelta = transform.eulerAngles.y;
                if ((pos.x - lastMousePosition.x) != 0f)
                {
                    yawDelta += (pos.x - lastMousePosition.x) / (mouseSpeed * 10f);
                }
                transform.eulerAngles = new Vector3(pitchDelta, yawDelta, transform.eulerAngles.z);

                lastMousePosition = new Vector3(pos.x, pos.y, 0f);
                Vector3 currentRotation = transform.eulerAngles;
                float clampX = Mathf.Clamp((currentRotation.x > 180) ? currentRotation.x - 360 : currentRotation.x, -40, 50);
                transform.eulerAngles = new Vector3(clampX, currentRotation.y, currentRotation.z);
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.Space))
            {
                lastMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButton(1) && !Input.GetKey(KeyCode.Space))
            {
                float pitchDelta = transform.eulerAngles.x;
                if ((Input.mousePosition.y - lastMousePosition.y) != 0f)
                {
                    pitchDelta += (Input.mousePosition.y - lastMousePosition.y) / (mouseSpeed * 10f);
                }
                float yawDelta = transform.eulerAngles.y;
                if ((Input.mousePosition.x - lastMousePosition.x) != 0f)
                {
                    yawDelta += (Input.mousePosition.x - lastMousePosition.x) / (mouseSpeed * 10f);
                }
                transform.eulerAngles = new Vector3(pitchDelta, yawDelta, transform.eulerAngles.z);

                lastMousePosition = Input.mousePosition;
                Vector3 currentRotation = transform.eulerAngles;
                float clampX = Mathf.Clamp((currentRotation.x > 180) ? currentRotation.x - 360 : currentRotation.x, -40, 50);
                transform.eulerAngles = new Vector3(clampX, currentRotation.y, currentRotation.z);
            }
        }
    }

    void KeyboardInputs()
    {
        if (GUIUtility.keyboardControl != 0) return; // IMGUI text field has focus
        float inputZ = 0f;
        float inputX = 0f;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) inputZ += panSpeed * Time.deltaTime;
            if (kb.sKey.isPressed) inputZ -= panSpeed * Time.deltaTime;
            if (kb.aKey.isPressed) inputX += panSpeed * Time.deltaTime;
            if (kb.dKey.isPressed) inputX -= panSpeed * Time.deltaTime;
        }
        else
        {
            if (Input.GetKey("w")) inputZ += panSpeed * Time.deltaTime;
            if (Input.GetKey("s")) inputZ -= panSpeed * Time.deltaTime;
            if (Input.GetKey("a")) inputX += panSpeed * Time.deltaTime;
            if (Input.GetKey("d")) inputX -= panSpeed * Time.deltaTime;
        }

        MoveCamera(inputX, inputZ);
    }

    void CameraMoveLimits()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }

    public void SetCameraToLocalPlayerHome()
    {
        if (foundHomePosition == false)
        {
            var localCivEneum = GameController.Instance.GameData.LocalPlayerCivEnum;
            var listStarSystems = StarSysManager.Instance.StarSysControllerList;
            for (int i = 0; i < listStarSystems.Count; i++)
            {
                if (listStarSystems[i].StarSysData.CurrentOwnerCivEnum == localCivEneum)
                {
                    lastCameraPosition = transform.position;

                    // Borg/Dominion homeworlds sit in the map's far corner (near
                    // GalaxyPositionBounds.XMax/ZMax), so approaching from the default -Z side
                    // puts almost the entire map behind the camera. Flip both the approach side
                    // and facing 180 degrees around the homeworld so the bulk of the map reads
                    // as "beyond" the homeworld instead of "behind the viewer".
                    bool approachFromFarSide = localCivEneum == CivEnum.BORG || localCivEneum == CivEnum.DOM;
                    float zOffset = approachFromFarSide ? 200f : -200f;
                    homeYRotation = approachFromFarSide ? 180f : 0f;

                    transform.position = new Vector3(listStarSystems[i].transform.position.x,
                        listStarSystems[i].transform.position.y + 125f, listStarSystems[i].transform.position.z + zOffset);
                    transform.rotation = Quaternion.Euler(homeXRotation, homeYRotation, transform.eulerAngles.z);
                    homePosition = transform.position;
                    foundHomePosition = true;
                    atHomePosition = true;
                    break;
                }
            }
        }
        else if (atHomePosition)
        {
            transform.position = lastCameraPosition;
            transform.rotation = Quaternion.Euler(galaxyXRotation, transform.eulerAngles.y, transform.eulerAngles.z);
            atHomePosition = false;
        }
        else
        {
            transform.position = homePosition;
            transform.rotation = Quaternion.Euler(homeXRotation, homeYRotation, transform.eulerAngles.z);
            atHomePosition = true;
        }
    }
}
}


