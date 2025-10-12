using UnityEngine;

public class CustomGrid
{
    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private GameObject[,] gridObjects; // Store assigned objects

    public CustomGrid(int width, int height, float cellSize, Vector3 originPosition)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        gridObjects = new GameObject[width, height];
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, 0, y) * cellSize + originPosition;
    }

    public void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
    }

    public void SetObject(int x, int y, GameObject obj)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            gridObjects[x, y] = obj;
        }
    }

    public GameObject GetObject(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            return gridObjects[x, y];
        }
        return null;
    }
}

