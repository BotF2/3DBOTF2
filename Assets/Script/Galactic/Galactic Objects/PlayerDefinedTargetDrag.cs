using Assets.Core;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Assets.GamePlay
{
    public class PlayerDefinedTargetDrag : MonoBehaviour
    {
        public static PlayerDefinedTargetDrag Instance;
        private float mouseSpeed = 2f;
        private bool playerTargetDrag = false;
        private GameObject ourPlayerTargetGO;
        private Vector3 lastMousePosition;


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
            if (playerTargetDrag)
                DragPlayerTargetWithLeftMouse(ourPlayerTargetGO);
        }
        public void SetPlayerTargetDrag(bool value, PlayerDefinedTargetController playerCon)
        {
            ourPlayerTargetGO = playerCon.gameObject;
            playerTargetDrag = value;
        }
        void DragPlayerTargetWithLeftMouse(GameObject playerTargetGO)
        {
            if (Input.GetMouseButtonDown(0)) // && !Input.GetKey(KeyCode.Space)) done in Update
            {
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0)) // && !Input.GetKey(KeyCode.Space))
            {
                if (EventSystem.current != null)
                {
                    if (!EventSystem.current.IsPointerOverGameObject()) // do not drage camera when over UI
                    {
                        Vector3 delta = (Input.mousePosition - lastMousePosition) / mouseSpeed;//
                        MovePlayerTarget(delta.x, delta.y, playerTargetGO);
                        lastMousePosition = Input.mousePosition;
                    }
                }
            }
        }
        private void MovePlayerTarget(float xInput, float zInput, GameObject playerTargetGO)
        {
            float zMove = Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180) * zInput + Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180) * xInput;
            float xMove = Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180) * zInput + Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180) * xInput;
            playerTargetGO.transform.position = playerTargetGO.transform.position + new Vector3(xMove * 1.4f, 0, zMove * 1.4f);
        }
    }
}
