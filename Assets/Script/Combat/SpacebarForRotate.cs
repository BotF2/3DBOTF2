using TMPro;
using UnityEngine;

namespace Assets.Core
{
    public class SpacebarForRotate : MonoBehaviour
    {
        public float dealySeconds = 6f;
        //public float resetTimer = 6f;

        public TMP_Text spacebarRotate;
        private bool reset = true;
        void Start()
        {
            //spacebarRotate.text = "RedStar Alert";
        }
        void Update()
        {

            //if (reset)
            //{
            //    dealySeconds -= Time.deltaTime;
            //    if (dealySeconds <= 3 && dealySeconds > -2)
            //    {
            //        spacebarRotate.text = "Hold down the spacebar to rotate with mouse";
            //    }
            //    else if (dealySeconds > 3)
            //    {
            //        spacebarRotate.text = "RedStar Alert";
            //    }
            //    else
            //    {
            //        spacebarRotate.text = "RedStar Alert";
            //        reset = false;
            //    }
            //}

        }

    }
}
