/*
 * Created :    Winter 2022
 * Author :     SeungGeon Kim (keithrek@hanmail.net)
 * Project :    FogWar
 * Filename :   csFogVisibilityAgent.cs (non-static monobehaviour module)
 * 
 * All Content (C) 2022 Unlimited Fischl Works, all rights reserved.
 */

/*
 * This script is just an example of what player can do with the visibility check interface.
 * You can create whatever agent that player want based on this script.
 * Also, I recommend player to change the part where the FogWar module is fetched with Find()...
 */



using System.Collections.Generic;   // List
using System.Linq;
using UnityEngine;                  // Monobehaviour

namespace FischlWorks_FogWar
{
    /// An example of an monobehaviour agent that utilizes the public interfaces of csFogWar class.

    /// Fetches all MeshRenderers and SkinnedMeshRenderers of child objects,\n
    /// then enables / disables them based on each FogRevealer's FOV.
    public class csFogVisibilityAgent : MonoBehaviour
    {
        [SerializeField]
        private csFogWar fogWar = null;
        public csFogWar FogWar { get { return fogWar; } set { fogWar = value; } }

        [SerializeField]
        private bool visibility = false;

        [SerializeField]
        [Range(0, 2)]
        private int additionalRadius = 0;

        public List<SpriteRenderer> spriteRenderers = null;
        private List<LineRenderer> lineRenderers = null;
        //** not using MeshRenderer in Galaxy map currently
        //private List<MeshRenderer> meshRenderers = null; 
        //** not using SkinnedMeshRenderer in Galaxy map currently
        //private List<SkinnedMeshRenderer> skinnedMeshRenderers = null;

        private void Start()
        {
            // FleetManager.RegisterFleetControllerAndSetupVisuals pre-assigns spriteRenderers before
            // this Start() ever runs (AddComponent + assignment happen synchronously, same frame) -
            // it deliberately excludes whichever of the insignia/unknown sprite renderers lost the
            // civ-contact check, so Update() below never force-enables the "wrong" one back on. Auto-
            // collecting unconditionally here clobbered that filtered list with every SpriteRenderer
            // under the fleet, silently re-including the excluded one and making contact state stop
            // mattering the instant fog visibility toggled true.
            if (spriteRenderers == null)
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>().ToList();
            if (lineRenderers == null)
                lineRenderers = GetComponentsInChildren<LineRenderer>().ToList();
            //meshRenderers = GetComponentsInChildren<MeshRenderer>().ToList();
            //skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
        }

        private void Update()
        {
            if (fogWar == null || fogWar.FogReady == false || fogWar.CheckWorldGridRange(transform.position) == false)
            {
                return;
            }

            visibility = fogWar.CheckVisibility(transform.position, additionalRadius);

            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                renderer.enabled = visibility;
            }
            foreach (LineRenderer renderer in lineRenderers)
            {
                renderer.enabled = visibility;
            }
            //foreach (MeshRenderer renderer in meshRenderers)
            //{
            //    renderer.enabled = visibility;
            //}
            //foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            //{
            //    renderer.enabled = visibility;
            //}
        }



#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (fogWar == null || Application.isPlaying == false || fogWar.FogReady == false)
            {
                return;
            }

            if (fogWar.CheckWorldGridRange(transform.position) == false)
            {
                Gizmos.color = Color.red;

                Gizmos.DrawWireSphere(
                    new Vector3(
                        Mathf.RoundToInt(transform.position.x),
                        0,
                        Mathf.RoundToInt(transform.position.z)),
                    (fogWar._UnitScale / 2.0f) + additionalRadius);

                return;
            }

            if (fogWar.CheckVisibility(transform.position, additionalRadius) == true)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.yellow;
            }

            Gizmos.DrawWireSphere(
                new Vector3(
                    Mathf.RoundToInt(transform.position.x),
                    0,
                    Mathf.RoundToInt(transform.position.z)),
                (fogWar._UnitScale / 2.0f) + additionalRadius);
        }
#endif
    }
}