using BOTF3D.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;


namespace BOTF3D.Galaxy
{
    public class PlayerDefinedTargetDrag : MonoBehaviour
    {
        public static PlayerDefinedTargetDrag Instance;
        private float mouseSpeed = 2f;
        private bool playerTargetDrag = false;
        private GameObject ourPlayerTargetGO;
        private Vector3 lastMousePosition;

        // Guards against finalizing on the very same frame the drag is armed (StartDrag() can be
        // called synchronously from a UI button's OnClick, which fires on the mouse-up that
        // released the button - without this, GetMouseButtonUp(0) could still read true that
        // frame and instantly finalize the marker right where it spawned, before the user ever
        // got to drag it).
        private bool sawMouseHeldSinceArm = false;


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
            if (value)
            {
                lastMousePosition = Input.mousePosition;
                sawMouseHeldSinceArm = false;
            }
        }
        void DragPlayerTargetWithLeftMouse(GameObject playerTargetGO)
        {
            if (Input.GetMouseButtonDown(0)) // && !Input.GetKey(KeyCode.Space)) done in Update
            {
                lastMousePosition = Input.mousePosition;
                sawMouseHeldSinceArm = true;
            }
            else if (Input.GetMouseButton(0)) // && !Input.GetKey(KeyCode.Space))
            {
                sawMouseHeldSinceArm = true;
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
            else if (sawMouseHeldSinceArm && Input.GetMouseButtonUp(0))
            {
                var targetController = playerTargetGO != null ? playerTargetGO.GetComponent<PlayerDefinedTargetController>() : null;
                if (targetController != null)
                    targetController.FinalizeDrag();
                else
                    playerTargetDrag = false;
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
