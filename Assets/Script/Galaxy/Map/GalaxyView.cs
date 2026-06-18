using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



public class GalaxyView : MonoBehaviour
{
        public void Initialize() { }
        public void UpdateState() { }
    public static GalaxyView Instance { get; private set; }
    [Header("Galaxy Reference")]
    private GameObject galaxyImageGO;

    // Cached bounds to avoid recalculating every frame
    private float galaxyWidth = -1f;
    private float galaxyHeight = -1f;
    private void Awake()
    {
        if (GalaxyView.Instance != null)
        {
            GalaxyView.Instance.RegisterGalaxy(GetComponent<SpriteRenderer>());
        }
        // ✅ Scene-based singleton
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("✅ GalaxyMenuUIController: Instance assigned");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"❌ Duplicate GalaxyMenuUIController found! Destroying duplicate.");
            Destroy(gameObject);
        }
        galaxyImageGO = this.gameObject; // the script is on the same GameObject as the galaxy image
    }
    public void RegisterGalaxy(SpriteRenderer renderer)
    {
        galaxyWidth = renderer.bounds.size.x;
        galaxyHeight = renderer.bounds.size.y;
    }
    public float GalaxyWidth
    {
        get
        {
            if (galaxyWidth < 0f)
                CalculateGalaxyBounds();
            return galaxyWidth;
        }
    }

    public float GalaxyHeight
    {
        get
        {
            if (galaxyHeight < 0f)
                CalculateGalaxyBounds();
            return galaxyHeight;
        }
    }
    public void CalculateGalaxyBounds()
    {
        if (galaxyImageGO == null)
        {
            Debug.LogWarning("GameManager: galaxyImageGO is null, using default galaxy bounds.");
            galaxyWidth = 200f;
            galaxyHeight = 200f;
            return;
        }

        var spriteRenderer = galaxyImageGO.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // SpriteRenderer.bounds gives world-space size after scale
            galaxyWidth = spriteRenderer.bounds.size.x;
            galaxyHeight = spriteRenderer.bounds.size.z; // or .y depending on your axis
            Debug.Log($"GameManager: Galaxy bounds calculated: width={galaxyWidth}, height={galaxyHeight}");
        }
        else
        {
            // Fallback: if Image component (UI) instead of SpriteRenderer
            var image = galaxyImageGO.GetComponent<Image>();
            var rectTransform = galaxyImageGO.GetComponent<RectTransform>();
            if (image != null && rectTransform != null)
            {
                galaxyWidth = rectTransform.rect.width * rectTransform.lossyScale.x;
                galaxyHeight = rectTransform.rect.height * rectTransform.lossyScale.y;
                Debug.Log($"GameManager: Galaxy UI bounds calculated: width={galaxyWidth}, height={galaxyHeight}");
            }
            else
            {
                Debug.LogWarning("GameManager: No SpriteRenderer or Image found, using default bounds.");
                galaxyWidth = 200f;
                galaxyHeight = 200f;
            }
        }
    }
    // Optional: force recalculation if galaxy scale changes at runtime
    //public void RecalculateGalaxyBounds()
    //{
    //    galaxyWidth = -1f;
    //    galaxyHeight = -1f;
    //    CalculateGalaxyBounds();
    //}
}
