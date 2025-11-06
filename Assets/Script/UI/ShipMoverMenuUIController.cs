using UnityEngine;

public class ShipMoverMenuUIController : MonoBehaviour
{
    public static ShipMoverMenuUIController Instance;
    public GameObject ShipMoveMenuView;
    public GameObject ShipMoveContainer;
    public GameObject TopSlot;
    public GameObject BottomSlot;
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
    }
}
