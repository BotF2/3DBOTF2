using UnityEngine;
using UnityEngine.UI;

public class GridInitializer : MonoBehaviour
{ 
    // custom script to initialize a grid layout in Unity UI
    [Header("References")]
    public RectTransform container;
    public GameObject itemPrefab;

    [Header("Grid Settings")]
    public int numberOfItems = 20;
    public Vector2 cellSize = new Vector2(100, 100);
    public Vector2 spacing = new Vector2(10, 10);

    void Start()
    {
        // Add or get GridLayoutGroup
        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = container.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4; // Number of columns

        // Populate grid
        for (int i = 0; i < numberOfItems; i++)
        {
            GameObject newItem = Instantiate(itemPrefab, container);
            newItem.name = $"Item {i}";
        }

        // Optional: Resize container height for scrolling
        int rows = Mathf.CeilToInt((float)numberOfItems / grid.constraintCount);
        float height = rows * (cellSize.y + spacing.y);
        container.sizeDelta = new Vector2(container.sizeDelta.x, height);
    }
}

