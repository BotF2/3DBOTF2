using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public float cellSize = 2f;
    public GameObject prefab;

    private CustomGrid grid;

    void Start()
    {
        grid = new CustomGrid(width, height, cellSize, Vector3.zero);

        // Optional: visualize grid with gizmos or spawn placeholders
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = grid.GetWorldPosition(x, y);
                Instantiate(prefab, pos, Quaternion.identity);
            }
        }
    }

    public void PlaceObject(Vector3 worldPosition, GameObject obj)
    {
        grid.GetXY(worldPosition, out int x, out int y);
        grid.SetObject(x, y, obj);
        obj.transform.position = grid.GetWorldPosition(x, y);
    }
}

