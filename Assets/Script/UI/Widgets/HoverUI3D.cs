using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



public class HoverUI3D : MonoBehaviour//, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// Only tool tips on our systems and fleet and System UI so far
    /// It gets complicated.
    /// </summary>
    // public static ToolTipController current;

    //public TextMeshProUGUI TextComponent;
    //[Header("Scale Settings")]
    //public float hoverScaleMultiplier = 1.15f;
    //public float scaleSpeed = 10f;

    //[Header("Glow Settings")]
    //public Outline glowOutline;
    //public Color glowColor = Color.cyan;

    //[Header("Sound")]
    //public AudioClip hoverSound;
    //private AudioSource audioSource;

    //[Header("Tooltip")]
    //[TextArea]
    //public string toolTipMessage;
    //public Vector3 toolTipOffset = new Vector3(0, 80f, 0);

    //private Vector3 originalScale;
    //private bool isHovering;

    ////private void Awake()
    ////{
    ////    if (current != null)
    ////    {
    ////        Destroy(gameObject);
    ////    }
    ////    else
    ////    {
    ////        current = this;
    ////        DontDestroyOnLoad(gameObject);
    ////    }
    ////}
    //void Start()
    //{
    //    originalScale = transform.localScale;

    //    if (glowOutline != null)
    //    {
    //        glowOutline.effectColor = glowColor;
    //        glowOutline.enabled = false;
    //    }

    //    audioSource = GetComponent<AudioSource>();
    //    if (audioSource == null)
    //        audioSource = gameObject.AddComponent<AudioSource>();
    //}

    //void Update()
    //{
    //    //Vector3 targetScale = isHovering
    //    //    ? originalScale * hoverScaleMultiplier
    //    //    : originalScale;

    //    //transform.localScale = Vector3.Lerp(
    //    //    transform.localScale,
    //    //    targetScale,
    //    //    Time.deltaTime * scaleSpeed
    //    //);
    //}

    //void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    //{
    //    isHovering = true;

    //    if (glowOutline != null)
    //        glowOutline.enabled = true;

    //    if (hoverSound != null)
    //        audioSource.PlayOneShot(hoverSound);

    //    if (!string.IsNullOrEmpty(toolTipMessage))
    //        HoverUI3DSystem.Instance.Show(
    //            toolTipMessage,
    //            transform.TransformPoint(toolTipOffset)
    //        );

    //    // Attach the single Merge button to the hovered FleetController UI (if any).
    //    // This moves the global ButtonMergeFleet under the hovered fleet's FleetUIGameObject.
    //    var hoverGO = eventData.pointerEnter;
    //    if (hoverGO != null && HoverUI3DSystem.Instance != null)
    //    {
    //        originalScale = new Vector3(0.1f, 0.1f, 0.1f);//fleetGO.transform.;
    //        HoverUI3DSystem.Instance.AttachMergeButtonToFleet(hoverGO, originalScale);
    //    }

    //    if (TextComponent != null) // old code, do we need this too?
    //    {
    //        //var localPlayerCivCon = CivManager.Instance.GetLocalPlayerCivController();
    //        //foreach (StarSysController starSysCon in localPlayerCivCon.CivData.StarSysOwned)
    //        //{
    //        //    if (starSysCon.StarSysData.GetSysName() == TextComponent.text)
    //        //    {
    //        //        //HoverManager.Instance.ShowTip(TextComponent.text);
    //        //        break;
    //        //    }
    //        //}

    //        //if (TextComponent.text.Contains(localPlayerCivCon.CivShortName))
    //        //{
    //        //    //HoverManager.Instance.ShowTip(TextComponent.text);
    //        //}
    //        /////***** ToDo maybe - also see civs we know?
    //        //else
    //        //{
    //        //    var starSysCon = eventData.selectedObject.GetComponent<StarSysController>();
    //        //    //foreach (CivController civCon in localPlayerCivCon.CivData.CivControllersWeKnow)
    //        //    if (starSysCon != null && DiplomacyManager.Instance.FoundADiplomacyController(localPlayerCivCon, starSysCon.StarSysData.CurrentCivController))
    //        //    {
    //        //        //HoverManager.Instance.ShowTip(TextComponent.text);
    //        //    }
    //        //}
    //    }
    //}

    //void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    //{
    //    isHovering = false;

    //    if (glowOutline != null)
    //        glowOutline.enabled = false;

    //    HoverUI3DSystem.Instance.Hide();

    //    // Return the Merge button to the panel (single shared button) when hover ends.
    //    if (HoverUI3DSystem.Instance != null)
    //        HoverUI3DSystem.Instance.DetachMergeButtonToPanel();

    //    //if (HoverManager.Instance != null)
    //    //    HoverManager.Instance.HidTip();
    //}
}
