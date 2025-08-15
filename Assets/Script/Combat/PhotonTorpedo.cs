using System.Collections.Generic;
using UnityEngine;

namespace Assets.Core
{
    public class PhotonTorpedo : MonoBehaviour
    {
    //    public float speed = 10f;
    //    public float turnRate = 1f;
    //    private Rigidbody homingTorpedo;

    //    private Transform target;
    //    private List<GameObject> theLocalTargetList;
    //    private float diff = 0;


    //    private void Start()
    //    {

    //        //if (GameManager.current._statePassedMain_Init) // ToDo: how do we know if combat is over? && GameManager.current.FriendShips.Count > 0)
    //        //{
    //        //    string whoTorpedo = gameObject.CivName.Substring(0, 3);
    //        //  //  string friendShips = GameManager.current.FriendNameArray[0].Substring(0, 3); 
    //        //    if (whoTorpedo == friendShips)
    //        //        theLocalTargetList = GameManager.current.EnemyShips;
    //        //    else
    //        //        theLocalTargetList = GameManager.current.FriendShips;
    //        //    homingTorpedo = transform.GetComponent<Rigidbody>();
    //        //    if (homingTorpedo != null)
    //        //    {
    //        //        FindTargetNearTorpedo(theLocalTargetList);
    //        //    }
    //        //    if (_destination == null)
    //        //    {
    //        //        Destroy(gameObject, 0.3f);
    //        //    }
    //        //}
    //    }

    //    private void FixedUpdate()
    //    {
    //        //if (_destination != null && homingTorpedo != null)
    //        //{
    //        //    var targetRotation = Quaternion.LookRotation(_destination.position - transform.position);
    //        //    homingTorpedo.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation, turnRate));
    //        //    transform.Translate(Vector3.forward * speed * Time.deltaTime * 3);
    //        //}
    //        //if (_destination == null)
    //        //{
    //        //    Destroy(gameObject);
    //        //}

    //    }

    //    //public void OnCollisionEnter(Collision collision)
    //    //{
    //    //    if (this.gameObject.tag != collision.gameObject.CivName) // do not blow up the torpedo if it hits the ship collider on launching
    //    //        Destroy(this.gameObject, 0.3f); // kill weapon gameobject holding speed script
    //    //}
    //    //public void FindTargetNearTorpedo(List<GameObject> theTargets)
    //    //{
    //    //    var distance = Mathf.Infinity;
    //    //    foreach (var possibleTarget in theTargets)
    //    //    {
    //    //        if (possibleTarget != null)
    //    //        {
    //    //            diff = (transform.position - possibleTarget.transform.position).sqrMagnitude;
    //    //            if (diff < distance)
    //    //            {
    //    //                distance = diff;
    //    //                _destination = possibleTarget.transform;
    //    //            }
    //    //        }
    //    //    }
    //    //}
    }
}

