using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Core;

public class GalaxyCameraDragMoveZoom : MonoBehaviour //, IPointerClickHandler
{
    public static GalaxyCameraDragMoveZoom Instance;
    [SerializeField]
    private Camera galaxyCam;
    [SerializeField]
    private float panSpeed = 400f;
    [SerializeField]
    private float zoomSpeed = 400f;
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



    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    void Update()
    {
        DoZoom();
        KeyboardInputs();
        if (!playerTargetDrag)//!Input.GetKey(KeyCode.Space))
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
        galaxyCam.fieldOfView -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        if (Input.GetKey("q"))
        {
            galaxyCam.fieldOfView += 1f;
        }
        if (Input.GetKey("e"))
        {
            galaxyCam.fieldOfView -= 1f;
        }
        galaxyCam.fieldOfView = Mathf.Clamp(galaxyCam.fieldOfView, 25f, 90f);

    }
    void DrageCameraWithLeftMouse()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0)) 
        {
            if (EventSystem.current != null)
            {
                if (!EventSystem.current.IsPointerOverGameObject()) // do not drage camera when over UI
                {
                    Vector3 delta = (Input.mousePosition - lastMousePosition) / mouseSpeed;//
                    MoveCamera(delta.x, delta.y);
                    lastMousePosition = Input.mousePosition;
                }
            }
        }
    }
    void RotateCamerWithRightMouse()
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

    // get keyboard inputs
    void KeyboardInputs()
    {
        float inputZ = 0f;
        float inputX = 0f;

        if (Input.GetKey("w"))
        {
            inputZ += panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("s"))
        {
            inputZ -= panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("a"))
        {
            inputX += panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("d"))
        {
            inputX -= panSpeed * Time.deltaTime;
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
                    transform.position = new Vector3(listStarSystems[i].transform.position.x, listStarSystems[i].transform.position.y + 100f, listStarSystems[i].transform.position.z - 250f);
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
            atHomePosition = false;
        }
        else
        {
            transform.position = homePosition;
            atHomePosition = true;
        }

    }
    public void OnPointerClick(PointerEventData eventData)
    {
        throw new NotImplementedException();
    }
}

