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
    private float maxX = 600f;
    [SerializeField]
    private float minZ = -1140f;
    [SerializeField]
    private float maxZ = 500f;
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
        Debug.Log("GalaxyCameraDragMoveZoom: Camera control enabled");
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
                lastMousePosition.y = pos.y;
            }
            if (mouse.rightButton.isPressed && !spacePressed)
            {
                var rotation = transform.eulerAngles.x;
                float delta = rotation;
                var pos = mouse.position.ReadValue();
                if ((pos.y - lastMousePosition.y) != 0f)
                {
                    delta = rotation += (pos.y - lastMousePosition.y) / (mouseSpeed * 10f);
                }
                transform.eulerAngles = new Vector3(delta, transform.eulerAngles.y, transform.eulerAngles.z);

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
                lastMousePosition.y = Input.mousePosition.y;
            }
            if (Input.GetMouseButton(1) && !Input.GetKey(KeyCode.Space))
            {
                var rotation = transform.eulerAngles.x;
                float delta = rotation;
                if ((Input.mousePosition.y - lastMousePosition.y) != 0f)
                {
                    delta = rotation += (Input.mousePosition.y - lastMousePosition.y) / (mouseSpeed * 10f);
                }
                transform.eulerAngles = new Vector3(delta, transform.eulerAngles.y, transform.eulerAngles.z);

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
                    transform.position = new Vector3(listStarSystems[i].transform.position.x,
                        listStarSystems[i].transform.position.y + 125f, listStarSystems[i].transform.position.z - 200f);
                    transform.rotation = Quaternion.Euler(homeXRotation, transform.eulerAngles.y, transform.eulerAngles.z);
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
            transform.rotation = Quaternion.Euler(homeXRotation, transform.eulerAngles.y, transform.eulerAngles.z);
            atHomePosition = true;
        }
    }
}
}


