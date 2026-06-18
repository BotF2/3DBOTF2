using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.SpaceCombat.AutoBattle.Scripts.Camera
{
    public class BehindCamera : MonoBehaviour
    {
        public GameObject player;
        public float cameraDistance = 0.5f;

        // Use this for initialization
        void Start()
        {
        }

        void LateUpdate()
        {
            //if (player == null)
            //{
            //    player = GameObject.Find("Attackers").transform.GetChild(0).gameObject;

            //    if (player == null)
            //    {
            //        return;
            //    }
            //}
            //transform.position = player.transform.position - player.transform.forward * cameraDistance;
            //transform.LookAt(player.transform.position);
            //transform.position = new Vector3(transform.position.x, transform.position.y + 0.15f, transform.position.z);
        }
    }
}
