using TMPro;
using UnityEngine;
using BOTF3D.Core;

namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Builds the four Alpha/Beta/Gamma/Delta quadrant name labels at the galaxy map's four
    /// corners, plus four short vertical "post" markers where the quadrant-dividing lines
    /// (x=0 and z=0) meet the map border - one post between each adjacent pair of labels.
    ///
    /// Quadrant split (matches ReportEntry.QuadrantFromPosition): -x/-z = Alpha, +x/-z = Beta,
    /// +x/+z = Delta, -x/+z = Gamma.
    ///
    /// Everything here is generated at runtime and parented under the scene's "GalaxyCenter"
    /// object, so local positions line up 1:1 with GalaxyPositionBounds/StarSysSO.Position.
    /// Attach this to any active GameObject in GalaxyScene - no Inspector wiring required.
    /// </summary>
    public class GalaxyQuadrantMarkers : MonoBehaviour
    {
        [Header("Label")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private float fontSize = 12f;
        [SerializeField] private Color labelColor = new Color(0.6f, 0.85f, 1f, 0.85f);

        // Pulls labels slightly inward from the exact corner so they don't clip off the map art.
        [SerializeField] private float labelInset = 4f;
        [SerializeField] private float labelHeight = 8f;

        [Header("Divider Posts")]
        [SerializeField] private float postHeight = 12f;
        [SerializeField] private float postWidth = 0.6f;
        [SerializeField] private Color postColor = new Color(0.5f, 0.8f, 1f, 0.6f);

        private static readonly (string name, float xSign, float zSign)[] QuadrantCorners =
        {
            ("Alpha", -1f, -1f),
            ("Beta",   1f, -1f),
            ("Delta",  1f,  1f),
            ("Gamma", -1f,  1f),
        };

        private void Start()
        {
            GameObject galaxyCenter = GameObject.Find("GalaxyCenter");
            if (galaxyCenter == null)
            {
                Debug.LogError("GalaxyQuadrantMarkers: GalaxyCenter not found in scene - cannot place quadrant markers.");
                return;
            }

            Transform parent = galaxyCenter.transform;

            float xMin = GalaxyPositionBounds.XMin + labelInset;
            float xMax = GalaxyPositionBounds.XMax - labelInset;
            float zMin = GalaxyPositionBounds.ZMin + labelInset;
            float zMax = GalaxyPositionBounds.ZMax - labelInset;

            foreach (var (name, xSign, zSign) in QuadrantCorners)
            {
                float xEdge = xSign < 0f ? xMin : xMax;
                float zEdge = zSign < 0f ? zMin : zMax;

                // Two labels per quadrant, each on one of its two border edges, sitting
                // halfway between the map corner and the divider post at x=0 or z=0 -
                // rather than stacked exactly on the corner.
                CreateLabel(parent, name, "ZEdge", new Vector3(xEdge, labelHeight, zEdge * 0.5f));
                CreateLabel(parent, name, "XEdge", new Vector3(xEdge * 0.5f, labelHeight, zEdge));
            }

            CreatePost(parent, new Vector3(0f, 0f, GalaxyPositionBounds.ZMin)); // Alpha | Beta
            CreatePost(parent, new Vector3(GalaxyPositionBounds.XMax, 0f, 0f)); // Beta | Delta
            CreatePost(parent, new Vector3(0f, 0f, GalaxyPositionBounds.ZMax)); // Delta | Gamma
            CreatePost(parent, new Vector3(GalaxyPositionBounds.XMin, 0f, 0f)); // Gamma | Alpha
        }

        private void CreateLabel(Transform parent, string quadrantName, string edgeSuffix, Vector3 localPosition)
        {
            GameObject billboardGO = new GameObject($"QuadrantLabel_{quadrantName}_{edgeSuffix}");
            billboardGO.transform.SetParent(parent, false);
            billboardGO.transform.localPosition = localPosition;
            billboardGO.transform.localRotation = Quaternion.identity;
            billboardGO.transform.localScale = Vector3.one;
            billboardGO.AddComponent<BillboardCameraGalactica>();

            GameObject canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(billboardGO.transform, false);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(60f, 10f);
            // World-space canvases are sized in pixels; scale down so 60x10 reads at map scale
            // rather than towering over the map (matches the ~0.05 scale other world-space UI
            // in this project uses under GalaxyCenter's own 10x scene scale).
            canvasRect.localScale = Vector3.one * 0.1f;
            canvasRect.localRotation = Quaternion.Euler(0f, 0f, 0f);

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(canvasGO.transform, false);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.font = fontAsset != null ? fontAsset : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            tmp.text = quadrantName.ToUpperInvariant();
            tmp.fontSize = fontSize;
            tmp.color = labelColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;

            // Force the label to draw on top of the map border art regardless of depth.
            // The default UGUI TMP shader (TMP_SDF-Mobile) hardcodes "ZTest [unity_GUIZTestMode]",
            // a Unity-internal global the UI system controls - there's no per-material property
            // to override it. TMP ships "Distance Field Overlay" specifically for this: it
            // hardcodes ZTest Always / ZWrite Off in the shader itself.
            Material tmpMaterial = tmp.fontMaterial;
            Shader overlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
            if (overlayShader != null)
            {
                tmpMaterial.shader = overlayShader;
            }
            tmpMaterial.renderQueue = 4000;
        }

        private void CreatePost(Transform parent, Vector3 localPosition)
        {
            GameObject postGO = new GameObject("QuadrantDividerPost");
            postGO.transform.SetParent(parent, false);
            postGO.transform.localPosition = localPosition;
            postGO.transform.localRotation = Quaternion.identity;
            postGO.transform.localScale = Vector3.one;

            LineRenderer lr = postGO.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, new Vector3(0f, postHeight, 0f));
            lr.startWidth = postWidth;
            lr.endWidth = postWidth;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            lr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                Material material = new Material(shader) { name = "Mat_QuadrantDividerPost" };
                material.SetColor("_BaseColor", postColor);
                // Default URP Unlit material is opaque, which would ignore postColor's alpha
                // entirely - explicitly configure standard alpha blending (same recipe as
                // StarSysController.CreateSelectionRing's Mat_SelectionRing).
                material.SetFloat("_Surface", 1f); // Transparent
                material.SetFloat("_Blend", 0f); // Alpha
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = 3100;
                lr.material = material;
            }
        }
    }
}
